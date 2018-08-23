//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="HttpCrawler.cs">
//      Created by: tomtan at 7/13/2015 10:26:23 AM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.Crawler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Net;
    using System.Net.Cache;
    using System.IO;
    using AdsAnswer.Utility;

    public class HttpCrawler : BaseCrawler
    {
        public HttpCrawler(FeedUriTypes uriType, string uri, string localPath, DateTime localVersion, string ua = null, ProxyType proxyType = ProxyType.NULL)
            : base(uriType, uri, localPath, localVersion, ua, proxyType)
        {
        }

        public HttpCrawler(FeedUriTypes uriType, string uri, Encoding encoding, string localPath, DateTime localVersion, string ua = null, ProxyType proxyType = ProxyType.NULL)
            : base(uriType, uri, encoding, localPath, localVersion, ua, proxyType)
        {
        }

        public override BuildResults Crawl()
        {
            string randomFilePath = this.PrepareFolderAndRandomFile();

            Exception exception = null;

            //CustomHttpCrawler client = new CustomHttpCrawler(new Uri(base.URI), randomFilePath, this.Encoding, this.LocalVersion, this.UserAgent);

            using (CustomWebClient client = new CustomWebClient(new Uri(base.URI), randomFilePath, this.Encoding, this.LocalVersion, this.UserAgent, proxyType))
            {
                if (this.ManualProxy != null)
                {
                    client.ManualProxy = this.ManualProxy;
                }

                //[Bug 730427:DownloadFileActivity need return directly when set manully proxy and download failed]
                for (int count = 0; count < CustomWebClient.ProxyList.Count; count++)
                {
                    BuildResults result = client.DownloadFile();
                    this.RemoteVersion = client.RemoteVersion;
                    exception = client.Exception;
                    switch (result)
                    {
                        case BuildResults.Crawler_Succeed:
                            FileHelper.MoveFile(randomFilePath, this.LocalPath);
                            return BuildResults.Crawler_Succeed;

                        case BuildResults.Crawler_NoNewFileFound:
                            return result;

                        case BuildResults.Crawler_404NotFound:
                            throw exception;

                        case BuildResults.Crawler_RemoteServerNoResponse:
                        case BuildResults.Crawler_UnexpectedError:
                            //System.Threading.Thread.Sleep(5 * 1000);
                            break;
                        case BuildResults.Crawler_ProtocolError:
                            throw exception;
                    }
                }
            }

            if (exception == null)
            {
                return BuildResults.Crawler_UnexpectedError;
            }
            else
            {
                throw exception;
            }
        }
    }
}
