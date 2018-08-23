//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="DefaultLogTraceListener.cs">
//      Created by: tomtan at 7/10/2015 12:12:27 PM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.Logging
{
    using System;
    using System.Diagnostics;
    using System.IO;

    public class DefaultLogTraceListener : TextWriterTraceListener
    {
        private string logFile = null;
        //string TimeZoneID;

        public DefaultLogTraceListener(string logFilePath)
            : base(logFilePath)
        {
            this.logFile = Path.GetFullPath(logFilePath);
            string folder = Path.GetDirectoryName(this.logFile);
            //this.TimeZoneID = timezoneId;
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (!this.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
                return;

            TraceReset();

            string prefix = GetPrefix(eventType);
            string timeStr = TraceLog.GetDateTimeFormatString(eventCache.DateTime.ToUniversalTime());
            this.Writer.WriteLine(string.Format("{0} : {1} : {2}", prefix, timeStr, message));
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            if (!this.Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
                return;

            TraceReset();

            string prefix = GetPrefix(eventType);
            string message = string.Format(format, args);

            string timeStr = TraceLog.GetDateTimeFormatString(eventCache.DateTime.ToUniversalTime());
            this.Writer.WriteLine(string.Format("{0} : {1} : {2}", prefix, timeStr, message));
        }

        public static string GetPrefix(TraceEventType eventType)
        {
            string prefix = null;
            switch (eventType)
            {
                case TraceEventType.Critical:
                    prefix = "C";
                    break;
                case TraceEventType.Error:
                    prefix = "E";
                    break;
                case TraceEventType.Warning:
                    prefix = "W";
                    break;
                case TraceEventType.Information:
                    prefix = "I";
                    break;
                case TraceEventType.Verbose:
                    prefix = "V";
                    break;
                default:
                    prefix = "A";
                    break;
            }
            return prefix;
        }

        private void TraceReset()
        {
            this.Writer.Flush();
            FileInfo info = new FileInfo(this.logFile);

            // File lenght > 8M, start a new file
            if (info.Length > 8 * 1024 * 1024)
            {
                try
                {
                    this.Writer.Close();
                    this.Writer.Dispose();
                    string folder = Path.GetDirectoryName(this.logFile);
                    string timestamp = TraceLog.GetLogFileTimeStamp(DateTime.UtcNow);
                    string filenoext = Path.GetFileNameWithoutExtension(this.logFile);
                    string ext = Path.GetExtension(this.logFile);
                    string filename = string.Format("{0}_{1}{2}", filenoext, timestamp, ext);
                    info.MoveTo(Path.Combine(folder, filename));
                }
                catch (Exception ex)
                {
                    this.Writer.Close();
                    this.Writer.Dispose();
                    if (File.Exists(this.logFile))
                    {
                        File.Move(this.logFile, this.logFile + "." + TraceLog.GetLogFileTimeStamp(DateTime.UtcNow));
                    }
                    Console.WriteLine(ex.ToString());
                }
                finally
                {
                    TextWriter writer = new StreamWriter(this.logFile);
                    this.Writer = writer;
                }
            }
        }
    }
}
