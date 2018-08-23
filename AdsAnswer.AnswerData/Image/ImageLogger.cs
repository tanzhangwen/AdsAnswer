//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="ImageLogger.cs">
//      Created by: tomtan at 7/13/2015 1:16:03 PM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.AnswerData.Image
{
    using System;
    using AdsAnswer.Logging;

    public class ImageLogger
    {
        public static void LogMessage(TraceLog logger, EventType type, string format, params object[] args)
        {
            string msg = string.Format(format, args);
            LogMessage(logger, type, msg);
        }

        public static void LogMessage(TraceLog logger, EventType type, string msg)
        {
            if (logger != null)
            {
                logger.WriteLine(type, msg);
            }
            else
            {
                Console.WriteLine(msg);
            }
        }
    }
}
