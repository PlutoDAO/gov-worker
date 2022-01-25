using PlutoDAO.Gov.Worker.WebDownloader;

namespace PlutoDAO.Gov.Worker.Test.Integration.Mocks
{
    public class WebDownloaderMock : IWebDownloader
    {
        public WebDownloaderMock(string pntAssetIssuer)
        {
            PntAssetIssuer = pntAssetIssuer;
        }

        private string PntAssetIssuer { get; }

        public string Get(string url)
        {
            return
                $@"{{name:""Proposal1NameTest"",description:""A testing proposal"",creator:""GA6NZK4HD2SHSS3VCDZURHXRHBTMAEB2RTEMUKNAQHY37H32QORVK22C"",deadline:""2030-11-19T13:08:19.29-03:00"",created:""2022-01-20T19:26:55.0406838-03:00"",whitelistedAssets:[{{asset:{{""isNative"":true,""code"":""XLM"",""issuer"":""""}},multiplier:1.0}},{{asset:{{isNative:false,code:""PNT"",issuer:""{
                    PntAssetIssuer
                }""}},multiplier:2.0}}], options:[{{name:""FOR""}},{{name:""AGAINST""}}]}}";
        }
    }
}
