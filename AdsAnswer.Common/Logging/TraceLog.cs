//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="TraceLog.cs">
//      Created by: tomtan at 7/10/2015 12:07:01 PM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Diagnostics;
    using System.IO;

    public class TraceLog
    {
        TraceSource ts;
        int id = 1;
        object _locker;

        public TraceLog(string scenarioName, string logFilePath, SourceLevels level = SourceLevels.All)
        {
            this._locker = new object();
            ts = new TraceSource(scenarioName, SourceLevels.All);
            ts.Listeners.Remove("Default");

            this.AddConsoleOutputListener();
            this.AddLogFileListener(logFilePath, level);
        }

        public void Close()
        {
            lock (_locker)
            {
                ts.Flush();
                ts.Close();
            }
        }

        /// <summary>
        /// add console listener
        /// </summary>
        private void AddConsoleOutputListener(SourceLevels level = SourceLevels.All)
        {
            DefaultConsoleTraceListener console = new DefaultConsoleTraceListener();
            console.Filter = new EventTypeFilter(level);
            ts.Listeners.Add(console);
        }

        /// <summary>
        /// add text writer to log file
        /// </summary>
        private void AddLogFileListener(string logFilePath, SourceLevels level = SourceLevels.All)
        {
            DefaultLogTraceListener log = new DefaultLogTraceListener(logFilePath);
            log.Filter = new EventTypeFilter(level);
            ts.Listeners.Add(log);
        }

        internal static string GetLogFileTimeStamp(DateTime dt)
        {
            /*
            DateTime local = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dt, timezoneId);
            return local.ToString("yyyyMMddHHmmss");
             */
            return dt.ToString("yyyyMMddHHmmss");
        }

        public static string GetDateTimeFormatString(DateTime dt)
        {
            /*
            DateTime local = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dt, timezoneId);
            return local.ToString("yyyy-MM-dd_HH:mm:ss");
             */
            return dt.ToString("yyyy-MM-dd_HH:mm:ss");
        }

        public void WriteLine(EventType type, string str)
        {
            lock (_locker)
            {
                TraceEventType tet = (TraceEventType)type;
                ts.TraceEvent(tet, id++, str);
                ts.Flush();
            }
        }

        public void WriteLine(EventType type, string format, params object[] args)
        {
            lock (_locker)
            {
                TraceEventType tet = (TraceEventType)type;
                ts.TraceEvent(tet, id++, format, args);
                ts.Flush();
            }
        }

        public void WriteLine(EventType type, Exception e)
        {
            StringBuilder sb = new StringBuilder();
            if (e != null)
            {
                sb.AppendLine(e.Message);
                sb.AppendLine(e.StackTrace);
                if (e.InnerException != null)
                {
                    sb.AppendLine(e.InnerException.Message);
                    sb.AppendLine(e.InnerException.StackTrace);
                }
            }

            lock (_locker)
            {
                TraceEventType tet = (TraceEventType)type;
                ts.TraceEvent(tet, id++, sb.ToString());
                ts.Flush();
            }
        }
    }
}
