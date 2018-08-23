//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="CustomWebClient.cs">
//      Created by: tomtan at 7/13/2015 10:12:23 AM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Cache;
    using System.Text;
    using System.IO;
    using System.Text.RegularExpressions;
    using AdsAnswer.Utility;

    public enum ProxyType
    {
        NULL = 0,
        Default,
        JPN,
        BJS
    }

    public class CustomWebClient : WebClient
    {
        internal class NotModifiedException : WebException
        {
            public NotModifiedException() : base() { }
            public NotModifiedException(string message) : base(message) { }
        }

        public readonly static IList<IWebProxy> ProxyList = new List<IWebProxy> 
        { 
            null,
            WebRequest.DefaultWebProxy,
            new WebProxy("jpnproxy") 
            {
                BypassProxyOnLocal = true, 
                UseDefaultCredentials = true
            }, 
            new WebProxy("bjsproxy")
            {
                BypassProxyOnLocal = true, 
                UseDefaultCredentials = true
            }
        };

        public WebProxy ManualProxy;
        private int proxyIndex = 0;
        private ProxyType proxyType = ProxyType.NULL;
        private long contentLength = -1L;

        public Uri Uri { get; private set; }

        public string Path { get; private set; }

        public DateTime LocalVersion { get; private set; }

        public DateTime RemoteVersion { get; private set; }

        public string UserAgent { get; private set; }

        public Exception Exception { get; private set; }

        public CustomWebClient(Uri uri, string path, string ua = null, ProxyType proxyType = ProxyType.NULL)
            : this(uri, path, DateTime.MinValue, ua, proxyType) { }

        public CustomWebClient(Uri uri, string path, DateTime localVersion, string ua = null, ProxyType proxyType = ProxyType.NULL)
            : this(uri, path, Encoding.Default, localVersion, ua, proxyType) { }

        public CustomWebClient(Uri uri, string path, Encoding encoding, DateTime localVersion, string ua = null, ProxyType proxyType = ProxyType.NULL)
        {
            this.Uri = uri;
            this.Path = path;
            this.LocalVersion = localVersion;
            this.Encoding = encoding;
            this.UserAgent = ua;
            this.proxyType = proxyType;
            this.proxyIndex = (int)proxyType;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request != null)
            {
                if (this.ManualProxy != null)
                {
                    request.Proxy = this.ManualProxy;
                }
                else
                {
                    request.Proxy = ProxyList[this.proxyIndex];
                }

                string ua = string.IsNullOrEmpty(this.UserAgent) ? "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)" : this.UserAgent;

                (request as HttpWebRequest).AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                (request as HttpWebRequest).Host = address.Host;
                (request as HttpWebRequest).UserAgent = ua;
                (request as HttpWebRequest).CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
            }

            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = null;
            try
            {
                //System.Net.WebException
                response = base.GetWebResponse(request);
            }
            catch
            {
                response = null; 
            }
            DateTime remoteVersion = this.LocalVersion;
            if (response != null)
            {
                // Some feed source doesn't have "Last-Modified" field, set RemoteVersion as  
                // Now, so that feed reporting can work smoothly.
                if (DateTime.TryParse(response.Headers.Get("Last-Modified"), out remoteVersion))
                {
                    this.RemoteVersion = remoteVersion;
                    if (remoteVersion <= LocalVersion)
                    {
                        throw new NotModifiedException("The file has no update");
                    }
                }
                else
                {
                    // Last-Modified not exist, use now as remote version
                    RemoteVersion = TimeHelper.DefaultNow();
                }
                // Get content length
                string contentLenField = response.Headers["Content-Length"];
                if (contentLenField != null)
                    this.contentLength = long.Parse(contentLenField);
            }

            return response;
        }

        public BuildResults DownloadFile()
        {
            try
            {
                this.DownloadFile(this.Uri, this.Path);

                if (this.Encoding != System.Text.Encoding.UTF8 && this.Encoding != System.Text.Encoding.Default)
                {
                    string content = string.Empty;

                    using (StreamReader reader = new StreamReader(this.Path, this.Encoding))
                    {
                        content = reader.ReadToEnd();
                    }
                     
                    if (!string.IsNullOrEmpty(content)) 
                    {
                        content = Regex.Replace(content, "<\\?xml([^>]*)encoding=\\s*\"[^\"]*\"([^>]*)\\?>", "<?xml$1encoding=\"utf-8\"$2?>");
                        using (StreamWriter writer = new StreamWriter(this.Path, false, Encoding.UTF8))
                        {
                            writer.Write(content);
                        }
                    }
                }
            }
            catch (NotModifiedException)
            {
                return BuildResults.Crawler_NoNewFileFound;
            }
            catch (WebException we)
            {
                SetNextProxyIndex();
                this.Exception = we;

                if (we.Message.Contains("(404) Not Found"))
                {
                    return BuildResults.Crawler_404NotFound;
                }
                else if (we.Status == WebExceptionStatus.ProtocolError)
                {
                    WebResponse resp = we.Response;
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    {
                        Exception ce = new System.Exception(sr.ReadToEnd(), we);
                        this.Exception = ce;
                    }

                    return BuildResults.Crawler_ProtocolError;
                }

                return BuildResults.Crawler_RemoteServerNoResponse;
            }
            catch (Exception e)
            {
                SetNextProxyIndex();
                this.Exception = e;
                return BuildResults.Crawler_UnexpectedError;
            }

            return BuildResults.Crawler_Succeed;
        }

        private void SetNextProxyIndex()
        {
            if (ProxyType.NULL == this.proxyType)
            {
                this.proxyIndex = (this.proxyIndex + 1) % ProxyList.Count;
            }
            else
            {
                this.proxyIndex = (int)this.proxyType;
            }
        }
    }
}
