using System.Net;

namespace PlutoDAO.Gov.Worker.WebDownloader
{
    public class WebDownloader : IWebDownloader
    {
        public string Get(string url)
        {
            return new WebClient().DownloadString(url);
        }
    }
}
