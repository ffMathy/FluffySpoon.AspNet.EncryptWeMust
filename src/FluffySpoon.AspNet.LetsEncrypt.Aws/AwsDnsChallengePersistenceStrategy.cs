using Amazon.Route53;
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
	public class AwsDnsChallengePersistenceStrategy : IDnsChallengePersistenceStrategy, IAwsDnsChallengePersistenceStrategy
	{
		private const int STATUS_POLL_INTERVAL_SECONDS = 15;

		private readonly AwsOptions _awsOptions;
		private readonly ILogger<IAwsDnsChallengePersistenceStrategy> _logger;
		private readonly IAmazonRoute53 _route53Client;

		public AwsDnsChallengePersistenceStrategy(AwsOptions awsOptions, ILogger<IAwsDnsChallengePersistenceStrategy> logger)
		{
			_logger = logger;
			_awsOptions = awsOptions;
			_route53Client = new AmazonRoute53Client(awsOptions.Credentials, awsOptions.Region);
		}

		public async Task DeleteAsync(string recordName, string recordType)
		{
			_logger.LogDebug($"Starting deletion of {recordType} {recordName}");

			var zone = await FindHostedZone(recordName);
			if (zone == null)
			{
				_logger.LogDebug($"No zone was found");
				return;
			}

			var recordSets = await FindRecordSets(zone, recordName, recordType);
			var changeBatch = new ChangeBatch();
			foreach (var recordSet in recordSets)
			{
				changeBatch.Changes.Add(new Change() { Action = ChangeAction.DELETE, ResourceRecordSet = recordSet });
			}

			if (changeBatch.Changes.Count > 0)
			{
				_logger.LogInformation($"Deleting {changeBatch.Changes.Count} DNS records matching {recordType} {recordName} in zone {zone.Name}");

				var deleteResponse = await _route53Client.ChangeResourceRecordSetsAsync(new ChangeResourceRecordSetsRequest() { ChangeBatch = changeBatch, HostedZoneId = zone.Id });

				var changeRequest = new GetChangeRequest
				{
					Id = deleteResponse.ChangeInfo.Id
				};

				while ((await _route53Client.GetChangeAsync(changeRequest)).ChangeInfo.Status == ChangeStatus.PENDING)
				{
					_logger.LogDebug($"Deletion of {recordType} {recordName} is pending. Checking for status update in {STATUS_POLL_INTERVAL_SECONDS} seconds.");
					Thread.Sleep(TimeSpan.FromSeconds(STATUS_POLL_INTERVAL_SECONDS));
				}
			}
			else
			{
				_logger.LogInformation($"No DNS records matching {recordType} {recordName} were found to delete in zone {zone.Name}");
			}
		}

		public async Task PersistAsync(string recordName, string recordType, string recordValue)
		{
			var zone = await FindHostedZone(recordName);
			if (zone == null)
				return;

			var recordSet = new ResourceRecordSet
			{
				Name = recordName,
				TTL = 60,
				Type = RRType.FindValue(recordType),
				ResourceRecords = new List<ResourceRecord> { new ResourceRecord { Value = recordValue } }
			};

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

			var upsertResponse = await _route53Client.ChangeResourceRecordSetsAsync(new ChangeResourceRecordSetsRequest() { ChangeBatch = changeBatch, HostedZoneId = zone.Id });

			var changeRequest = new GetChangeRequest
			{
				Id = upsertResponse.ChangeInfo.Id
			};

			while ((await _route53Client.GetChangeAsync(changeRequest)).ChangeInfo.Status == ChangeStatus.PENDING)
			{
				_logger.LogDebug($"Creation/update of {recordType} {recordName} with value {recordValue} is pending. Checking for status update in {STATUS_POLL_INTERVAL_SECONDS} seconds.");
				Thread.Sleep(TimeSpan.FromSeconds(STATUS_POLL_INTERVAL_SECONDS));
			}
		}

		private async Task<HostedZone> FindHostedZone(string dnsName)
		{
			_logger.LogDebug($"Finding hosted zone responsible for {dnsName}");

			var partsToMatch = dnsName.Split('.');
			var bestPossibleScore = dnsName.StartsWith("*.") ? partsToMatch.Length - 1 : partsToMatch.Length;

			HostedZone bestMatch = null;
			int bestMatchScore = 0;

			// Enumerate all hosted zones until a match is found, or until all hosted zones have been enumerated
			var hostedZones = await _route53Client.ListHostedZonesAsync();
			while (hostedZones.IsTruncated)
			{
				foreach (var hostedZone in hostedZones.HostedZones)
				{
					_logger.LogDebug($"Checking zone {hostedZone.Name}");

					if (dnsName.ToLower().EndsWith("." + hostedZone.Name.ToLower()))
					{
						// Matches, calculate score
						var hostedZoneParts = hostedZone.Name.Split('.');
						var score = hostedZoneParts.Length;

						if (score == bestPossibleScore)
						{
							_logger.LogInformation($"Exact match for {dnsName} found (zone {hostedZone.Name})");

							// Exact match found
							return hostedZone;
						}
						else if (score > bestMatchScore)
						{
							_logger.LogDebug($"Setting best match for {dnsName} to zone {hostedZone.Name}");

							bestMatch = hostedZone;
							bestMatchScore = score;
						}
					}
				}

				hostedZones = await _route53Client.ListHostedZonesAsync(new ListHostedZonesRequest() { Marker = hostedZones.Marker });
			}

			if (bestMatch == null)
				_logger.LogInformation($"No zone match for {dnsName} found");
			else
				_logger.LogInformation($"Best match for {dnsName} found (zone {bestMatch.Name})");

			return bestMatch;
		}

		private async Task<IEnumerable<ResourceRecordSet>> FindRecordSets(HostedZone zone, string dnsName, string type)
		{
			_logger.LogDebug($"Finding record sets for {type} {dnsName} in zone {zone.Name}");

			var result = new List<ResourceRecordSet>();
			var rootedDnsName = dnsName.EndsWith(".") ? dnsName : dnsName + '.';
			var remainder = dnsName.Replace(zone.Name, String.Empty);
			var recordSets = await _route53Client.ListResourceRecordSetsAsync(new ListResourceRecordSetsRequest() { HostedZoneId = zone.Id, StartRecordType = RRType.FindValue(type), StartRecordName = dnsName });

			while (recordSets.IsTruncated)
			{
				foreach (var recordSet in recordSets.ResourceRecordSets)
				{
					if (recordSet.Name.ToLower().Equals(rootedDnsName.ToLower()))
						result.Add(recordSet);
				}

				recordSets = await _route53Client.ListResourceRecordSetsAsync(new ListResourceRecordSetsRequest() { HostedZoneId = zone.Id, StartRecordType = recordSets.NextRecordType, StartRecordName = recordSets.NextRecordName, StartRecordIdentifier = recordSets.NextRecordIdentifier });
			}

			_logger.LogInformation($"{result.Count} record sets were found for {type} {dnsName} in zone {zone.Name}");

			return result;
		}
	}
}
