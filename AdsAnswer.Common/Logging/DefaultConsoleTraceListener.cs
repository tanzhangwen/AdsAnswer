//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="DefaultConsoleTraceListener.cs">
//      Created by: tomtan at 7/10/2015 12:10:40 PM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.Logging
{
    using System;
    using System.Diagnostics;

    public class DefaultConsoleTraceListener : ConsoleTraceListener
    {
        public DefaultConsoleTraceListener()
        {
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            SetConsoleColor(eventType);
            string timeStr = TraceLog.GetDateTimeFormatString(eventCache.DateTime);
            Console.WriteLine("{0} : {1}", timeStr, message);
            Console.ResetColor();
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            SetConsoleColor(eventType);
            string message = string.Format(format, args);
            string timeStr = TraceLog.GetDateTimeFormatString(eventCache.DateTime);
            Console.WriteLine("{0} : {1}", timeStr, message);
            Console.ResetColor();
        }

        private void SetConsoleColor(TraceEventType eventType)
        {
            switch (eventType)
            {
                case TraceEventType.Critical:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case TraceEventType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case TraceEventType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case TraceEventType.Information:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case TraceEventType.Verbose:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                default:    
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
        }
    }
}
