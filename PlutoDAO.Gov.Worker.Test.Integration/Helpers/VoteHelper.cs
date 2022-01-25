using System;
using System.Threading.Tasks;
using PlutoDAO.Gov.Worker.Entities;
using stellar_dotnet_sdk;
using Asset = stellar_dotnet_sdk.Asset;

namespace PlutoDAO.Gov.Worker.Test.Integration.Helpers
{
    public static class VoteHelper
    {
        public static Server Server { get; set; } = null!;

        public static async Task CastVote(KeyPair voter, KeyPair escrow, string proposalId, Vote vote)
        {
            var voterAccountResponse = await Server.Accounts.Account(voter.AccountId);
            var voterAccount =
                new Account(voter.AccountId, voterAccountResponse.SequenceNumber);


            var escrowClaimant = new Claimant
            {
                Destination = escrow,
                Predicate = ClaimPredicate.Not(ClaimPredicate.BeforeAbsoluteTime(DateTime.Now.AddDays(31)))
            };

            var voterClaimant = new Claimant
            {
                Destination = voter,
                Predicate = ClaimPredicate.Not(
                    ClaimPredicate.BeforeAbsoluteTime(DateTime.Now.AddDays(31)))
            };

            var asset = vote.Asset.IsNative
                ? new AssetTypeNative()
                : Asset.CreateNonNativeAsset(vote.Asset.Code, vote.Asset.Issuer.Address);

            var txBuilder = new TransactionBuilder(voterAccount);
            var claimableBalanceOp =
                new CreateClaimableBalanceOperation.Builder(asset, $"{vote.Amount}",
                        new[] {escrowClaimant, voterClaimant})
                    .SetSourceAccount(voter)
                    .Build();
            txBuilder.AddOperation(claimableBalanceOp)
                .AddMemo(new MemoText($"{proposalId} {vote.Option.Name}"));

            var tx = txBuilder.Build();
            tx.Sign(voter);
            await Server.SubmitTransaction(tx);
        }
    }
}
