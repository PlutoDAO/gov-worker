using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PlutoDAO.Gov.Worker.Entities;
using PlutoDAO.Gov.Worker.Providers;
using PlutoDAO.Gov.Worker.Test.Integration.Fixtures;
using PlutoDAO.Gov.Worker.Test.Integration.Helpers;
using PlutoDAO.Gov.Worker.Test.Integration.Mocks;
using stellar_dotnet_sdk;
using Xunit;
using Xunit.Abstractions;
using Asset = PlutoDAO.Gov.Worker.Entities.Asset;

namespace PlutoDAO.Gov.Worker.Test.Integration
{
    [Collection("Stellar collection")]
    [TestCaseOrderer("PlutoDAO.Gov.Worker.Test.Integration.Helpers.AlphabeticalOrderer", "PlutoDAO.Gov.Worker.Test.Integration")]
    public class WorkerTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public WorkerTest(StellarFixture fixture, ITestOutputHelper testOutputHelper)
        {
            Config = fixture.Config;
            _testOutputHelper = testOutputHelper;
        }

        private TestConfiguration Config { get; }

        [Fact]
        public async Task Test_00_Worker_Saves_Voting_Result_In_Results_Account()
        {
            var client = new HttpClient();

            var proposal1 =
                $@"{{""name"": ""Proposal1NameTest"", ""description"": ""A testing proposal"", ""creator"": ""{
                    Config.ProposalCreatorPublic
                }"", ""deadline"": ""2030-11-19T16:08:19.290Z"", ""whitelistedAssets"": [{{""asset"": {{ ""isNative"": true, ""code"": ""XLM"", ""issuer"": ""{
                    ""
                }""}}, ""multiplier"": ""1""}}]}}";

            var proposal2 =
                $@"{{""name"": ""Proposal1NameTest"", ""description"": ""A testing proposal"", ""creator"": ""{
                    Config.ProposalCreatorPublic
                }"", ""deadline"": ""2030-11-19T16:08:19.290Z"", ""whitelistedAssets"": [{{""asset"": {{ ""isNative"": true, ""code"": ""XLM"", ""issuer"": ""{
                    ""
                }""}}, ""multiplier"": ""1""}}]}}";

            var proposal3 =
                $@"{{""name"": ""Proposal1NameTest"", ""description"": ""A testing proposal"", ""creator"": ""{
                    Config.ProposalCreatorPublic
                }"", ""deadline"": ""2030-11-19T16:08:19.290Z"", ""whitelistedAssets"": [{{""asset"": {{ ""isNative"": true, ""code"": ""XLM"", ""issuer"": ""{
                    ""
                }""}}, ""multiplier"": ""1""}}]}}";

            await PlutoDAOHelper.ChangeBlockchainServerTime(client, "+1d");
            await ProposalHelper.SaveProposal(proposal1, Config.PlutoDAOReceiverKeyPair, Config.PlutoDAOSenderKeyPair);

            await PlutoDAOHelper.ChangeBlockchainServerTime(client, "+2d");
            await ProposalHelper.SaveProposal(proposal2, Config.PlutoDAOReceiverKeyPair, Config.PlutoDAOSenderKeyPair);

            await PlutoDAOHelper.ChangeBlockchainServerTime(client, "+3d");
            await ProposalHelper.SaveProposal(proposal3, Config.PlutoDAOReceiverKeyPair, Config.PlutoDAOSenderKeyPair);

            var xlm = new Asset(new AccountAddress(""), "XLM", true);
            var pnt = new Asset(new AccountAddress(Config.PntAssetIssuerPublic), "PNT");
            var pntAsset = StellarHelper.CreateAsset("PNT", Config.PntAssetIssuerPublic);

            await StellarHelper.Pay(Config.PntAssetIssuerKeyPair, Config.Trader1KeyPair, pntAsset, 10000m);
            await StellarHelper.Pay(Config.PntAssetIssuerKeyPair, Config.Trader2KeyPair, pntAsset, 10000m);

            await StellarHelper.OfferToSellAssetForXlm(
                pntAsset,
                Config.Trader1KeyPair,
                "10",
                "5"
            );

            await StellarHelper.OfferToBuyAssetForXlm(
                pntAsset,
                Config.Trader2KeyPair,
                "10",
                "5"
            );

            await StellarHelper.Pay(Config.PntAssetIssuerKeyPair, Config.VoterKeyPair, pntAsset, 10000m);
            await StellarHelper.CreateTrustLine(pntAsset, Config.PlutoDAOEscrowKeyPair);

            await VoteHelper.CastVote(Config.VoterKeyPair, Config.PlutoDAOEscrowKeyPair, "PROP1",
                new Vote(Config.VoterPublic, new Option("FOR"), xlm, 50m));

            await VoteHelper.CastVote(Config.VoterKeyPair, Config.PlutoDAOEscrowKeyPair, "PROP2",
                new Vote(Config.VoterPublic, new Option("FOR"), xlm, 50m));

            await VoteHelper.CastVote(Config.VoterKeyPair, Config.PlutoDAOEscrowKeyPair, "PROP2",
                new Vote(Config.VoterPublic, new Option("FOR"), pnt, 50m));

            await VoteHelper.CastVote(Config.VoterKeyPair, Config.PlutoDAOEscrowKeyPair, "PROP2",
                new Vote(Config.VoterPublic, new Option("AGAINST"), xlm, 50m));

            await VoteHelper.CastVote(Config.VoterKeyPair, Config.PlutoDAOEscrowKeyPair, "PROP3",
                new Vote(Config.VoterPublic, new Option("FOR"), xlm, 50m));


            Worker.Server = new Server(Config.TestHorizonUrl);
            Worker.DateTimeProvider = new DateTimeProvider(DateTime.Today.AddDays(33));
            Worker.WebDownloader = new WebDownloaderMock(Config.PntAssetIssuerPublic);
            await Worker.Run();

            var resultsAccountBalance = await StellarHelper.GetAccountBalance(Config.PlutoDAOResultsKeyPair);

            Assert.Equal("1.0000000",
                resultsAccountBalance.Single(balance => balance.AssetCode == "PROP2").BalanceString);
        }

        [Fact]
        public async Task Test_01_Worker_Only_Creates_Trustline_For_Draw_In_Results_Account()
        {
            var client = new HttpClient();

            var proposal =
                $@"{{""name"": ""Proposal4NameTest"", ""description"": ""A testing proposal"", ""creator"": ""{
                    Config.ProposalCreatorPublic
                }"", ""deadline"": ""2030-11-19T16:08:19.290Z"", ""whitelistedAssets"": [{{""asset"": {{ ""isNative"": true, ""code"": ""XLM"", ""issuer"": ""{
                    ""
                }""}}, ""multiplier"": ""1""}}]}}";
            
            await PlutoDAOHelper.ChangeBlockchainServerTime(client, "+5d");
            await ProposalHelper.SaveProposal(proposal, Config.PlutoDAOReceiverKeyPair, Config.PlutoDAOSenderKeyPair);
            
            var xlm = new Asset(new AccountAddress(""), "XLM", true);
            
            await VoteHelper.CastVote(Config.VoterKeyPair, Config.PlutoDAOEscrowKeyPair, "PROP4",
                new Vote(Config.VoterPublic, new Option("FOR"), xlm, 50m));

            await VoteHelper.CastVote(Config.VoterKeyPair, Config.PlutoDAOEscrowKeyPair, "PROP4",
                new Vote(Config.VoterPublic, new Option("AGAINST"), xlm, 50m));
            
            Worker.Server = new Server(Config.TestHorizonUrl);
            Worker.DateTimeProvider = new DateTimeProvider(DateTime.Today.AddDays(36));
            Worker.WebDownloader = new WebDownloaderMock(Config.PntAssetIssuerPublic);
            await Worker.Run();
            
            var resultsAccountBalance = await StellarHelper.GetAccountBalance(Config.PlutoDAOResultsKeyPair);

            Assert.Equal("0.0000000",
                resultsAccountBalance.Single(balance => balance.AssetCode == "PROP4").BalanceString);
        }
    }
}
