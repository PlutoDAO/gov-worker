using System;
using PlutoDAO.Gov.Worker.Test.Integration.Helpers;
using stellar_dotnet_sdk;

namespace PlutoDAO.Gov.Worker.Test.Integration.Fixtures
{
    public class StellarFixture : IDisposable
    {
        public StellarFixture()
        {
            Console.WriteLine("Running collection filter");

            Config = new TestConfiguration();
            var server = new Server(Config.TestHorizonUrl);

            NetworkSetup.Setup(server, Config).GetAwaiter().GetResult();
        }

        public TestConfiguration Config { get; }

        public void Dispose()
        {
        }
    }
}
