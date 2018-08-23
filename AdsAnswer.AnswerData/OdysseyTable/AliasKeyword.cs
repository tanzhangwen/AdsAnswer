//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="AliasKeyword.cs">
//      Created by: tomtan at 7/14/2015 3:17:14 PM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.AnswerData.OdysseyTable
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class AliasKeyword
    {
        public string Key;
        public float Score;
        public string ProviderName;
        public string UniqueKey { get { return string.Format("{0}{1}", this.Key, this.ProviderName); } }

        public AliasKeyword(string key, float score, string scenarioName)
        {
            this.Key = key;
            this.Score = score;
            this.ProviderName = scenarioName;
        }

        public static AliasKeyword FromString(string value)
        {
            string[] tmp = value.Split(new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
            if (tmp.Length == 3)    // 8寸戚风蛋糕^1^MeishiChina
            {
                float score = 1.0f;
                if (!float.TryParse(tmp[1], out score))
                    score = 1.0f;
                AliasKeyword r = new AliasKeyword(tmp[0], score, tmp[2]);
                return r;
            }

            return null;
        }

        public override string ToString()
        {
            return string.Format("{0}^{1}^{2}", this.Key, this.Score, this.ProviderName);
        }
    }
}
