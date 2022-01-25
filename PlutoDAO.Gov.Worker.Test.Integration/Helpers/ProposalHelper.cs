using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using stellar_dotnet_sdk;
using stellar_dotnet_sdk.responses.operations;

namespace PlutoDAO.Gov.Worker.Test.Integration.Helpers
{
    public static class ProposalHelper
    {
        private const decimal StellarPrecision = 10000000M;
        private const long MaxTokens = 100000000000;
        private const int MaximumFiguresPerPayment = 16;
        public static Server Server { get; set; } = null!;


        public static async Task SaveProposal(string serializedProposal, KeyPair proposalMicropaymentReceiver,
            KeyPair proposalMicropaymentSender)
        {
            var senderAccountResponse = await Server.Accounts.Account(proposalMicropaymentSender.AccountId);
            var senderAccount = new Account(proposalMicropaymentSender.AccountId, senderAccountResponse.SequenceNumber);
            var encodedProposalPayments = Encode(serializedProposal);
            var assetCode = await GenerateAssetCode(proposalMicropaymentReceiver.AccountId,
                proposalMicropaymentSender.AccountId);

            var txBuilder = new TransactionBuilder(senderAccount);
            var asset = Asset.CreateNonNativeAsset(assetCode, proposalMicropaymentReceiver.AccountId);

            var changeTrustLineOp = new ChangeTrustOperation.Builder(ChangeTrustAsset.Create(asset))
                .SetSourceAccount(proposalMicropaymentSender).Build();
            var paymentOp =
                new PaymentOperation.Builder(proposalMicropaymentSender, asset, MaxTokens.ToString())
                    .SetSourceAccount(proposalMicropaymentReceiver).Build();
            txBuilder.AddOperation(changeTrustLineOp).AddOperation(paymentOp);

            foreach (var payment in encodedProposalPayments.EncodedProposalMicropayments)
            {
                var encodedTextPaymentOp = new PaymentOperation.Builder(
                        proposalMicropaymentReceiver,
                        asset,
                        payment.ToString(CultureInfo.CreateSpecificCulture("en-us"))
                    )
                    .SetSourceAccount(proposalMicropaymentSender)
                    .Build();
                txBuilder.AddOperation(encodedTextPaymentOp);
            }

            txBuilder.AddOperation(new PaymentOperation.Builder(proposalMicropaymentReceiver, asset,
                encodedProposalPayments.ExcessTokens.ToString(CultureInfo.CreateSpecificCulture("en-us"))).Build());

            var tx = txBuilder.Build();
            tx.Sign(proposalMicropaymentSender);
            tx.Sign(proposalMicropaymentReceiver);

            await Server.SubmitTransaction(tx);
        }

        public static EncodedProposalPayment Encode(string serializedProposal)
        {
            IList<decimal> extraPayments = new List<decimal>();
            decimal encodedDataPayment;
            decimal totalPayments = 0;

            var extraDigits = HexToDecimal(StringToHex(serializedProposal));

            for (var i = 0; i < extraDigits.Length; i += MaximumFiguresPerPayment)
            {
                var encodedDataDecimalSection =
                    decimal.Parse(
                        extraDigits.Substring(i,
                            extraDigits.Length - i > MaximumFiguresPerPayment
                                ? MaximumFiguresPerPayment
                                : extraDigits.Length - i),
                        CultureInfo.InvariantCulture);
                encodedDataPayment = encodedDataDecimalSection / StellarPrecision;

                if (encodedDataPayment == 0) encodedDataPayment = 1000000000;

                extraPayments.Add(encodedDataPayment);
                totalPayments += encodedDataPayment;
            }

            decimal lastSequenceDigitCount = extraDigits.Length % MaximumFiguresPerPayment;
            if (lastSequenceDigitCount == 0) lastSequenceDigitCount = MaximumFiguresPerPayment;

            encodedDataPayment = lastSequenceDigitCount / StellarPrecision;
            extraPayments.Add(encodedDataPayment);
            totalPayments += encodedDataPayment;
            var excessTokens = MaxTokens - totalPayments;

            return new EncodedProposalPayment(extraPayments, excessTokens);
        }

        private static async Task<string> GenerateAssetCode(string proposalReceiverPublicKey,
            string proposalSenderPublicKey)
        {
            IList<string> assetList = new List<string>();
            var response =
                await Server.Payments.ForAccount(proposalSenderPublicKey).Limit(200).Execute();
            while (response.Embedded.Records.Count != 0)
            {
                foreach (var payment in response.Records.OfType<PaymentOperationResponse>())
                    if (payment.SourceAccount == proposalReceiverPublicKey && payment.AssetCode.Contains("PROP"))
                        assetList.Add(payment.AssetCode);
                response = await response.NextPage();
            }

            var uniqueAssetCount = assetList.Distinct().Count();
            return $"PROP{uniqueAssetCount + 1}";
        }

        private static string StringToHex(string decString)
        {
            var bytes = Encoding.Default.GetBytes(decString);
            var hexString = BitConverter.ToString(bytes);
            return hexString.Replace("-", "");
        }

        private static string HexToDecimal(string hexString)
        {
            return BigInteger.Parse(hexString, NumberStyles.HexNumber).ToString();
        }

        public class EncodedProposalPayment
        {
            public readonly IList<decimal> EncodedProposalMicropayments;
            public readonly decimal ExcessTokens;

            public EncodedProposalPayment(IList<decimal> encodedProposalMicropayments, decimal excessTokens)
            {
                EncodedProposalMicropayments = encodedProposalMicropayments;
                ExcessTokens = excessTokens;
            }
        }
    }
}
