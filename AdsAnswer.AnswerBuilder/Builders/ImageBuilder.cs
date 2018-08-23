//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="ImageBuilder.cs">
//      Created by: tomtan at 7/9/2015 4:20:39 PM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.AnswerBuilder
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Xml;
    using System.Xml.Linq;
    using AdsAnswer;
    using AdsAnswer.AnswerData;
    using AdsAnswer.AnswerData.Image;
    using AdsAnswer.Logging;
    using AdsAnswer.Cosmos;

    public class ImageBuilder
    {
        private BuildResults Result;
        private Feed feed;
        private string WorkingFolder;
        private string ImageWorkingFolder;

        private Action<EventType, string> feedWriteLog;

        public ImageBuilder(Feed feed, Action<EventType, string> writeLog)//, Action<string> addAttachFile)
        {
            this.feed = feed;
            //this.TsHelper = helper;
            //this.MappingRuntime = feed.MappingRuntime;
            this.WorkingFolder = feed.BuildWorkingFolder;
            this.feedWriteLog = writeLog;
            //this.addAttachFile = addAttachFile;

            //this.MappingFileList = new List<string>();
            //foreach (var mappingFile in feed.MappingFiles)
            //{
            //    MappingFileList.Add(mappingFile.Value);
            //}

            this.ImageWorkingFolder = Path.Combine(this.WorkingFolder, "Image");
            //this.fullImagePath = feed.ImageFolder;
        }

        private void WriteLog(EventType eventType, string format, params object[] args)
        {
            string message = string.Format(format, args);
            this.feedWriteLog(eventType, message);
        }

        private void ExtractImageUrls(string inputFile, List<string> imgXpaths)
        {
            List<Image> imgs = new List<Image>();
            foreach(var xpath in imgXpaths)
            {
                imgs.AddRange(ExtractImageUrls(inputFile, xpath));
            }

            string imgUrlsFile = Path.Combine(this.WorkingFolder, string.Format("{0}.ImageUrlList.txt", feed.ProviderName));
            using (StreamWriter sw = new StreamWriter(imgUrlsFile))
            {
                foreach (var img in imgs.GroupBy(img => img.SourceUrl).Select(grp => grp.First()))
                {
                    sw.WriteLine(img.ToString());
                }
            }
        }

        private List<Image> ExtractImageUrls(string inputFile, string imgXpath)
        {
            List<Image> imgs = new List<Image>();

            XDocument doc = XDocument.Load(inputFile);
            var pics = doc.Descendants(imgXpath);

            if (pics.Count() > 0)
            {
                foreach(var pic in pics)
                {
                    if (string.IsNullOrEmpty(pic.Value)) continue;
                    Image image = new Image();
                    image.SourceUrl = pic.Value;
                    image.Width = int.Parse(pic.Attribute("w").Value);
                    image.Height = int.Parse(pic.Attribute("h").Value);
                    imgs.Add(image);
                }
            }
            return imgs;
        }

        public void ProcessImage(string imageUrlListFile, string sourceTargetUrlMappingFile)
        {
            this.WriteLog(EventType.Critical, "[Start] | [{0}]", "ProcessImage");

            string logPath = Path.Combine(this.WorkingFolder, "prolog.txt");//ImageWorkingFile.ImageProcessLogFile);
            TraceLog log = new TraceLog("ImageProcess", logPath);

            string imageUrlListFilePath = Path.Combine(this.WorkingFolder, imageUrlListFile);

            string imageDownloadFolder = Path.Combine(this.WorkingFolder, "Download");
            if (!Directory.Exists(imageDownloadFolder))
            {
                Directory.CreateDirectory(imageDownloadFolder);
            }

            string imageScaleFolder = Path.Combine(this.WorkingFolder, "Scale");
            if (!Directory.Exists(imageScaleFolder))
            {
                Directory.CreateDirectory(imageScaleFolder);
            }

            string imageUrlMappingFile = Path.Combine(this.WorkingFolder, sourceTargetUrlMappingFile);
            string serverFile = CosmosStream.BuildUrlPath("https://cosmos08.osdinfra.net/cosmos/bingads.marketplace.VC2/local/RAACenter/Data/SnRAA/", sourceTargetUrlMappingFile);
            if (CosmosStream.StreamExists(serverFile))
            {
                CosmosStream.DownloadStream(serverFile, imageUrlMappingFile);
            }

            int oriImageCount = 0;
            if (File.Exists(imageUrlMappingFile))
            {
                List<string> lines = File.ReadAllLines(imageUrlMappingFile).ToList();
                oriImageCount = lines.Where(l => !string.IsNullOrEmpty(l)).Count();
            }

            bool bSuccess = ImageProcessor.ProcessImage(imageUrlListFilePath, imageDownloadFolder, imageScaleFolder, imageUrlMappingFile, serverFile, log, this.feedWriteLog);
            if (bSuccess)
            {
                this.Result = BuildResults.Image_Succeed;
                this.WriteLog(EventType.Critical, "[{0}] Image succeeded.", "Process");
            }
            else
            {
                this.Result = BuildResults.Image_ProcessImageFailed;
                this.WriteLog(EventType.Critical, "[{0}] Image failed.", "Process");
                //this.addAttachFile(ImageLibrary.ImageWorkingFile.ImageProcessLogFile);
                //this.WriteLog(EventType.Error, "Please refer the image log [{0}] for more details", ImageLibrary.ImageWorkingFile.ImageProcessLogFile);
            }

            int newImageCount = 0;
            if (File.Exists(imageUrlMappingFile))
            {
                List<string> lines = File.ReadAllLines(imageUrlMappingFile).ToList();
                newImageCount = lines.Where(l => !string.IsNullOrEmpty(l)).Count();
            }
            this.WriteLog(EventType.Critical, "[ProcessedImageCount] | [{0}]", newImageCount - oriImageCount);

            log.Close();
            log = null;
            this.WriteLog(EventType.Critical, "[End] | [{0}]", "ProcessImage");
        }

        public void Build()
        {
            //string imgn = HttpUrlHash.GetHashValueString("http://imga.4399.cn/upload_pic/2012/5/19/4399_16195998160.jpg");
            //Console.WriteLine(imgn);
            ExtractImageUrls("sn_1.xml", new List<string>(){"pic"});
            string imgUrlsFile = Path.Combine(this.WorkingFolder, string.Format("{0}.ImageUrlList.txt", feed.ProviderName));
            ProcessImage(imgUrlsFile, "urlMap.txt");
        }
    }
}
