﻿using Amazon.Route53;
using Amazon.Route53.Model;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FluffySpoon.AspNet.LetsEncrypt.Aws
{
	public class AwsDnsChallengePersistenceStrategy : BaseDnsChallengePersistenceStrategy<AwsDnsChallengePersistenceStrategy>
	{
		private const char DomainSegmentSeparator = '.';
		private const string WildcardPrefix = "*.";
		private const int StatusPollIntervalSeconds = 5;
		private const string TxtRecordType = "TXT";

		private readonly AwsOptions _awsOptions;
		private readonly ILogger<AwsDnsChallengePersistenceStrategy> _logger;
		private readonly IAmazonRoute53 _route53Client;

		public AwsDnsChallengePersistenceStrategy(AwsOptions awsOptions, ILogger<AwsDnsChallengePersistenceStrategy> logger) : base(logger)
		{
			_awsOptions = awsOptions;
			_route53Client = new AmazonRoute53Client(awsOptions.Credentials, awsOptions.Region);
			_logger = logger;
		}

		protected override async Task DeleteAsync(string recordName, string recordType, string recordValue)
		{
			_logger.LogDebug("Starting deletion of {RecordType} {RecordName} with value {RecordValue}", recordType, recordName, recordValue);

			var zone = await FindHostedZoneAsync(recordName);
			if (zone == null)
			{
				_logger.LogDebug("No zone was found");
				return;
			}

			if (recordType == TxtRecordType)
				recordValue = NormalizeTxtValue(recordValue);

			var recordSets = await FindRecordSetsAsync(zone, recordName, recordType);
			var changeBatch = new ChangeBatch();

			if (!recordSets.Any())
			{
				_logger.LogInformation("No DNS records matching {RecordType} {RecordName} were found to delete in zone {Zone}", recordType, recordName, zone.Name);
				return;
			}

			foreach (var recordSet in recordSets)
			{
				if (!recordSet.ResourceRecords.Any(x => x.Value != recordValue))
				{
					_logger.LogDebug("Will delete record {RecordType} {RecordName}", recordType, recordName);
					changeBatch.Changes.Add(new Change() { Action = ChangeAction.DELETE, ResourceRecordSet = recordSet });
				}
				else
				{
					recordSet.ResourceRecords.RemoveAll(x => x.Value == recordValue);

					_logger.LogDebug("Will retain remaining answers for {RecordType} {RecordName}", recordType, recordName);
					changeBatch.Changes.Add(new Change() { Action = ChangeAction.UPSERT, ResourceRecordSet = recordSet });
				}
			}

			_logger.LogInformation("Deleting or modifying {Count} DNS records matching {RecordType} {RecordName} with value {RecordValue} in zone {Zone}", changeBatch.Changes.Count, recordType, recordName, recordValue, zone.Name);

			var deleteResponse = await _route53Client.ChangeResourceRecordSetsAsync(
				new ChangeResourceRecordSetsRequest() {
					ChangeBatch = changeBatch,
					HostedZoneId = zone.Id
				});

			var changeRequest = new GetChangeRequest
			{
				Id = deleteResponse.ChangeInfo.Id
			};

			while ((await _route53Client.GetChangeAsync(changeRequest)).ChangeInfo.Status == ChangeStatus.PENDING)
			{
				_logger.LogDebug("Deletion or modification of {RecordType} {RecordName} is pending. Checking for status update in {StatusPollIntervalSeconds} seconds.", recordType, recordName, StatusPollIntervalSeconds);
				Thread.Sleep(TimeSpan.FromSeconds(StatusPollIntervalSeconds));
			}
		}

		protected override async Task PersistAsync(string recordName, string recordType, string recordValue)
		{
			_logger.LogDebug("Starting creation or update of {RecordType} {RecordName} with value {RecordValue}", recordType, recordName, recordValue);

			var zone = await FindHostedZoneAsync(recordName);
			if (zone == null)
			{
				_logger.LogDebug("No zone was found");
				return;
			}

			if (recordType == TxtRecordType)
				recordValue = NormalizeTxtValue(recordValue);

			var existingRecordSets = await FindRecordSetsAsync(zone, recordName, recordType);
			var existingRecordSet = existingRecordSets.FirstOrDefault();

			var recordSet = existingRecordSet ?? new ResourceRecordSet
			{
				Name = recordName,
				TTL = 60,
				Type = RRType.FindValue(recordType),
				ResourceRecords = new List<ResourceRecord>()
			};

			if (recordSet.ResourceRecords.Any(x => x.Value == recordValue))
			{
				_logger.LogDebug("Record {RecordType} {RecordName} with value {RecordValue} already exists", recordType, recordName, recordValue);
				return;
			}

			recordSet.ResourceRecords.Add(new ResourceRecord { Value = recordValue });

			var change1 = new Change
			{
				ResourceRecordSet = recordSet,
				Action = ChangeAction.UPSERT
			};

			var changeBatch = new ChangeBatch
			{
				Changes = new List<Change> { change1 }
			};

			var recordsetRequest = new ChangeResourceRecordSetsRequest
			{
				HostedZoneId = zone.Id,
				ChangeBatch = changeBatch
			};

			_logger.LogInformation("Creating or updating DNS record {RecordType} {RecordName}", recordType, recordName);

			var upsertResponse = await _route53Client.ChangeResourceRecordSetsAsync(
				new ChangeResourceRecordSetsRequest() {
					ChangeBatch = changeBatch,
					HostedZoneId = zone.Id
				});

			var changeRequest = new GetChangeRequest
			{
				Id = upsertResponse.ChangeInfo.Id
			};

			while (await IsChangePendingAsync(changeRequest))
			{
				_logger.LogDebug("Creation/update of {RecordType} {RecordName} with value {RecordValue} is pending. Checking for status update in {StatusPollIntervalSeconds} seconds.", recordType, recordName, recordValue, StatusPollIntervalSeconds);
				Thread.Sleep(TimeSpan.FromSeconds(StatusPollIntervalSeconds));
			}
		}

		private async Task<bool> IsChangePendingAsync(GetChangeRequest changeRequest)
		{
			var changeReponse = await _route53Client.GetChangeAsync(changeRequest);
			return changeReponse.ChangeInfo.Status == ChangeStatus.PENDING;
		}

		private string NormalizeDnsName(string dnsName)
		{
			return dnsName.EndsWith(".") ? dnsName.ToLower() : dnsName.ToLower() + ".";
		}

		private string NormalizeTxtValue(string recordValue)
		{
			if (!recordValue.StartsWith("\"") && !recordValue.EndsWith("\""))
				recordValue = $"\"{recordValue}\"";

			return recordValue;
		}

		private async Task<HostedZone> FindHostedZoneAsync(string dnsName)
		{
			_logger.LogDebug("Finding hosted zone responsible for {DnsName}", dnsName);

			var partsToMatch = dnsName.Split(DomainSegmentSeparator);
			var isWildcard = dnsName.StartsWith(WildcardPrefix);
			var bestPossibleScore = isWildcard ? partsToMatch.Length - 1 : partsToMatch.Length;

			HostedZone bestMatch = null;
			int bestMatchScore = 0;

			var normalizedDnsName = NormalizeDnsName(dnsName);

			// Enumerate all hosted zones until a match is found, or until all hosted zones have been enumerated
			var hostedZones = await _route53Client.ListHostedZonesAsync();

			do
			{
				foreach (var hostedZone in hostedZones.HostedZones)
				{
					_logger.LogDebug("Checking zone {Zone}", hostedZone.Name);

					if (normalizedDnsName.EndsWith(hostedZone.Name, StringComparison.InvariantCultureIgnoreCase))
					{
						var hostedZoneParts = hostedZone.Name.Split(DomainSegmentSeparator);
						var score = hostedZoneParts.Length;

						if (score == bestPossibleScore)
						{
							_logger.LogInformation("Exact match for {DnsName} found (zone {Zone})", dnsName, hostedZone.Name);

							return hostedZone;
						}
						else if (score > bestMatchScore)
						{
							_logger.LogDebug("Setting best match for {DnsName} to zone {Zone}", dnsName, hostedZone.Name);

							bestMatch = hostedZone;
							bestMatchScore = score;
						}
					}
				}

				hostedZones = await _route53Client.ListHostedZonesAsync(
					new ListHostedZonesRequest()
					{
						Marker = hostedZones.Marker
					});
			} while (hostedZones.IsTruncated);

			if (bestMatch == null)
			{
				_logger.LogInformation("No zone match for {DnsName} found", dnsName);
			}
			else
			{
				_logger.LogInformation("Best match for {DnsName} found (zone {Zone})", dnsName, bestMatch.Name);
			}

			return bestMatch;
		}

		private async Task<IEnumerable<ResourceRecordSet>> FindRecordSetsAsync(HostedZone zone, string dnsName, string recordType)
		{
			_logger.LogDebug("Finding record sets for {RecordType} {DnsName} in zone {Zone}", recordType, dnsName, zone.Name);

			var result = new List<ResourceRecordSet>();
			var rootedDnsName = dnsName.EndsWith(DomainSegmentSeparator.ToString()) ? dnsName : dnsName + DomainSegmentSeparator;
			var remainder = dnsName.Replace(zone.Name, String.Empty);

			var recordSets = await _route53Client.ListResourceRecordSetsAsync(
				new ListResourceRecordSetsRequest() {
					HostedZoneId = zone.Id,
					StartRecordType = RRType.FindValue(recordType),
					StartRecordName = dnsName
				});

			do
			{
				foreach (var recordSet in recordSets.ResourceRecordSets)
				{
					if (recordSet.Name.ToLower().Equals(rootedDnsName.ToLower()))
						result.Add(recordSet);
				}

				recordSets = await _route53Client.ListResourceRecordSetsAsync(
					new ListResourceRecordSetsRequest()
					{
						HostedZoneId = zone.Id,
						StartRecordType = recordSets.NextRecordType,
						StartRecordName = recordSets.NextRecordName,
						StartRecordIdentifier = recordSets.NextRecordIdentifier
					});
			} while (recordSets.IsTruncated);

			_logger.LogInformation("{Count} record sets were found for {RecordType} {DnsName} in zone {Zone}", result.Count, recordType, dnsName, zone.Name);

			return result;
		}
	}
}
