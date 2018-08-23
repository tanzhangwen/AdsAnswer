//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="FeedBuilder.cs">
//      Created by: tomtan at 7/9/2015 4:22:38 PM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.AnswerBuilder
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AdsAnswer.AnswerData;
    using AdsAnswer.Logging;

    public class FeedBuilder
    {
        private Feed feed;

        TraceLog logger;

        public bool ReadConfigure()
        {
            Markets market = (Markets)Enum.Parse(typeof(Markets), "zhCN", true);
            feed = new Feed(market, "RAA", "DataPro");
            feed.LastBuildTime = DateTime.UtcNow;
            return true;
        }

        public void Build()
        {
            ReadConfigure();
            logger = new TraceLog(
                    feed.FullName, "log.txt", System.Diagnostics.SourceLevels.Information);
            ImageBuilder imageBuilder = new ImageBuilder(this.feed, this.WriteLog);
            imageBuilder.Build();
        }

        /// <summary>
        /// write log to db, file and console
        /// </summary>
        public void WriteLog(EventType eventType, string message)
        {
            if (this.logger != null)
            {
                this.logger.WriteLine(eventType, message);
            }

            //this.WriteJobLog(eventType, message);
        }
    }
}
