using System;
using System.Globalization;
using System.Threading.Tasks;
using stellar_dotnet_sdk;
using stellar_dotnet_sdk.responses;
using Claimant = stellar_dotnet_sdk.Claimant;

namespace PlutoDAO.Gov.Worker.Test.Integration.Helpers
{
    public static class StellarHelper
    {
        private const decimal MaxValue = long.MaxValue / 10000000M;
        public static KeyPair MasterAccount { get; set; } = null!;
        public static Server Server { get; set; } = null!;

        public static async Task AddXlmFunds(KeyPair destinationKeyPair)
        {
            var fundAccountOp = new PaymentOperation.Builder(destinationKeyPair, new AssetTypeNative(), "10000")
                .SetSourceAccount(MasterAccount)
                .Build();

            var masterAccount = await Server.Accounts.Account(MasterAccount.AccountId);
            var transaction = new TransactionBuilder(masterAccount).AddOperation(fundAccountOp).Build();
            transaction.Sign(MasterAccount);
            await Server.SubmitTransaction(transaction);
        }

        private static async Task FundAccountWithXlm(string address)
        {
            var destinationKeyPair = KeyPair.FromAccountId(address);
            var createAccountOp = new CreateAccountOperation.Builder(destinationKeyPair, "10000")
                .SetSourceAccount(MasterAccount)
                .Build();

            var masterAccount = await Server.Accounts.Account(MasterAccount.AccountId);
            var transaction = new TransactionBuilder(masterAccount).AddOperation(createAccountOp).Build();
            transaction.Sign(MasterAccount);
            await Server.SubmitTransaction(transaction);

            Console.WriteLine($"Account {address} funded successfully.");
        }

        public static async Task<KeyPair> CreateAccountKeyPair(string description)
        {
            var pair = KeyPair.Random();
            await FundAccountWithXlm(pair.AccountId);

            Console.WriteLine($"{description} secret key is {pair.SecretSeed}");
            Console.WriteLine($"{description} public key is {pair.AccountId}");
            Console.WriteLine($"{pair.AccountId} funded successfully with XLM");

            return pair;
        }

        //public static async Task CreateFakeProposal(string)

        public static async Task CreateFeesPaymentClaimableBalance(KeyPair proposalCreator, KeyPair destination)
        {
            var proposalCreatorAccountResponse = await Server.Accounts.Account(proposalCreator.AccountId);
            var proposalCreatorAccount =
                new Account(proposalCreator.AccountId, proposalCreatorAccountResponse.SequenceNumber);

            var claimant = new Claimant
            {
                Destination = destination,
                Predicate = ClaimPredicate.Unconditional()
            };

            var txBuilder = new TransactionBuilder(proposalCreatorAccount);
            var claimableBalanceOp =
                new CreateClaimableBalanceOperation.Builder(new AssetTypeNative(), "5", new[] {claimant})
                    .SetSourceAccount(proposalCreator)
                    .Build();
            txBuilder.AddOperation(claimableBalanceOp);

            var tx = txBuilder.Build();
            tx.Sign(proposalCreator);
            await Server.SubmitTransaction(tx);
        }

        public static async Task<KeyPair> GetOrCreateAccountKeyPair(
            string key,
            string description,
            string? secret = null
        )
        {
            Console.WriteLine($"Looking for {key} for {description}");

            if (secret != null)
            {
                Console.WriteLine($"Found {description}");
                return KeyPair.FromSecretSeed(secret);
            }

            Console.WriteLine($"Didn't find {description}, an account will be created and funded");
            var pair = KeyPair.Random();
            var env = Environment.GetEnvironmentVariable("ENVIRONMENT")!;
            await FundAccountWithXlm(pair.AccountId);

            Console.WriteLine($"{description} secret key is {pair.SecretSeed}");
            Console.WriteLine($"{description} public key is {pair.AccountId}");
            Console.WriteLine($"{pair.AccountId} funded successfully with XLM");

            return pair;
        }

        public static Asset CreateAsset(string assetCode, string issuer)
        {
            return new AssetTypeCreditAlphaNum4(assetCode, issuer);
        }

        private static async Task<Account> GetAccount(KeyPair accountKeyPair)
        {
            var sourceAccountResponse = await Server.Accounts.Account(accountKeyPair.AccountId);
            return new Account(accountKeyPair, sourceAccountResponse.SequenceNumber);
        }

        private static async Task<AccountResponse> GetAccountResponse(KeyPair accountKeyPair)
        {
            var sourceAccountResponse = await Server.Accounts.Account(accountKeyPair.Address);
            return sourceAccountResponse;
        }

