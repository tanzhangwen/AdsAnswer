//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="FileCrawler.cs">
//      Created by: tomtan at 7/13/2015 10:40:00 AM
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
    using AdsAnswer;
    using AdsAnswer.Utility;

    public class FileCrawler : BaseCrawler
    {
        public FileCrawler(FeedUriTypes uriType, string uri, string localPath, DateTime localVersion)
            : base(uriType, uri, localPath, localVersion)
        {
        }

        public override BuildResults Crawl()
        {
            if (File.Exists(base.URI))
            {
                string randomFilePath = this.PrepareFolderAndRandomFile();

                BuildResults result = CrawlInternal(randomFilePath);
                switch (result)
                {
                    case BuildResults.Crawler_Succeed:
                        FileHelper.MoveFile(randomFilePath, this.LocalPath);
                        break;
                    default:
                        break;
                }

                return result;
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
                this.RemoteVersion = File.GetLastWriteTime(base.URI);
                if (RemoteVersion <= LocalVersion)
                {
                    return BuildResults.Crawler_NoNewFileFound;
                }
                else
                {
                    if (FileHelper.CopyFile(base.URI, filename))
                        return BuildResults.Crawler_Succeed;
                    else
                        return BuildResults.Crawler_CannotCopyRemoteFile;
                }
            }
            catch (Exception)
            {
                //TraceLog.WriteLine(EventType.Verbose, ex.Message);
                return BuildResults.Crawler_UnexpectedError;
            }
        }
    }
}
