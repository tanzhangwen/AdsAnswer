//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="HttpUrlHash.cs">
//      Created by: tomtan at 7/9/2015 5:52:09 PM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.AnswerData.Image
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    public class HttpUrlHash
    {
        [DllImport("hashvalue.dll", EntryPoint = "GetHttpUrlHash")]
        [CLSCompliant(false)]
        public static extern bool GetHttpUrlHash(string url, ref UInt64 low, ref UInt16 high);

        public static string GetHashValueString(string input)
        {
            string httpurl = input;
            if (!input.StartsWith("http://"))
            {
                if (input.StartsWith("//"))
                {
                    httpurl = "http:" + input;
                }
                else if (input.StartsWith("/"))
                {
                    httpurl = "http:/" + input;
                }
                else
                {
                    httpurl = "http://" + input;
                }
            }

            // some url has Chinese characters, but GetHttpUrlHash cannot identify them
            string encodeUrl = UrlEncode(httpurl);

            UInt64 low = 0;
            UInt16 high = 0;
            if (GetHttpUrlHash(encodeUrl, ref low, ref high))
            {
                byte[] bytes = new byte[10];
                for (int i = 0; i < 8; ++i)
                {
                    bytes[i] = (byte)((low >> (i * 8)) & 0xff);
                }
                bytes[8] = (byte)(high & 0xff);
                bytes[9] = (byte)((high >> 8) & 0xff);
                string base64 = Convert.ToBase64String(bytes);
                // 10 bytes * 4 / 3 => 14 bytes + 2 padding '=' chars
                // remove '=' chars
                base64 = base64.Replace('+', '0');
                base64 = base64.Replace('/', '0');
                return base64.Substring(0, 14);
            }
            return string.Empty;
        }

        public static string UrlEncode(string url)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in url)
            {
                if (ch < 128)
                    sb.Append(ch);
                else
                {
                    UTF8Encoding utf8 = new UTF8Encoding();
                    byte[] bytes = utf8.GetBytes(new string(ch, 1));
                    string tmp = System.Web.HttpUtility.UrlEncode(bytes);
                    sb.Append(tmp);
                }
            }
            return sb.ToString();
        }
    }
}
