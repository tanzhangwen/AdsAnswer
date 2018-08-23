//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="BaseObject.cs">
//      Created by: tomtan at 7/10/2015 10:47:09 AM
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
    using System.IO;

    public class BaseObject
    {
        public string Owner;
        public DateTime CreateTime;
        public DateTime LastBuildTime;
        public string BuildWorkingFolder;
        public string BuildHistoryFolder;
        public string LatestSucceedVersionFile = "LatestSucceedVersion.txt";
        public string LatestFailedVersionFile = "LatestFailedVersion.txt";

        public string GetWorkingFilePath(string fileName)
        {
            return Path.Combine(this.BuildWorkingFolder, fileName);
        }
    }
}
