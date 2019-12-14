using Certes.Acme;
using FluffySpoon.AspNet.LetsEncrypt.Persistence.Models;

namespace FluffySpoon.AspNet.LetsEncrypt.Logic.Models
{
    public class PlacedOrder
    {
        public ChallengeDto[] Challenges { get; }
        public IOrderContext Order { get; }
        public IChallengeContext[] ChallengeContexts { get; }

        public PlacedOrder(
            ChallengeDto[] challenges,
            IOrderContext order,
            IChallengeContext[] challengeContexts)
        {
            Challenges = challenges;
            Order = order;
            ChallengeContexts = challengeContexts;
        }
    }
}