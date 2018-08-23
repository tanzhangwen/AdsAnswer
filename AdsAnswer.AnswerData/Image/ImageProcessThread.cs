//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="ImageProcessThread.cs">
//      Created by: tomtan at 7/13/2015 9:35:27 AM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.AnswerData.Image
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using Microsoft.ObjectStore;
    using AdsAnswer.Crawler;
    using AdsAnswer.Logging;

    public class ImageProcessHelper
    {
        public static IEnumerable<Tuple<List<Image>, int>> SplitIntoBuckets(List<Image> images, int BurstCount)
        {
            int index = 0;
            List<Image> list = new List<Image>();
            foreach (var image in images)
            {
                list.Add(image);
                if (list.Count >= BurstCount)
                {
                    yield return new Tuple<List<Image>, int>(list, index++);
                    list = new List<Image>();
                }
            }
            if (list.Count > 0)
            {
                yield return new Tuple<List<Image>, int>(list, index);
            }
        }
    }

    public class ImageProcessThread
    {
        private List<Image> listTask;
        private object _lockTaskWriter = new object();
        private string proxy;
        private int failureBuffer;
        private int processFailureBuffer;
        private int ingestBurstCount;
        private object _failBufferLock = new object();
        private string workingFile;
        private string serverFile;
        private int ingestedCount;

        private DateTime lastTime;
        private TimeSpan reportInterval = new TimeSpan(0, 30, 0);

        public bool Result { get; set; }

        TraceLog log;
        private Action<EventType, string> WriteLog;

        public ImageProcessThread()
        {
            this.Result = true;
            this.log = null;
        }

        public ImageProcessThread(TraceLog log, Action<EventType, string> writeLog)
        {
            this.Result = true;
            this.log = log;
            this.WriteLog = writeLog;
        }

        public void AssignTaskList(List<Image> list, string workingFile, string serverfile, string proxy, int buffer, int ingestBurstCount)
        {
            this.listTask = list;
            this.proxy = proxy;
            this.failureBuffer = buffer;
            this.workingFile = workingFile;
            this.serverFile = serverfile;
            this.ingestBurstCount = ingestBurstCount;
        }

        public void Start(int threadCount)
        {
            this.processFailureBuffer = this.failureBuffer;
            ingestedCount = 0;
            this.lastTime = DateTime.Now;

            ImageProcessHelper.SplitIntoBuckets(listTask, ingestBurstCount)
                .AsParallel()
                .WithDegreeOfParallelism(threadCount)
                .Select(DownloadImage)
                .Select(ScaleImage)
                .Select(IngestImage)
                .Select(AggregeteResults)
                .ToList();

            if (!Result)
            {
                ImageLogger.LogMessage(this.log, EventType.Warning, "Image process failure buffer exhausted!");
            }
            else
            {
                ImageLogger.LogMessage(this.log, EventType.Information, "[ImageProcess] - Image Process Done [{0}/{1}]",
                    ingestedCount, listTask.Count);
            }
        }

        private Tuple<List<Image>, int> DownloadImage(Tuple<List<Image>, int> tuple)
        {
            List<Image> list = tuple.Item1;
            int index = tuple.Item2;
            List<Image> downloadedList = new List<Image>();

            foreach (var image in list)
            {
                try
                {
                    lock (_failBufferLock)
                    {
                        if (!Result)
                        {
                            break;
                        }
                    }

                    bool bSuccess = DownloadImage(image);
                    if (!bSuccess)
                    {
                        continue;
                    }

                    downloadedList.Add(image);
                    lock (_failBufferLock)
                    {
                        // process succeed, reset failure buffer
                        this.processFailureBuffer = this.failureBuffer;
                    }
                }
                catch (Exception ex)
                {
                    ImageLogger.LogMessage(this.log, EventType.Error, ex.Message);
                    ImageLogger.LogMessage(this.log, EventType.Warning, "Exception when download image {0}", image.SourceUrl);
                    lock (_failBufferLock)
                    {
                        this.processFailureBuffer -= 1;
                        if (this.processFailureBuffer < 0)
                        {
                            Result = false;
                        }
                    }
                    continue;
                }
            }

            return new Tuple<List<Image>, int>(downloadedList, index);
        }

        private bool DownloadImage(Image myImage)
        {
            BaseCrawler crawler = null;

            string sourceUrl = myImage.SourceUrl.Trim();
            if (sourceUrl.StartsWith(@"https://cosmos", StringComparison.InvariantCultureIgnoreCase)) // cosmos path
            {
                crawler = new CosmosCrawler(FeedUriTypes.ShareFolder, sourceUrl,
                    myImage.DownloadImageName, DateTime.MinValue);
            }
            else if (sourceUrl.StartsWith("http://")
                || sourceUrl.StartsWith("https://"))
            {
                ProxyType proxyType;
                if (!Enum.TryParse<ProxyType>(proxy, true, out proxyType))
                {
                    proxyType = ProxyType.NULL;
                }
                crawler = new HttpCrawler(FeedUriTypes.Http, sourceUrl, myImage.DownloadImageName, DateTime.MinValue, null, proxyType);
            }
            else // wrong image URI
            {
                ImageLogger.LogMessage(this.log, EventType.Warning, "Cannot identify this image's URI: {0}",
                    sourceUrl);
                return false;
            }

            if (crawler != null)
            {
                if (crawler.Crawl() != BuildResults.Crawler_Succeed)
                {
                    ImageLogger.LogMessage(this.log, EventType.Warning, "Exception when download image {0}", sourceUrl);
                    return false;
                }
            }
            return true;
        }

        private Tuple<List<Image>, int> ScaleImage(Tuple<List<Image>, int> tuple)
        {
            List<Image> list = tuple.Item1;
            int index = tuple.Item2;
            List<Image> scaledList = new List<Image>();

            foreach (var image in list)
            {
                try
                {
                    lock (_failBufferLock)
                    {
                        if (!Result)
                        {
                            break;
                        }
                    }

                    bool bSuccess = image.ScaleDefault();
                    if (!bSuccess)
                    {
                        continue;
                    }

                    //image.ExtractAccentColor(log);
                    scaledList.Add(image);

                    lock (_failBufferLock)
                    {
                        // process succeed, reset failure buffer
                        this.processFailureBuffer = this.failureBuffer;
                    }
                }
                catch (Exception ex)
                {
                    ImageLogger.LogMessage(this.log, EventType.Error, ex.Message);
                    ImageLogger.LogMessage(this.log, EventType.Warning, "Exception when scale image {0}", image.SourceUrl);
                    lock (_failBufferLock)
                    {
                        this.processFailureBuffer -= 1;
                        if (this.processFailureBuffer < 0)
                        {
                            Result = false;
                        }
                    }
                    continue;
                }
            }

            return new Tuple<List<Image>, int>(scaledList, index);
        }

        private Tuple<string, int, int> IngestImage(Tuple<List<Image>, int> tuple)
        {
            List<Image> list = tuple.Item1;
            int index = tuple.Item2;
            int partIngestedCount = 0;

            string tmpFile = string.Format("{0}.part{1}", workingFile, index);
            using (StreamWriter sw = new StreamWriter(tmpFile, true))
            {
                string feed = string.Empty;
                List<IDataLoadResult> results = null;

                try
                {
                    using (DataLoader loader = new DataLoader(OSContext.Instance.Configuration))
                    {
                        for (int i = 0; i < list.Count; ++i)
                        {
                            Image myImage = list[i];
                            byte[] data = ScaledImageToBinary(myImage);
                            string hashValue = HttpUrlHash.GetHashValueString(myImage.SourceUrl);
                            results = ImageIngester.Ingest(hashValue, data, myImage, loader);
                            partIngestedCount += LogIngestResults(results, sw);
                        }

                        loader.Flush();
                        results = loader.Receive(true);
                        partIngestedCount += LogIngestResults(results, sw);
                    }
                }
                catch (Exception ex)
                {
                    ImageLogger.LogMessage(this.log, EventType.Error, "[ImageIngest][{0}][T{1}]: {2}", feed, index, ex.Message);
                    ImageLogger.LogMessage(this.log, EventType.Error, ex.StackTrace);
                }
            }

            return new Tuple<string, int, int>(tmpFile, partIngestedCount, index);
        }

        private int LogIngestResults(List<IDataLoadResult> results, StreamWriter sw)
        {
            int succeedCount = 0;
            foreach (IDataLoadResult result in results)
            {
                Image myImage = result.Context as Image;
                string key, feed, scenario;
                scenario = ExtractImageContext(myImage, out feed, out key);

                if (result.IsSuccessful)
                {
                    sw.WriteLine(myImage.ToString());
                    ++succeedCount;
                }
                else
                {
                    ImageLogger.LogMessage(this.log, EventType.Warning, "[ImageIngest][{0}]: Ingest failed to {1} locations: {2}", scenario, result.FailedLocations.Count, String.Join(", ", result.FailedLocations));
                }
            }
            return succeedCount;
        }

        private string ExtractImageContext(Image image, out string feed, out string key)
        {
            feed = string.Empty;
            key = string.Empty;

            string[] tmp = image.LocalImageName.Split("\\".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (tmp.Count() > 3)
            {
                feed = tmp[tmp.Count() - 3];
                key = Path.GetFileNameWithoutExtension(tmp.Last());
                return string.Format("{0}.{1}", feed, key);
            }
            return string.Empty;
        }

        private byte[] ScaledImageToBinary(Image myImage)
        {
            using (FileStream file = new FileStream(myImage.LocalImageName, FileMode.Open))
            {
                byte[] buf = new byte[file.Length];
                file.Read(buf, 0, (int)file.Length);
                return buf;
            }
        }

        private Tuple<int> AggregeteResults(Tuple<string, int, int> tuple)
        {
            string tmpFile = tuple.Item1;
            int partIngestCount = tuple.Item2;
            int index = tuple.Item3;

            lock (_lockTaskWriter)
            {
                File.AppendAllText(workingFile, File.ReadAllText(tmpFile));
                File.Delete(tmpFile);
                //CosmosStream.UploadStream(serverFile, workingFile);
                ingestedCount += partIngestCount;
                ImageLogger.LogMessage(this.log, EventType.Information, "[ImageProcess] - Image Process in progress [{0}/{1}]", ingestedCount, listTask.Count);

                if (DateTime.Now - this.lastTime > this.reportInterval)
                {
                    this.WriteLog(
                        EventType.Information,
                        string.Format(
                            "[ImageProcess]: In Progress ... [{0}/{1}]",
                            ingestedCount,
                            listTask.Count));
                    lastTime = DateTime.Now;
                }
            }
            return new Tuple<int>(index);
        }
    }
}
