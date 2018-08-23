//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="ConfigStore.cs">
//      Created by: tomtan at 7/10/2015 1:40:34 PM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.Config
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Search.Platform.Parallax;
    using System.IO;

    public class ConfigStore
    {
        public static string WorkingDir = @"d:\data\UsageOnboarding";
        private const string DefaultConfigFile = "Config.ini";


        private static ConfigStore instance = new ConfigStore();
        public static void CreateWorkingDir()
        {
            if (!Directory.Exists(WorkingDir))
                Directory.CreateDirectory(WorkingDir);
            if (!Directory.Exists(LogFolder))
                Directory.CreateDirectory(LogFolder);
        }

        public static string LogFolder
        {
            get
            {
                return Path.Combine(WorkingDir, "Logs");
            }
        }

    }
}
