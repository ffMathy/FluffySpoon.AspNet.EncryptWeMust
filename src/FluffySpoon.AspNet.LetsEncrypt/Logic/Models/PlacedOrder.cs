using Certes.Acme;
using FluffySpoon.AspNet.LetsEncrypt.Persistence.Models;

namespace FluffySpoon.AspNet.LetsEncrypt.Logic
{
    public class PlacedOrder
    {
        public ChallengeDto[] ChallengeDtos { get; }
        public IOrderContext Order { get; }
        public IChallengeContext[] Challenges { get; }

        public PlacedOrder(
            ChallengeDto[] challengeDtos,
            IOrderContext order,
            IChallengeContext[] challenges)
        {
            // todo invariants
            ChallengeDtos = challengeDtos;
            Order = order;
            Challenges = challenges;
        }
    }
}