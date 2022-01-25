using System;
using System.Threading.Tasks;
using PlutoDAO.Gov.Worker.Providers;
using stellar_dotnet_sdk;

namespace PlutoDAO.Gov.Worker
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var network = new Network(Environment.GetEnvironmentVariable("HORIZON_NETWORK_PASSPHRASE"));
            Network.Use(network);
            Worker.Server = new Server(Environment.GetEnvironmentVariable("HORIZON_URL"));
            Worker.DateTimeProvider = new DateTimeProvider(DateTime.Now);
            Worker.WebDownloader = new WebDownloader.WebDownloader();

            await Worker.Run();
        }
    }
}
