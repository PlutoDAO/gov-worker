using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using stellar_dotnet_sdk;

namespace PlutoDAO.Gov.Worker.Test.Integration.Helpers
{
    public class TestConfiguration
    {
        private const string MasterAccountConfigKey = "MASTER_ACCOUNT";
        public const string PlutoDAOSenderConfigKey = "PLUTODAO_PROPOSAL_SENDER_ACCOUNT";
        public const string PlutoDAOReceiverConfigKey = "PLUTODAO_PROPOSAL_RECEIVER_ACCOUNT";
        public const string PlutoDAOEscrowConfigKey = "PLUTODAO_ESCROW_ACCOUNT";
        public const string PlutoDAOResultsConfigKey = "PLUTODAO_RESULTS_ACCOUNT";
        public const string ProposalCreatorConfigKey = "TEST_PROPOSAL_CREATOR_ACCOUNT";
        public const string VoterConfigKey = "TEST_VOTER_ACCOUNT";
        public const string Trader1ConfigKey = "TEST_TRADER1_ACCOUNT";
        public const string Trader2ConfigKey = "TEST_TRADER2_ACCOUNT";
        public const string PntAssetIssuerConfigKey = "TEST_PNT_ASSET_ISSUER_ACCOUNT";

        public TestConfiguration()
        {
            var fileToLoad = "appsettings.staging.json";
            var baseConfigFile = "appsettings.staging.json.dist";

            if (File.Exists("appsettings.test.json"))
            {
                baseConfigFile = "appsettings.test.json.dist";
                fileToLoad = "appsettings.test.json";
            }

            if (File.Exists("appsettings.dev.json"))
            {
                baseConfigFile = "appsettings.dev.json.dist";
                fileToLoad = "appsettings.dev.json";
            }

            ConfigFile = fileToLoad;
            BaseConfigFile = baseConfigFile;
            Console.WriteLine($"Loading file: {ConfigFile}");
            Console.WriteLine($"Base config file: {BaseConfigFile}");

            var configuration = new ConfigurationBuilder()
                .AddJsonFile(fileToLoad, false, true)
                .Build();

            var environment = configuration.GetValue<string>("ENVIRONMENT");
            Environment.SetEnvironmentVariable("ENVIRONMENT", environment);

            var passphrase = configuration.GetValue<string>("HORIZON_NETWORK_PASSPHRASE");
            Network.Use(new Network(passphrase));
            Environment.SetEnvironmentVariable("HORIZON_NETWORK_PASSPHRASE", passphrase);

            var baseFeeInXlm = configuration.GetValue<string>("BASE_FEE_IN_XLM");
            Environment.SetEnvironmentVariable("BASE_FEE_IN_XLM", baseFeeInXlm);

            TestHorizonUrl = configuration.GetValue<string>("HORIZON_URL");
            Environment.SetEnvironmentVariable("HORIZON_URL", TestHorizonUrl);

            var daysSinceProposalCreation = configuration.GetValue<string>("DAYS_SINCE_PROPOSAL_CREATION");
            Environment.SetEnvironmentVariable("DAYS_SINCE_PROPOSAL_CREATION", daysSinceProposalCreation);

            MasterAccountPublic = configuration.GetValue<string>(GetPublicConfigKey(MasterAccountConfigKey));
            Environment.SetEnvironmentVariable(GetPublicConfigKey(MasterAccountConfigKey), MasterAccountPublic);
            MasterAccountPrivate = configuration.GetValue<string>(GetPrivateConfigKey(MasterAccountConfigKey));
            Environment.SetEnvironmentVariable(GetPrivateConfigKey(MasterAccountConfigKey), MasterAccountPrivate);
            KeyPair.FromSecretSeed(MasterAccountPrivate);

            PlutoDAOSenderPublic = configuration.GetValue<string>(GetPublicConfigKey(PlutoDAOSenderConfigKey));
            Environment.SetEnvironmentVariable(GetPublicConfigKey(PlutoDAOSenderConfigKey), PlutoDAOSenderPublic);
            PlutoDAOSenderPrivate = configuration.GetValue<string>(GetPrivateConfigKey(PlutoDAOSenderConfigKey));
            Environment.SetEnvironmentVariable(GetPrivateConfigKey(PlutoDAOSenderConfigKey), PlutoDAOSenderPrivate);
            if (PlutoDAOSenderPrivate != null)
                PlutoDAOSenderKeyPair = KeyPair.FromSecretSeed(PlutoDAOSenderPrivate);

            PlutoDAOReceiverPublic = configuration.GetValue<string>(GetPublicConfigKey(PlutoDAOReceiverConfigKey));
            Environment.SetEnvironmentVariable(GetPublicConfigKey(PlutoDAOReceiverConfigKey), PlutoDAOReceiverPublic);
            PlutoDAOReceiverPrivate = configuration.GetValue<string>(GetPrivateConfigKey(PlutoDAOReceiverConfigKey));
            Environment.SetEnvironmentVariable(GetPrivateConfigKey(PlutoDAOReceiverConfigKey), PlutoDAOReceiverPrivate);
            if (PlutoDAOReceiverPrivate != null)
                PlutoDAOReceiverKeyPair = KeyPair.FromSecretSeed(PlutoDAOReceiverPrivate);

            ProposalCreatorPublic = configuration.GetValue<string>(GetPublicConfigKey(ProposalCreatorConfigKey));
            Environment.SetEnvironmentVariable(GetPublicConfigKey(ProposalCreatorConfigKey), ProposalCreatorPublic);
            ProposalCreatorPrivate = configuration.GetValue<string>(GetPrivateConfigKey(ProposalCreatorConfigKey));
            Environment.SetEnvironmentVariable(GetPrivateConfigKey(ProposalCreatorConfigKey), ProposalCreatorPrivate);
            if (ProposalCreatorPrivate != null)
                ProposalCreatorKeyPair = KeyPair.FromSecretSeed(ProposalCreatorPrivate);

            PlutoDAOEscrowPublic = configuration.GetValue<string>(GetPublicConfigKey(PlutoDAOEscrowConfigKey));
            Environment.SetEnvironmentVariable(GetPublicConfigKey(PlutoDAOEscrowConfigKey), PlutoDAOEscrowPublic);
            PlutoDAOEscrowPrivate = configuration.GetValue<string>(GetPrivateConfigKey(PlutoDAOEscrowConfigKey));
            Environment.SetEnvironmentVariable(GetPrivateConfigKey(PlutoDAOEscrowConfigKey), PlutoDAOEscrowPrivate);
            if (PlutoDAOEscrowPrivate != null)
                PlutoDAOEscrowKeyPair = KeyPair.FromSecretSeed(PlutoDAOEscrowPrivate);

            PlutoDAOResultsPublic = configuration.GetValue<string>(GetPublicConfigKey(PlutoDAOResultsConfigKey));
            Environment.SetEnvironmentVariable(GetPublicConfigKey(PlutoDAOResultsConfigKey), PlutoDAOResultsPublic);
            PlutoDAOResultsPrivate = configuration.GetValue<string>(GetPrivateConfigKey(PlutoDAOResultsConfigKey));
            Environment.SetEnvironmentVariable(GetPrivateConfigKey(PlutoDAOResultsConfigKey), PlutoDAOResultsPrivate);
            if (PlutoDAOResultsPrivate != null)
                PlutoDAOResultsKeyPair = KeyPair.FromSecretSeed(PlutoDAOResultsPrivate);

            VoterPublic = configuration.GetValue<string>(GetPublicConfigKey(VoterConfigKey));
            Environment.SetEnvironmentVariable(GetPublicConfigKey(VoterConfigKey), VoterPublic);
            VoterPrivate = configuration.GetValue<string>(GetPrivateConfigKey(VoterConfigKey));
            Environment.SetEnvironmentVariable(GetPrivateConfigKey(VoterConfigKey), VoterPrivate);
            if (VoterPrivate != null)
                VoterKeyPair = KeyPair.FromSecretSeed(VoterPrivate);

            Trader1Public = configuration.GetValue<string>(GetPublicConfigKey(Trader1ConfigKey));
            Environment.SetEnvironmentVariable(GetPublicConfigKey(Trader1ConfigKey), Trader1Public);
            Trader1Private = configuration.GetValue<string>(GetPrivateConfigKey(Trader1ConfigKey));
            Environment.SetEnvironmentVariable(GetPrivateConfigKey(Trader1ConfigKey), Trader1Private);
            if (Trader1Private != null)
                Trader1KeyPair = KeyPair.FromSecretSeed(Trader1Private);

            Trader2Public = configuration.GetValue<string>(GetPublicConfigKey(Trader2ConfigKey));
            Environment.SetEnvironmentVariable(GetPublicConfigKey(Trader2ConfigKey), Trader2Public);
            Trader2Private = configuration.GetValue<string>(GetPrivateConfigKey(Trader2ConfigKey));
            Environment.SetEnvironmentVariable(GetPrivateConfigKey(Trader2ConfigKey), Trader2Private);
            if (Trader2Private != null)
                Trader2KeyPair = KeyPair.FromSecretSeed(Trader2Private);

            PntAssetIssuerPublic = configuration.GetValue<string>(GetPublicConfigKey(PntAssetIssuerConfigKey));
            Environment.SetEnvironmentVariable(GetPublicConfigKey(PntAssetIssuerConfigKey), PntAssetIssuerPublic);
            PntAssetIssuerPrivate = configuration.GetValue<string>(GetPrivateConfigKey(PntAssetIssuerConfigKey));
            Environment.SetEnvironmentVariable(GetPrivateConfigKey(PntAssetIssuerConfigKey), PntAssetIssuerPrivate);
            if (PntAssetIssuerPrivate != null)
                PntAssetIssuerKeyPair = KeyPair.FromSecretSeed(PntAssetIssuerPrivate);
        }

