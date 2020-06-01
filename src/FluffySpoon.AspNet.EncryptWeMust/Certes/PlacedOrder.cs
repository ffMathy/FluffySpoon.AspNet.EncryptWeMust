using Certes.Acme;
using FluffySpoon.AspNet.EncryptWeMust.Persistence;

namespace FluffySpoon.AspNet.EncryptWeMust.Certes
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