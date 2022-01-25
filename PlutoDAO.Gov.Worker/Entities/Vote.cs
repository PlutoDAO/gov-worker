using System;

namespace PlutoDAO.Gov.Worker.Entities
{
    public class Vote
    {
        public readonly decimal Amount;
        public readonly Asset Asset;
        public readonly Option Option;
        public readonly string Voter;

        public Vote(string voter, Option option, Asset asset, decimal amount)
        {
            Voter = voter;
            Option = option;
            Asset = asset;
            Amount = amount <= 0
                ? throw new ArgumentOutOfRangeException(nameof(amount), "Only positive amounts are allowed")
                : amount;
        }
    }
}
