//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="CosmosStream.cs">
//      Created by: tomtan at 7/13/2015 10:43:27 AM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.Cosmos
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;

    using VcClient;
    using VcClientExceptions;

    public class CosmosStream
    {
        public const int CosmosRetryTimes = 3;

        public static void Setup(string VC, string user)
        {
            VcClient.VC.Setup(VC, VcClient.VC.NoProxy, user);
        }

        public static void AppendStream(string cosmosStream, string localFilePath, bool lineBoundary = false)
        {
            if (VcClient.VC.StreamExists(cosmosStream))
            {
                VcClient.VC.AppendFile(cosmosStream, localFilePath, false);
            }
            else
            {
                UploadStream(cosmosStream, localFilePath, lineBoundary);
            }
        }

        public static void DeleteStream(string cosmosStream)
        {
            if (VcClient.VC.StreamExists(cosmosStream))
            {
                VcClient.VC.Delete(cosmosStream);
            }
        }

        public static void DownloadStream(string cosmosStream, string localFile)
        {
            for (int retries = 0; retries < CosmosRetryTimes; retries++)
            {
                try
                {
                    if (VC.StreamExists(cosmosStream))
                    {
                        VC.Download(cosmosStream, localFile, false, true);
                    }
                    break;
                }
                catch (Exception)
                {
                    if (retries >= CosmosRetryTimes - 1)
                    {
                        throw;
                    }

                    System.Threading.Thread.Sleep(1000 * 60);
                }
            }
        }

        public static void RenameStream(string oldStreamName, string newStreamName)
        {
            if (!newStreamName.Equals(oldStreamName) && VcClient.VC.StreamExists(oldStreamName))
            {
                if (VcClient.VC.StreamExists(newStreamName))
                {
                    VcClient.VC.Delete(newStreamName);
                }
                VcClient.VC.Rename(oldStreamName, newStreamName);
            }
        }

        public static void ConcatStream(string sourceStream, string targetStream)
        {
            VC.Concatenate(sourceStream, targetStream);
        }

        public static bool StreamExists(string targetStream)
        {
            return VC.StreamExists(targetStream);
        }

        private const int DEFAULT_STREAM_EXPIRE_DAYS = 365;
        /// <summary>
        /// upload file to cosmos
        /// if the file is too large, it is need to use 'VC.Append' mode
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="sourceFile"></param>
        /// <param name="destStream"></param>
        /// <param name="proxy"></param>
        /// <param name="userName"></param>
        public static void UploadStream(string cosmosStream, string localFilePath, bool lineBoundary = false)
        {
            for (int retries = 0; retries < CosmosRetryTimes; retries++)
            {
                try
                {
                    string tmpStream = string.Format("{0}_{1}.tmp", cosmosStream, DateTime.UtcNow.ToFileTime());
                    DeleteStream(tmpStream);
                    if (lineBoundary)
                    {
                        VcClient.VC.Upload(localFilePath, tmpStream, true,
                            new TimeSpan(DEFAULT_STREAM_EXPIRE_DAYS, 0, 0, 0), lineBoundary);
                        VcClient.VC.SealStream(tmpStream);
                    }
                    else
                    {
                        VcClient.VC.Upload(localFilePath, tmpStream, true); // default Compression is true
                        VcClient.VC.SealStream(tmpStream);
                    }
                    DeleteStream(cosmosStream);
                    RenameStream(tmpStream, cosmosStream);

                    break;
                }
                catch (Exception)
                {
                    if (retries >= CosmosRetryTimes - 1)
                    {
                        throw;
                    }

                    System.Threading.Thread.Sleep(1000 * 60);
                }
            }
        }

        public static void SetExpiration(string stream, int day = 7)
        {
            VcClient.VC.SetStreamExpirationTime(stream, new TimeSpan(day, 0, 0, 0));
        }

        public static void UploadLargeStream(string target, string source, Action<AdsAnswer.Logging.EventType, string> writeLog = null, bool lineBoundary = false)
        {
            var fileSize = new System.IO.FileInfo(source).Length;
            if (fileSize < 500 * 1024 * 1024) // <500M
            {
                UploadStream(target, source, lineBoundary);
                return;
            }

            // split into blocks
            List<long> blockOffsets = new List<long>();
            List<int> blockLengths = new List<int>();
            using (var stream = File.OpenRead(source))
            {
                long blockSize = 200 * 1024 * 1024; // 200M 
                if (lineBoundary)
                {
                    byte[] Buffer = new byte[blockSize];
                    const byte EndOfLine = (byte)'\n';
                    long pos = 0;
                    int numRead = -1;
                    while ((numRead = stream.Read(Buffer, 0, Buffer.Length)) > 0)
                    {
                        int offset = -1;
                        for (int i = numRead - 1; i >= 0; i--)
                        {
                            if (Buffer[i].Equals(EndOfLine))
                            {
                                offset = i + 1;
                                break;
                            }
                        }
                        if (offset < 0) // the last line
                        {
                            blockOffsets.Add(pos);
                            blockLengths.Add(numRead);
                        }
                        else
                        {
                            blockOffsets.Add(pos);
                            blockLengths.Add(offset);
                            pos += offset;
                            stream.Seek(pos, SeekOrigin.Begin);
                        }
                    }
                    Buffer = null;
                }
                else
                {
                    long pos;
                    for (pos = 0; pos < stream.Length; pos += blockSize)
                    {
                        blockOffsets.Add(pos);
                        blockLengths.Add((int)Math.Min(blockSize, stream.Length - pos));
                    }
                }
            }
            long[] offsets = blockOffsets.ToArray();
            int[] lengths = blockLengths.ToArray();
            Debug.Assert(offsets.Length == lengths.Length);

            // upload blocks
            string guid = Guid.NewGuid().ToString();
            string incomplete = target + guid;
            List<string> partials = new List<string>();
            int n = offsets.Length;
            int retryNum = 0;
            do
            {
                if (retryNum++ >= 3)
                    throw new Exception("Failed to upload " + source);
                partials.Clear();

                if (VC.StreamExists(incomplete))
                    VC.Delete(incomplete);
                for (int i = 0; i < n; i++)
                {
                    long offset = offsets[i];
                    int length = lengths[i];
                    string partial = string.Format("{0}_parts/{1}_{2}", target, guid, i);
                    int number = 0;
                    do
                    {
                        number++;
                        try
                        {
                            if (VC.StreamExists(partial))
                            {
                                if (VC.GetStreamInfo(partial, false).CommittedLength == length)
                                    break;
                                VC.Delete(partial);
                            }
                            if (writeLog != null && i % 10 == 0)
                                writeLog(AdsAnswer.Logging.EventType.Information, string.Format("Uploading block {0}/{1}", i, n));
                            // Console.WriteLine(partial);
                            VC.Upload(source, partial, offset, length, true, new TimeSpan(DEFAULT_STREAM_EXPIRE_DAYS, 0, 0, 0), true);
                            VC.SealStream(partial);
                        }
                        catch (Exception ex)
                        {
                            AdsAnswer.Logging.Logger.Error("CosmosStream", "UploadLargeStream", "", ex.ToString());
                            if (number >= 3)
                                throw ex;
                        }
                    } while (!VC.StreamExists(partial) || VC.GetStreamInfo(partial, false).CommittedLength != length);
                    VC.Concatenate(partial, incomplete);
                    partials.Add(partial);
                }
                VC.SealStream(incomplete);
            } while (!VC.StreamExists(incomplete) || VC.GetStreamInfo(incomplete, false).CommittedLength != fileSize);

            // commit & clean up
            if (VC.StreamExists(target))
                VC.Delete(target);
            VC.Concatenate(incomplete, target);
            VC.Delete(incomplete);
            foreach (var p in partials)
                VC.Delete(p);
        }

        /// <summary>
        /// Get last modified time
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="compression"></param>
        /// <returns></returns>
        public static DateTime GetLastModifiedTime(string stream, bool compression = true)
        {
            StreamInfo info = VC.GetStreamInfo(stream, compression);
            return info.PublishedUpdateTime;
        }

        public static string BuildUrlPath(string root, params string[] folders)
        {
            string path = root;
            if (path.EndsWith("/"))
            {
                path = path.Substring(0, path.Length - 1);
            }

            if (folders != null)
            {
                foreach (string folder in folders)
                {
                    path = string.Format("{0}/{1}", path, folder);
                }
            }

            return path;
        }

        public static void DeleteCosmosDirectoryFiles(string directoryName, bool recursive = false)
        {
            if (string.IsNullOrEmpty(directoryName))
            {
                return;
            }

            directoryName = directoryName.TrimEnd(new char[] { '/' });

            List<StreamInfo> streamInfos = null;
            try
            {
                streamInfos = VC.GetDirectoryInfo(directoryName, false);
            }
            catch (VcClientException)
            {
                // the directory doens't exist
            }

            if (streamInfos != null)
            {
                foreach (StreamInfo streamInfo in streamInfos)
                {
                    if (streamInfo.IsDirectory && recursive)
                    {
                        DeleteCosmosDirectoryFiles(streamInfo.StreamName, recursive);
                    }
                    else if (!streamInfo.IsDirectory)
                    {
                        DeleteCosmosFile(streamInfo);
                    }
                }
            }
        }

        private static void DeleteCosmosFile(StreamInfo streamInfo)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    VC.Delete(streamInfo.StreamName);
                    break;
                }
                catch (Exception ex)
                {
                    if (i == 2)
                    {
                        throw ex;
                    }

                    System.Threading.Thread.Sleep(2000);
                }
            }
        }

        public static void DeleteCosmosFile(string filePath)
        {
            if (VC.StreamExists(filePath))
            {
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        VC.Delete(filePath);
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (i == 2)
                        {
                            throw ex;
                        }

                        System.Threading.Thread.Sleep(2000);
                    }
                }
            }
        }

        public static int CopyFolderFiles(string dirSource, string dirTarget, bool recursive = false)
        {
            dirSource = dirSource.TrimEnd(new char[] { '/' });
            dirTarget = dirTarget.TrimEnd(new char[] { '/' });

            int fileCount = 0;

            List<StreamInfo> streamInfos = null;
            try
            {
                streamInfos = VC.GetDirectoryInfo(dirSource, false);
            }
            catch (VcClientException)
            {
                // the directory doens't exist
            }

            if (streamInfos != null)
            {
                foreach (StreamInfo streamInfo in streamInfos)
                {
                    string[] parts = streamInfo.StreamName.Split(
                            new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                    string lastPart = parts[parts.Length - 1];
                    string targetName = BuildUrlPath(dirTarget, lastPart);
                    if (streamInfo.IsDirectory && recursive)
                    {
                        fileCount += CopyFolderFiles(
                            streamInfo.StreamName, targetName, recursive);
                    }
                    else
                    {
                        ConcatStream(streamInfo.StreamName, targetName);
                        fileCount++;
                    }
                }
            }

            return fileCount;
        }

        public static int MoveFolderFiles(string dirSource, string dirTarget, bool recursive = false)
        {
            dirSource = dirSource.TrimEnd(new char[] { '/' });
            dirTarget = dirTarget.TrimEnd(new char[] { '/' });

            int fileCount = 0;

            List<StreamInfo> streamInfos = null;
            try
            {
                streamInfos = VC.GetDirectoryInfo(dirSource, false);
            }
            catch (VcClientException)
            {
                // the directory doens't exist
            }

            if (streamInfos != null)
            {
                foreach (StreamInfo streamInfo in streamInfos)
                {
                    string[] parts = streamInfo.StreamName.Split(
                            new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                    string lastPart = parts[parts.Length - 1];
                    string targetName = BuildUrlPath(dirTarget, lastPart);
                    if (streamInfo.IsDirectory && recursive)
                    {
                        fileCount += MoveFolderFiles(
                            streamInfo.StreamName, targetName, recursive);
                    }
                    else
                    {
                        RenameStream(streamInfo.StreamName, targetName);
                        fileCount++;
                    }
                }
            }

            return fileCount;
        }

        public static List<string> GetFiles(string cosmosDirectory, string pattern)
        {
            List<string> files = new List<string>();
            if (string.IsNullOrEmpty(cosmosDirectory) || string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentException("Argument cosmosDirectory or pattern is null/empty");
            }

            cosmosDirectory = cosmosDirectory.Trim(new char[] { '/' });
            List<StreamInfo> streamInfos = null;
            try
            {
                streamInfos = VC.GetDirectoryInfo(cosmosDirectory, false);
            }
            catch (VcClientException)
            {
                // the directory doens't exist
            }

            if (streamInfos != null)
            {
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
                foreach (StreamInfo streamInfo in streamInfos)
                {
                    string fileName = CosmosStream.GetFileName(streamInfo.StreamName);
                    if (regex.IsMatch(fileName))
                    {
                        files.Add(streamInfo.StreamName);
                    }
                }
            }

            return files;
        }

        public static bool DirectoryExists(string directoryPath)
        {
            try
            {
                VC.GetDirectoryInfo(directoryPath, false);
                return true;
            }
            catch (VcClientException)
            {
                return false;
            }
        }

        public static string GetFileName(string cosmosPath)
        {
            if (!string.IsNullOrEmpty(cosmosPath))
            {
                string[] parts = cosmosPath.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    return parts[parts.Length - 1];
                }
            }

            return string.Empty;
        }
    }
}
