using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using stellar_dotnet_sdk;

namespace PlutoDAO.Gov.Worker.Test.Integration.Helpers
{
    public static class NetworkSetup
    {
        public static async Task Setup(Server server, TestConfiguration configuration)
        {
            StellarHelper.Server = server;
            StellarHelper.MasterAccount = KeyPair.FromSecretSeed(configuration.MasterAccountPrivate);
            ProposalHelper.Server = server;
            VoteHelper.Server = server;

            await CreatePlutoDAOAccounts(configuration);
            await CreateUserAccounts(configuration);

            PrintConfigurationValues(
                new[]
                {
                    configuration.PlutoDAOSenderKeyPair,
                    configuration.PlutoDAOReceiverKeyPair,
                    configuration.PlutoDAOEscrowKeyPair,
                    configuration.PlutoDAOResultsKeyPair,
                    configuration.ProposalCreatorKeyPair,
                    configuration.VoterKeyPair,
                    configuration.Trader1KeyPair,
                    configuration.Trader2KeyPair,
                    configuration.PntAssetIssuerKeyPair
                },
                new[]
                {
                    TestConfiguration.PlutoDAOSenderConfigKey,
                    TestConfiguration.PlutoDAOReceiverConfigKey,
                    TestConfiguration.PlutoDAOEscrowConfigKey,
                    TestConfiguration.PlutoDAOResultsConfigKey,
                    TestConfiguration.ProposalCreatorConfigKey,
                    TestConfiguration.VoterConfigKey,
                    TestConfiguration.Trader1ConfigKey,
                    TestConfiguration.Trader2ConfigKey,
                    TestConfiguration.PntAssetIssuerConfigKey
                },
                configuration.BaseConfigFile
            );
        }

        private static async Task CreatePlutoDAOAccounts(TestConfiguration configuration)
        {
            // PLUTODAO SENDER
            configuration.PlutoDAOSenderKeyPair = await StellarHelper.GetOrCreateAccountKeyPair(
                TestConfiguration.PlutoDAOSenderConfigKey,
                "Main PlutoDAO Sender Account", configuration.PlutoDAOSenderPrivate
            );
            configuration.PlutoDAOSenderPublic = configuration.PlutoDAOSenderKeyPair.AccountId;
            configuration.PlutoDAOSenderPrivate = configuration.PlutoDAOSenderKeyPair.SecretSeed;

            // PLUTODAO RECEIVER
            configuration.PlutoDAOReceiverKeyPair = await StellarHelper.GetOrCreateAccountKeyPair(
                TestConfiguration.PlutoDAOReceiverConfigKey,
                "Main PlutoDAO Receiver Account", configuration.PlutoDAOReceiverPrivate
            );
            configuration.PlutoDAOReceiverPublic = configuration.PlutoDAOReceiverKeyPair.AccountId;
            configuration.PlutoDAOReceiverPrivate = configuration.PlutoDAOReceiverKeyPair.SecretSeed;

            // PLUTODAO ESCROW
            configuration.PlutoDAOEscrowKeyPair = await StellarHelper.GetOrCreateAccountKeyPair(
                TestConfiguration.PlutoDAOEscrowConfigKey, "PlutoDAO escrow account",
                configuration.PlutoDAOEscrowPrivate);
            configuration.PlutoDAOEscrowPublic = configuration.PlutoDAOEscrowKeyPair.AccountId;
            configuration.PlutoDAOEscrowPrivate = configuration.PlutoDAOEscrowKeyPair.SecretSeed;

            // PLUTODAO RESULTS
            configuration.PlutoDAOResultsKeyPair = await StellarHelper.GetOrCreateAccountKeyPair(
                TestConfiguration.PlutoDAOResultsConfigKey, "PlutoDAO escrow account",
                configuration.PlutoDAOResultsPrivate);
            configuration.PlutoDAOResultsPublic = configuration.PlutoDAOResultsKeyPair.AccountId;
            configuration.PlutoDAOResultsPrivate = configuration.PlutoDAOResultsKeyPair.SecretSeed;
        }

        private static async Task CreateUserAccounts(TestConfiguration configuration)
        {
            //PROPOSAL CREATOR
            configuration.ProposalCreatorKeyPair = await StellarHelper.GetOrCreateAccountKeyPair(
                TestConfiguration.ProposalCreatorConfigKey, "ProposalCreator account",
                configuration.ProposalCreatorPrivate);
            configuration.ProposalCreatorPublic = configuration.ProposalCreatorKeyPair.AccountId;
            configuration.ProposalCreatorPrivate = configuration.ProposalCreatorKeyPair.SecretSeed;

            //VOTER
            configuration.VoterKeyPair = await StellarHelper.GetOrCreateAccountKeyPair(
                TestConfiguration.VoterConfigKey, "Voter account",
                configuration.VoterPrivate);
            configuration.VoterPublic = configuration.VoterKeyPair.AccountId;
            configuration.VoterPrivate = configuration.VoterKeyPair.SecretSeed;

            //TRADER1
            configuration.Trader1KeyPair = await StellarHelper.GetOrCreateAccountKeyPair(
                TestConfiguration.Trader1ConfigKey, "Trader1 account",
                configuration.Trader1Private);
            configuration.Trader1Public = configuration.Trader1KeyPair.AccountId;
            configuration.Trader1Private = configuration.Trader1KeyPair.SecretSeed;

            //TRADER2
            configuration.Trader2KeyPair = await StellarHelper.GetOrCreateAccountKeyPair(
                TestConfiguration.Trader2ConfigKey, "Trader2 account",
                configuration.Trader2Private);
            configuration.Trader2Public = configuration.Trader2KeyPair.AccountId;
            configuration.Trader2Private = configuration.Trader2KeyPair.SecretSeed;

            //PNT ISSUER
            configuration.PntAssetIssuerKeyPair = await StellarHelper.GetOrCreateAccountKeyPair(
                TestConfiguration.PntAssetIssuerConfigKey, "PntAssetIssuer account",
                configuration.PntAssetIssuerPrivate);
            configuration.PntAssetIssuerPublic = configuration.PntAssetIssuerKeyPair.AccountId;
            configuration.PntAssetIssuerPrivate = configuration.PntAssetIssuerKeyPair.SecretSeed;
        }

        private static void PrintConfigurationValues(
            IReadOnlyCollection<KeyPair> accountKeyPairs,
            string[] descriptions,
            string baseConfigFile
        )
        {
            var template = File.ReadAllText(baseConfigFile);
            var configObject = JObject.Parse(template);

            for (var i = 0; i < accountKeyPairs.Count; i++)
            {
                var configKeyBase = descriptions.ElementAt(i);
                var publicKeyConfigKey = $"{configKeyBase}_PUBLIC_KEY";
                var privateKeyConfigKey = $"{configKeyBase}_PRIVATE_KEY";
                var keyPair = accountKeyPairs.ElementAt(i);
                var publicKey = keyPair.AccountId;
                var privateKey = keyPair.SecretSeed;
                Environment.SetEnvironmentVariable(publicKeyConfigKey, publicKey);
                Environment.SetEnvironmentVariable(privateKeyConfigKey, privateKey);
                Console.WriteLine($"\"{publicKeyConfigKey}\" : \"{publicKey}\",");
                Console.WriteLine($"\"{privateKeyConfigKey}\" : \"{privateKey}\",");
                configObject.Add(publicKeyConfigKey, publicKey);
                configObject.Add(privateKeyConfigKey, privateKey);
            }

            File.WriteAllText(@"./appsettings.test-result.json", configObject.ToString());
        }
    }
}
