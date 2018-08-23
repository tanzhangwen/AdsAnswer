//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="Logger.cs">
//      Created by: tomtan at 7/10/2015 12:13:35 PM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.Logging
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AdsAnswer.Config;

    public class Logger
    {
        public static void Init()
        {
            string buildName;
            try
            {
                buildName = System.Environment.CurrentDirectory;
                buildName = buildName.TrimEnd(new char[] { '\\' });
                buildName = buildName.Substring(buildName.LastIndexOf('\\') + 1);
            }
            catch
            {
                buildName = string.Empty;
            }

            LogFilePattern = string.Format("{0}_{1}_{2}.txt", buildName, System.Environment.MachineName, DateTime.UtcNow.ToFileTime());
        }

        private static string LogFilePattern = "Log.txt";

        static Lazy<TraceLog> TraceLog = new Lazy<TraceLog>(
            () =>
            {
                string logFile = Path.Combine(ConfigStore.LogFolder, LogFilePattern);
                try
                {
                    var files = Directory.GetFiles(ConfigStore.LogFolder);
                    foreach (var file in files)
                        if (DateTime.UtcNow - File.GetLastWriteTime(file) > new TimeSpan(10, 0, 0, 0))
                            File.Delete(file);
                }
                catch
                {
                }
                return new TraceLog("Logger", logFile, System.Diagnostics.SourceLevels.Information);
            }, LazyThreadSafetyMode.PublicationOnly);

        public static void Trace(string text)
        {
            // This level is just used for development. 
            // Write(text);
        }

        public static void Info(string component, string stage, string dataContext, string detail)
        {
            try
            {
                // [Compoent.Stage][DataContext] Details
                var text = string.Format("[{0}.{1}][{2}] {3}", component, stage, dataContext, detail);
                TraceLog.Value.WriteLine(EventType.Information, text);
            }
            catch { }
        }

        public static void Error(string component, string stage, string dataContext, string detail)
        {
            try
            {
                // [Compoent.Stage][DataContext] Details
                var text = string.Format("[{0}.{1}][{2}] {3}", component, stage, dataContext, detail);
                TraceLog.Value.WriteLine(EventType.Error, text);
            }
            catch { }
        }
    }
}
