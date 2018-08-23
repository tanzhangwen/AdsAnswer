//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="TimeHelper.cs">
//      Created by: tomtan at 7/13/2015 10:34:23 AM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Globalization;

    public class TimeHelper
    {
        public static DateTime NeverRunBefore
        {
            get
            {
                return DateTime.MinValue;
            }
            set
            {
                value = DateTime.MinValue;
            }
        }

        /*
        static string DefaultTimeZoneID = "China Standard Time";

        public static void SetDefaultTimeZone(string timeZoneID)
        {
            DefaultTimeZoneID = timeZoneID;
        }
        */

        /// <summary>
        /// return 'Now' according to the time zone setting of v3.ini
        /// </summary>
        /// <returns></returns>
        public static DateTime DefaultNow()
        {
            /*
            DateTime dt = DateTime.Now;
            DateTime local = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dt, DefaultTimeZoneID);
            return local;
             */
            return DateTime.UtcNow;
        }

        /*
        /// <summary>
        /// return 'Now' according to the time zone setting of v3.ini
        /// </summary>
        /// <returns></returns>
        public static DateTime Now(string timeZoneID)
        {
            DateTime dt = DateTime.Now;
            DateTime local = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dt, timeZoneID);
            return local;
        }
        */

        public static string DateTimeToVersionString(DateTime dt)
        {
            return dt.ToString("yyyyMMddHHmmss");
        }

        /*
        public static string NowToVersionString(string timeZoneID)
        {
            DateTime now = TimeHelper.Now(timeZoneID);
            return TimeHelper.DateTimeToVersionString(now);
        }
        */

        public static string DateTimeToFormatString(DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /*
        public static string NowToFormatString(string timeZoneID)
        {
            DateTime now = TimeHelper.Now(timeZoneID);
            return TimeHelper.DateTimeToFormatString(now);
        }
         */

        public static DateTime DateTimeFromVersionString(string versionString)
        {
            try
            {
                DateTime dt = DateTime.ParseExact(versionString, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                return dt;
            }
            catch (Exception ex)
            {
                DateTime vdt;
                if (DateTime.TryParse(versionString, out vdt))
                {
                    return vdt;
                }
                Console.WriteLine("Parse DateTime string '{0}' error: {1}", versionString, ex.Message);
                return DateTime.MinValue;
            }
        }

        public static string DateTimeToStringWithTimeZone(DateTime dt)
        {
            return dt.ToString("o");
        }

        public static string MinVersionString()
        {
            return DateTimeToVersionString(DateTime.MinValue);
        }
    }
}
