using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PlutoDAO.Gov.Worker.Entities;
using PlutoDAO.Gov.Worker.Providers;
using PlutoDAO.Gov.Worker.WebDownloader;
using stellar_dotnet_sdk;
using stellar_dotnet_sdk.requests;
using stellar_dotnet_sdk.responses;
using stellar_dotnet_sdk.responses.operations;
using Asset = stellar_dotnet_sdk.Asset;
using AssetEntity = PlutoDAO.Gov.Worker.Entities.Asset;

namespace PlutoDAO.Gov.Worker
{
    public static class Worker
    {
        public static Server Server { get; set; } = null!;
        public static IWebDownloader WebDownloader { get; set; } = null!;
        public static DateTimeProvider DateTimeProvider { get; set; } = null!;

        public static async Task Run()
        {
            var proposalMicropaymentSenderPrivateKey =
                Environment.GetEnvironmentVariable("PLUTODAO_PROPOSAL_SENDER_ACCOUNT_PRIVATE_KEY");
            var proposalMicropaymentReceiverPrivateKey =
                Environment.GetEnvironmentVariable("PLUTODAO_PROPOSAL_RECEIVER_ACCOUNT_PRIVATE_KEY");
            var plutoDAOEscrowPrivateKey = Environment.GetEnvironmentVariable("PLUTODAO_ESCROW_ACCOUNT_PRIVATE_KEY");
            var plutoDAOResultsPrivateKey = Environment.GetEnvironmentVariable("PLUTODAO_RESULTS_ACCOUNT_PRIVATE_KEY");
            var daysSinceProposalCreation = Environment.GetEnvironmentVariable("DAYS_SINCE_PROPOSAL_CREATION");
            var proposalMicropaymentSenderKeyPair = KeyPair.FromSecretSeed(proposalMicropaymentSenderPrivateKey);
            var proposalMicropaymentReceiverKeyPair = KeyPair.FromSecretSeed(proposalMicropaymentReceiverPrivateKey);
            var plutoDAOEscrowKeyPair = KeyPair.FromSecretSeed(plutoDAOEscrowPrivateKey);
            var plutoDAOResults = KeyPair.FromSecretSeed(plutoDAOResultsPrivateKey);
            var plutoDAOUrl = Environment.GetEnvironmentVariable("PLUTODAO_GOV_URL");

            var transactions = await GetRecentlyClosedProposals(proposalMicropaymentReceiverKeyPair.AccountId,
                proposalMicropaymentSenderKeyPair.AccountId, daysSinceProposalCreation);
            var proposalIds =
                await GetProposalIdsFromTransactions(transactions,
                    proposalMicropaymentReceiverKeyPair.AccountId);
            foreach (var proposalId in proposalIds)
            {
                var votes = await GetVotesForProposalId(proposalId, plutoDAOEscrowKeyPair);
                var proposal = await GetProposal(plutoDAOUrl, proposalId);
                var winningOptionIndex = await CalculateWinnerOption(proposal, votes);
                await SaveResults(plutoDAOResults, proposalMicropaymentReceiverKeyPair, proposalId, winningOptionIndex);
            }
        }

        private static async Task<IList<TransactionResponse>> GetRecentlyClosedProposals(
            string proposalMicropaymentReceiver, string proposalMicropaymentSender, string daysSinceProposalCreation)
        {
            var recentlyClosedProposals = new List<TransactionResponse>();
            var response = await Server.Transactions.ForAccount(proposalMicropaymentReceiver).Limit(200).Execute();
            
            while (response.Embedded.Records.Count != 0)
            {
                recentlyClosedProposals.AddRange(response.Records.Where(tx =>
                    DateTime.Parse(tx.CreatedAt).Date ==
                    DateTimeProvider.Now.Date.AddDays(-int.Parse(daysSinceProposalCreation)))
                    .Where(tx => tx.SourceAccount == proposalMicropaymentSender));

                response = await response.NextPage();
            }

            return recentlyClosedProposals;
        }

        private static async Task<IEnumerable<string>> GetProposalIdsFromTransactions(
            IList<TransactionResponse> transactions,
            string proposalReceiver)
        {
            IList<PaymentOperationResponse> retrievedRecords = new List<PaymentOperationResponse>();

            foreach (var transaction in transactions)
            {
                var response = await Server.Payments.ForTransaction(transaction.Hash).Limit(200).Execute();
                var paymentRecords = response.Records.OfType<PaymentOperationResponse>()
                    .Where(payment => payment.TransactionSuccessful).ToList();
                foreach (var record in paymentRecords)
                    if (record.To == proposalReceiver && record.AssetIssuer == proposalReceiver)
                        retrievedRecords.Add(record);
            }

            return retrievedRecords.Select(record => record.AssetCode).Distinct().ToList();
        }

