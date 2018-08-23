//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="Feed.cs">
//      Created by: tomtan at 7/10/2015 10:49:11 AM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.AnswerData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using AdsAnswer;

    public class Feed : BaseObject
    {
        public Markets Market;
        public string DomainName;
        public string ProviderName;
        public string Exclude;
        public float IntentionScore = 0.85f;

        public string AliasFile;
        public string ManualAliasFile;
        public string AliasPatternFile;
        public string BlackListFile;

        public string FullName
        {
            get
            {
                return string.Format("{0}.{1}.{2}", this.Market, this.DomainName, this.ProviderName);
            }
        }

        public Feed() { }

        public Feed(Markets mkt, string domainName, string providerName, bool ifSetDefaultValue = true)
        {
            this.Market = mkt;
            this.DomainName = domainName;
            this.ProviderName = providerName;

            if(ifSetDefaultValue)
            {
                base.BuildWorkingFolder = @"D:\\data\" + DomainName;
                //TO DO
            }
        }
    }
}