        public static async Task Pay(KeyPair sourceKeyPair,
            KeyPair destinationKeyPair,
            Asset asset,
            decimal amount)
        {
            var sourceAccount = await GetAccount(sourceKeyPair);

            var changeTrustOperation =
                new ChangeTrustOperation.Builder(ChangeTrustAsset.Create(asset),
                        MaxValue.ToString(CultureInfo.InvariantCulture))
                    .SetSourceAccount(destinationKeyPair)
                    .Build();

            var paymentOperation = new PaymentOperation.Builder(
                    destinationKeyPair,
                    asset,
                    amount.ToString(CultureInfo.InvariantCulture)
                )
                .SetSourceAccount(sourceKeyPair)
                .Build();

            var paymentTransactionBuilder = new TransactionBuilder(sourceAccount);

            var paymentTransaction = paymentTransactionBuilder.AddOperation(changeTrustOperation)
                .AddOperation(paymentOperation).Build();

            paymentTransaction.Sign(sourceKeyPair);
            paymentTransaction.Sign(destinationKeyPair);

            var response = await Server.SubmitTransaction(paymentTransaction);

            Console.WriteLine(
                response.IsSuccess()
                    ? $"Paid {amount.ToString(CultureInfo.InvariantCulture)} {asset.CanonicalName()} to {destinationKeyPair.Address}."
                    : ObjectDumper.Dump(response)
            );
        }


        public static async Task OfferToSellAssetForXlm(Asset asset, KeyPair trader, string amount, string price)
        {
            var traderAccount = await GetAccount(trader);

            var changeTrustOperation =
                new ChangeTrustOperation.Builder(ChangeTrustAsset.Create(asset),
                        MaxValue.ToString(CultureInfo.InvariantCulture))
                    .SetSourceAccount(trader)
                    .Build();


            var bidOperation = new ManageSellOfferOperation.Builder(asset, new AssetTypeNative(), amount, price)
                .SetOfferId(0)
                .SetSourceAccount(trader)
                .Build();

            var transaction = new TransactionBuilder(traderAccount).AddOperation(changeTrustOperation)
                .AddOperation(bidOperation).Build();
            transaction.Sign(trader);
            var response = await Server.SubmitTransaction(transaction);

            Console.WriteLine(
                response.IsSuccess()
                    ? $"Created bid operation for {asset.CanonicalName()}/XLM."
                    : ObjectDumper.Dump(response)
            );
        }

        public static async Task OfferToBuyAssetForXlm(Asset asset, KeyPair trader, string amount, string price)
        {
            var traderAccount = await GetAccount(trader);

            var changeTrustOperation =
                new ChangeTrustOperation.Builder(ChangeTrustAsset.Create(asset),
                        MaxValue.ToString(CultureInfo.InvariantCulture))
                    .SetSourceAccount(trader)
                    .Build();

            var askOperation = new ManageBuyOfferOperation.Builder(new AssetTypeNative(), asset, amount, price)
                .SetOfferId(0)
                .SetSourceAccount(trader)
                .Build();

            var transaction = new TransactionBuilder(traderAccount)
                .AddOperation(changeTrustOperation)
                .AddOperation(askOperation).Build();

            transaction.Sign(trader);
            var response = await Server.SubmitTransaction(transaction);

            Console.WriteLine(
                response.IsSuccess()
                    ? $"Created ask operation for {asset.CanonicalName()}/XLM."
                    : ObjectDumper.Dump(response)
            );
        }

        public static async Task CreateTrustLine(Asset asset, KeyPair receiverKeyPair)
        {
            var receiverAccount = await GetAccountResponse(receiverKeyPair);

            var changeTrustOperation = new ChangeTrustOperation.Builder(ChangeTrustAsset.Create(asset))
                .SetSourceAccount(receiverKeyPair)
                .Build();

            var changeTrustTransaction = new TransactionBuilder(receiverAccount)
                .AddOperation(changeTrustOperation)
                .Build();

            changeTrustTransaction.Sign(receiverKeyPair);

            var transactionResponse = await Server.SubmitTransaction(changeTrustTransaction);

            Console.WriteLine(
                transactionResponse.IsSuccess()
                    ? $"TrustLine from {receiverKeyPair.Address} to {asset.CanonicalName()} was created"
                    : ObjectDumper.Dump(transactionResponse)
            );
        }

        public static async Task<Balance[]> GetAccountBalance(KeyPair accountKeyPair)
        {
            var sourceAccountResponse = await Server.Accounts.Account(accountKeyPair.AccountId);
            return sourceAccountResponse.Balances;
        }
    }
}