        private static async Task<IList<Vote>> GetVotesForProposalId(string proposalId,
            KeyPair escrowKeyPair)
        {
            IList<Vote> votes = new List<Vote>();
            var response = await Server.Transactions.ForAccount(escrowKeyPair.AccountId).Order(OrderDirection.DESC)
                .Limit(200).Execute();
            while (response.Embedded.Records.Count != 0)
            {
                var transactionRecords = response.Records.OfType<TransactionResponse>()
                    .Where(transaction => transaction.Successful).ToList();

                foreach (var transactionResponse in transactionRecords)
                    if (transactionResponse.MemoValue != null && transactionResponse.MemoValue.Contains(proposalId))
                    {
                        var operation = await Server.Operations.ForTransaction(transactionResponse.Hash).Execute();
                        var claimableBalance = operation.Embedded.Records
                            .OfType<CreateClaimableBalanceOperationResponse>().First();
                        var option = transactionResponse.MemoValue.Split(" ").Last();
                        string assetCode, assetIssuer;
                        var isNative = claimableBalance.Asset == "native";
                        if (isNative)
                        {
                            assetCode = "XLM";
                            assetIssuer = "";
                        }
                        else
                        {
                            assetCode = claimableBalance.Asset.Split(":").First();
                            assetIssuer = claimableBalance.Asset.Split(":").Last();
                        }

                        var asset = new AssetEntity(new AccountAddress(assetIssuer), assetCode, isNative);
                        var vote = new Vote(claimableBalance.SourceAccount, new Option(option), asset,
                            decimal.Parse(claimableBalance.Amount, CultureInfo.InvariantCulture));
                        votes.Add(vote);
                    }
                response = await response.NextPage();
            }

            return votes;
        }

        private static async Task<Proposal> GetProposal(string plutoDAOUrl, string proposalId)
        {
            var url = $"{plutoDAOUrl}/proposal/{proposalId}";

            try
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                var res = WebDownloader.Get(url);

                var record = JObject.Parse(res);
                var proposalWhitelistedAssets = new List<WhitelistedAsset>();
                var proposalOptions = new List<Option>();

                foreach (var whitelistedAsset in record["whitelistedAssets"])
                {
                    var issuer = new AccountAddress(whitelistedAsset["asset"]["issuer"].ToString());
                    var assetCode = whitelistedAsset["asset"]["code"].ToString();
                    var isNative = bool.Parse(whitelistedAsset["asset"]["isNative"].ToString());
                    var multiplier = decimal.Parse(whitelistedAsset["multiplier"].ToString());
                    var asset = new AssetEntity(issuer, assetCode, isNative);
                    proposalWhitelistedAssets.Add(new WhitelistedAsset(asset, multiplier));
                }

                foreach (var option in record["options"])
                {
                    var parsedOption = new Option(option["name"].ToString());
                    proposalOptions.Add(parsedOption);
                }

                return new Proposal(proposalOptions, proposalWhitelistedAssets);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static async Task<string> CalculateWinnerOption(Proposal proposal,
            IList<Vote> votes)
        {
            var assetPriceInXlm = new Dictionary<string, decimal>();
            var assetMultiplier = new Dictionary<string, decimal>();
            var optionCount = new Dictionary<string, decimal>();

            foreach (var whitelistedAsset in proposal.WhitelistedAssets)
            {
                assetMultiplier[whitelistedAsset.Asset.Code] = whitelistedAsset.Multiplier;

                if (whitelistedAsset.Asset.IsNative)
                {
                    assetPriceInXlm[whitelistedAsset.Asset.Code] = 1m;
                }
                else
                {
                    var tradeAggregations = await Server.TradeAggregations
                        .BaseAsset(Asset.CreateNonNativeAsset(whitelistedAsset.Asset.Code,
                            whitelistedAsset.Asset.Issuer.Address))
                        //.BaseAsset(asset)
                        .CounterAsset(new AssetTypeNative())
                        .Resolution(604800000).Execute();
                    var priceInXlm = decimal.Parse(tradeAggregations.Records.First().Avg, CultureInfo.InvariantCulture);
                    assetPriceInXlm[whitelistedAsset.Asset.Code] = priceInXlm;
                }
            }

            foreach (var option in proposal.Options) optionCount.Add(option.Name, 0m);

            foreach (var vote in votes)
                optionCount[vote.Option.Name] +=
                    assetPriceInXlm[vote.Asset.Code] * assetMultiplier[vote.Asset.Code];

            var orderedOptionCount = optionCount.OrderByDescending(pair => pair.Value).Take(2).ToList();
            var isThereAWinnerOption = orderedOptionCount[0].Value > orderedOptionCount[1].Value;

            if (isThereAWinnerOption)
            {
                var winningOptionIndex = proposal.Options.ToList()
                    .FindIndex(option => option.Name == orderedOptionCount.First().Key) + 1;
                return winningOptionIndex.ToString();
            }

            return "0";
        }

        private static async Task SaveResults(KeyPair resultsAccount,
            KeyPair proposalMicropaymentReceiver, string proposalId, string winningOptionIndex)
        {
            var proposalMicropaymentReceiverAccount =
                await Server.Accounts.Account(proposalMicropaymentReceiver.AccountId);
            var txBuilder = new TransactionBuilder(proposalMicropaymentReceiverAccount);

            var asset = Asset.CreateNonNativeAsset(proposalId, proposalMicropaymentReceiver.AccountId);
            var changeTrustLineOp = new ChangeTrustOperation.Builder(ChangeTrustAsset.Create(asset))
                .SetSourceAccount(resultsAccount).Build();
            txBuilder.AddOperation(changeTrustLineOp);

            if (winningOptionIndex != "0")
            {
                var paymentOp =
                    new PaymentOperation.Builder(resultsAccount, asset, winningOptionIndex)
                        .SetSourceAccount(proposalMicropaymentReceiver).Build();
                txBuilder.AddOperation(paymentOp);
            }

            var tx = txBuilder.Build();
            tx.Sign(proposalMicropaymentReceiver);
            tx.Sign(resultsAccount);
            var transactionResponse = await Server.SubmitTransaction(tx);

            if (!transactionResponse.IsSuccess())
                throw new ApplicationException(
                    transactionResponse
                        .SubmitTransactionResponseExtras
                        .ExtrasResultCodes
                        .OperationsResultCodes
                        .Aggregate("", (acc, code) => $"{acc}, {code}")
                );
        }
    }
}
