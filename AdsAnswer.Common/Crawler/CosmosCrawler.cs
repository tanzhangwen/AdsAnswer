//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="CosmosCrawler.cs">
//      Created by: tomtan at 7/13/2015 10:41:36 AM
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
    using AdsAnswer.Utility;
    using AdsAnswer.Cosmos;

    public class CosmosCrawler : BaseCrawler
    {
        public CosmosCrawler(FeedUriTypes sourceType, string uri, string localPath, DateTime localVersion)
            : base(sourceType, uri, localPath, localVersion)
        {
        }

        public override BuildResults Crawl()
        {
            if (CosmosStream.StreamExists(base.URI))
            {
                string randomFilePath = this.PrepareFolderAndRandomFile();

                for (int count = 0; count < 3; count++)
                {
                    BuildResults result = CrawlInternal(randomFilePath);
                    switch (result)
                    {
                        case BuildResults.Crawler_Succeed:
                            FileHelper.MoveFile(randomFilePath, this.LocalPath);
                            return result;

                        case BuildResults.Crawler_NoNewFileFound:
                            return result;

                        default:
                            System.Threading.Thread.Sleep(1000);
                            break;
                    }
                }

                return BuildResults.Crawler_UnexpectedError;
            }
            else
            {
                return BuildResults.Crawler_CannotCopyRemoteFile;
            }
        }

        /// <summary>
        /// copy file from remote share folder to local download folder with random file name
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="filename"></param>
        /// <param name="localVersion"></param>
        /// <param name="remoteVersion"></param>
        /// <returns></returns>
        private BuildResults CrawlInternal(string filename)
        {
            try
            {
                this.RemoteVersion = CosmosStream.GetLastModifiedTime(base.URI);
                if (this.RemoteVersion <= this.LocalVersion)
                {
                    return BuildResults.Crawler_NoNewFileFound;
                }
                else
                {
                    CosmosStream.DownloadStream(base.URI, filename);
                    return BuildResults.Crawler_Succeed;
                }
            }
            catch
            {
                return BuildResults.Crawler_UnexpectedError;
            }
        }
    }
}
