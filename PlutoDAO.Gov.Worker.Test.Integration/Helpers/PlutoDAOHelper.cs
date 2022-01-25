using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PlutoDAO.Gov.Worker.Test.Integration.Helpers
{
    public static class PlutoDAOHelper
    {
        public static async Task ChangeBlockchainServerTime(HttpClient client, string date)
        {
            var dateRequestContent = $@"{{""FAKETIME"": ""{date}""}}";
            var data = new StringContent(dateRequestContent, Encoding.UTF8, "application/json");
            var requestUri = "http://localhost:5555/";
            var response = await client.PostAsync(requestUri, data);
            await response.Content.ReadAsStringAsync();
        }
    }
}
