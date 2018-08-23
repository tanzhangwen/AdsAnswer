//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="BaseCrawler.cs">
//      Created by: tomtan at 7/13/2015 10:06:53 AM
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
    using System.IO;
    using System.Net;

    public class BaseCrawler
    {
        protected FeedUriTypes UriType;
        protected string URI;
        protected string LocalPath;
        protected DateTime LocalVersion;
        protected string UserAgent;
        public DateTime RemoteVersion;
        public Encoding Encoding = Encoding.Default;
        public WebProxy ManualProxy = null;
        public ProxyType proxyType = ProxyType.NULL;

        public static BaseCrawler CreateCralwer(FeedUriTypes uriType, string uri, Encoding encoding, string localPath, DateTime localVersion, string ua = null, ProxyType proxyType = ProxyType.NULL)
        {
            BaseCrawler crawler = BaseCrawler.CreateCralwer(uriType, uri, localPath, localVersion, ua, proxyType);
            crawler.Encoding = encoding;

            return crawler;
        }

        public static BaseCrawler CreateCralwer(FeedUriTypes uriType, string uri, string localPath, DateTime localVersion, string ua = null, ProxyType proxyType = ProxyType.NULL)
        {
            BaseCrawler crawler = null;
            switch (uriType)
            {
                case FeedUriTypes.Http:  // "http://"
                    crawler = new HttpCrawler(uriType, uri, localPath, localVersion, ua, proxyType);
                    break;
                case FeedUriTypes.Ftp:   // "ftp://"
                    // crawler = new FtpCrawler(uriType, uri, localPath, localVersion);
                    throw new NotImplementedException();
                case FeedUriTypes.SearchGold:    // //depot/
                    // crawler = new SearchGoldCrawler(uriType, uri, localPath, localVersion);
                    throw new NotImplementedException();
                case FeedUriTypes.ShareFolder:   // \\\\machine\\
                    crawler = new FileCrawler(uriType, uri, localPath, localVersion);
                    break;
                case FeedUriTypes.Cosmos:   // "https://cosmos08.osdinfra.net/cosmos/"
                    crawler = new CosmosCrawler(uriType, uri, localPath, localVersion);
                    break;
                default:
                    throw new NotImplementedException();
            }
            crawler.UserAgent = ua;
            return crawler;
        }

        public static FeedUriTypes GetUriTypesByUrl(string uri)
        {
            if (uri.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase))
            {
                return FeedUriTypes.Http;
            }
            else if (uri.StartsWith("ftp://", StringComparison.InvariantCultureIgnoreCase))
            {
                return FeedUriTypes.Ftp;
            }
            else if (uri.StartsWith("\\\\", StringComparison.InvariantCultureIgnoreCase))
            {
                return FeedUriTypes.ShareFolder;
            }
            else if (uri.StartsWith("//depot/", StringComparison.InvariantCultureIgnoreCase))
            {
                return FeedUriTypes.SearchGold;
            }
            else if (uri.StartsWith(@"https://cosmos08.osdinfra.net/cosmos/", StringComparison.InvariantCultureIgnoreCase))
            {
                return FeedUriTypes.Cosmos;
            }
            else
            {
                return FeedUriTypes.SearchGold;
            }
        }

        public BaseCrawler(FeedUriTypes uriType, string uri, string localPath, DateTime localVersion, string ua = null, ProxyType proxyType = ProxyType.NULL)
        {
            this.UriType = uriType;
            this.URI = uri;
            this.LocalPath = localPath;
            this.LocalVersion = localVersion;
            this.RemoteVersion = DateTime.MinValue;
            this.UserAgent = ua;
            this.proxyType = proxyType;
        }

        public BaseCrawler(FeedUriTypes uriType, string uri, Encoding encoding, string localPath, DateTime localVersion, string ua = null, ProxyType proxyType = ProxyType.NULL)
        {
            this.UriType = uriType;
            this.URI = uri;
            this.LocalPath = localPath;
            this.LocalVersion = localVersion;
            this.RemoteVersion = DateTime.MinValue;
            this.Encoding = encoding;
            this.UserAgent = ua;
            this.proxyType = proxyType;
        }

        public virtual BuildResults Crawl()
        {
            return BuildResults.Crawler_Succeed;
        }

        /// <summary>
        /// create the target folder if necessary and then one random file
        /// </summary>
        /// <returns>random file name</returns>
        protected string PrepareFolderAndRandomFile()
        {
            string dir = Path.GetDirectoryName(this.LocalPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            string filename = Path.GetRandomFileName();
            string randomFilePath = Path.Combine(dir, filename);
            return randomFilePath;
        }
    }
}