        public string ConfigFile { get; }
        public string BaseConfigFile { get; }
        public string MasterAccountPrivate { get; }
        private string MasterAccountPublic { get; }
        public string PlutoDAOSenderPublic { get; set; }
        public string? PlutoDAOSenderPrivate { get; set; }
        public KeyPair PlutoDAOSenderKeyPair { get; set; } = null!;
        public string PlutoDAOReceiverPublic { get; set; }
        public string? PlutoDAOReceiverPrivate { get; set; }
        public KeyPair PlutoDAOReceiverKeyPair { get; set; } = null!;
        public string PlutoDAOEscrowPublic { get; set; }
        public string PlutoDAOEscrowPrivate { get; set; }
        public KeyPair PlutoDAOEscrowKeyPair { get; set; } = null!;
        public string PlutoDAOResultsPublic { get; set; }
        public string PlutoDAOResultsPrivate { get; set; }
        public KeyPair PlutoDAOResultsKeyPair { get; set; } = null!;
        public string ProposalCreatorPublic { get; set; }
        public string ProposalCreatorPrivate { get; set; }
        public KeyPair ProposalCreatorKeyPair { get; set; } = null!;
        public string VoterPublic { get; set; }
        public string VoterPrivate { get; set; }
        public KeyPair VoterKeyPair { get; set; } = null!;
        public string Trader1Public { get; set; }
        public string Trader1Private { get; set; }
        public KeyPair Trader1KeyPair { get; set; } = null!;
        public string Trader2Public { get; set; }
        public string Trader2Private { get; set; }
        public KeyPair Trader2KeyPair { get; set; } = null!;
        public string PntAssetIssuerPublic { get; set; }
        public string? PntAssetIssuerPrivate { get; set; }
        public KeyPair PntAssetIssuerKeyPair { get; set; } = null!;
        public string TestHorizonUrl { get; }

        private static string GetPrivateConfigKey(string baseString)
        {
            return $"{baseString}_PRIVATE_KEY";
        }

        private static string GetPublicConfigKey(string baseString)
        {
            return $"{baseString}_PUBLIC_KEY";
        }
    }
}
