using Certes.Acme;
using FluffySpoon.AspNet.LetsEncrypt.Persistence;

namespace FluffySpoon.AspNet.LetsEncrypt.Certes
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