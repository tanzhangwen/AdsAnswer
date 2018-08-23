//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="ImageProcessor.cs">
//      Created by: tomtan at 7/9/2015 5:29:54 PM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.AnswerData.Image
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AdsAnswer.Logging;

    public class ImageProcessor
    {
        private static string GetHashFileName(string line)
        {
            string[] tmp = line.Split(new char[] { '\t' });
            if (tmp.Length > 2)
            {
                return System.IO.Path.GetFileName(tmp[2]);
            }
            else
            {
                return null;
            }
        }

        private static List<Image> AssignLocalPathName(List<Image> listAll, string imageDownloadFolder, string imageScaleFolder, HashSet<string> folderFiles)
        {
            List<Image> listNew = new List<Image>();

            string hashValue;
            string ext;
            string localFileName;

            foreach (Image myImage in listAll)
            {
                hashValue = HttpUrlHash.GetHashValueString(myImage.SourceUrl);
                ext = myImage.GetExtFromUrl();
                localFileName = string.Format("{0}.jpg", hashValue);
                myImage.LocalImageName = string.Format("{0}\\{1}", imageScaleFolder, localFileName);
                localFileName = string.Format("{0}{1}", hashValue, ext);
                myImage.DownloadImageName = string.Format("{0}\\{1}", imageDownloadFolder, localFileName);

                if (!folderFiles.Contains(localFileName))
                {
                    listNew.Add(myImage);
                }
            }

            return listNew;
        }

        public static bool ProcessImage(string imageUrlListFilePath, string imageDownloadFolder, string imageScaleFolder, string imageUrlMappingFile, string serverFile, TraceLog log, Action<EventType, string> WriteLog)
        {
            HashSet<string> folderFiles = new HashSet<string>();

            if (File.Exists(imageUrlMappingFile))
            {
                folderFiles = new HashSet<string>(File.ReadLines(imageUrlMappingFile).Select(l => GetHashFileName(l)));
            }

            // read Image URl list file to get process task
            ImageWorkingFile iwf = new ImageWorkingFile(imageUrlListFilePath, log);
            List<Image> listAll = iwf.Read();
            ImageLogger.LogMessage(log, EventType.Information, "[ImageProcess] - Total {0} images in list.", listAll.Count);

            // assign local path name to each myImage
            List<Image> listNew = AssignLocalPathName(listAll, imageDownloadFolder, imageScaleFolder, folderFiles);
            ImageLogger.LogMessage(log, EventType.Information, "[ImageProcess] - Total {0} new images need to process.", listNew.Count);
            WriteLog(
                EventType.Information,
                string.Format("[ImageProcess] - Total {0} new images need to process.", listNew.Count));

            int failureBuffer = Math.Max(listAll.Count * 3 / 100, 50);     // 3% or 50 failure buffer
            int threadCount = 10;// ConfigStore.Instance.OsClientConfig.IngestThreadCount;
            int ingestBurstCount = 10;// ConfigStore.Instance.OsClientConfig.IngestBurstCount;
            string proxy = "bjsproxy";
            if (listNew.Count >= 1000000)
            {
                ImageLogger.LogMessage(log, EventType.Error, "[ImageProcess] - Too many images (1+ million) need to process, will casue image table full.");
                return false;
            }
            else if (listNew.Count > 0)
            {
                System.Net.ServicePointManager.DefaultConnectionLimit = 5120;
                ImageProcessThread thread = new ImageProcessThread(log, WriteLog);
                thread.AssignTaskList(listNew, imageUrlMappingFile, serverFile, proxy, failureBuffer, ingestBurstCount);
                thread.Start(threadCount);

                if (!thread.Result)
                    return false;
            }
            else
            {
                ImageLogger.LogMessage(log, EventType.Information, "[ImageProcess] - No new images to process.");
            }

            return true;
        }
    }
}
