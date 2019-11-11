using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluffySpoon.AspNet.LetsEncrypt.Persistence.Models;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt.Persistence
{
	public abstract class BaseDnsChallengePersistenceStrategy<TParent> : IChallengePersistenceStrategy
	{
		private const string DnsChallengeNameFormat = "_acme-challenge.{0}";
		private const string WildcardRegex = "^\\*\\.";
		private const string TxtRecordType = "TXT";

		private readonly ILogger<TParent> _logger;

		public BaseDnsChallengePersistenceStrategy(ILogger<TParent> logger)
		{
			this._logger = logger;
		}

		public virtual bool CanHandleChallengeType(ChallengeType challengeType)
		{
			return challengeType == ChallengeType.Dns01;
		}

		public virtual async Task DeleteAsync(IEnumerable<ChallengeDto> challenges)
		{
			var dnsChallenges = challenges.Where(x => x.Type == ChallengeType.Dns01);
			foreach (var dnsChallenge in dnsChallenges)
			{
				_logger.LogTrace("Deleting DNS challenges");

				foreach (var domain in dnsChallenge.Domains)
				{
					var dnsName = GetChallengeDnsName(domain);
					await DeleteAsync(dnsName, TxtRecordType, dnsChallenge.Token);
				}
			}
		}

		public virtual async Task PersistAsync(IEnumerable<ChallengeDto> challenges)
		{
			var dnsChallenges = challenges.Where(x => x.Type == ChallengeType.Dns01);
			foreach (var dnsChallenge in dnsChallenges)
			{
				_logger.LogTrace("Persisting DNS challenges");

				foreach (var domain in dnsChallenge.Domains)
				{
					var dnsName = GetChallengeDnsName(domain);
					await PersistAsync(dnsName, TxtRecordType, dnsChallenge.Token);
				}
			}
		}

		protected string GetChallengeDnsName(string domain)
		{
			var dnsName = Regex.Replace(domain, WildcardRegex, String.Empty);
			dnsName = String.Format(DnsChallengeNameFormat, dnsName);

			return dnsName;
		}

		public virtual Task<IEnumerable<ChallengeDto>> RetrieveAsync()
		{
			return null;
		}

		protected abstract Task DeleteAsync(string recordName, string recordType, string recordValue);

		protected abstract Task PersistAsync(string recordName, string recordType, string recordValue);
	}
}
