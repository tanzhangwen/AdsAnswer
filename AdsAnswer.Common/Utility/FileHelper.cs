//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="FileHelper.cs">
//      Created by: tomtan at 7/13/2015 10:29:23 AM
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
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Management;

    public class FileHelper
    {
        /// <summary>
        /// get full absolute path of a file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetFullPath(string fileName)
        {
            return GetAppDirectory() + fileName;
        }
        /// <summary>
        /// get directory of the executing file  
        /// </summary>
        /// <returns></returns>
        public static string GetAppDirectory()
        {
            string codeBase = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            return GetFileDirectory(codeBase);
        }

        /// <summary>
        /// get the directory of a file 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string GetFileDirectory(string filePath)
        {
            string appDirectory = filePath.Remove(filePath.LastIndexOf('\\') + 1);
            return appDirectory;
        }

        public static string GetSubFolderName(string folder)
        {
            string tmp = folder.TrimEnd('\\');
            int lastSlash = tmp.LastIndexOf('\\');
            if (lastSlash == -1)
                return string.Empty;
            return tmp.Substring(lastSlash + 1);
        }

        public static void DeleteFolderFiles(string dirSource, bool recursive = false)
        {
            if (!Directory.Exists(dirSource))
                return;

            string[] files = Directory.GetFiles(dirSource);
            foreach (string file in files)
            {
                DeleteFile(file);
            }

            if (recursive)
            {
                string[] subDirs = Directory.GetDirectories(dirSource);
                foreach (string subDir in subDirs)
                {
                    DeleteFolderFiles(subDir, recursive);
                }
            }
            //DeleteDirectory(dirSource);
        }

        public static void DeleteDirectory(string destPath)
        {
            if (Directory.Exists(destPath))
            {
                // delete each file in the folder, because some of the file might be with ReadOnly attribute
                string[] files = Directory.GetFiles(destPath);
                foreach (string file in files)
                {
                    DeleteFile(file);
                }
                Directory.Delete(destPath, true);
            }
        }

        public static void DeleteDirectory(string folderPath, bool throwException = true)
        {
            //First delete files in the folder
            foreach (string file in Directory.GetFiles(folderPath))
            {
                DeleteFile(file);
            }

            //Delete folder
            foreach (string dir in Directory.GetDirectories(folderPath))
            {
                DeleteDirectory(dir, throwException);
            }

            try
            {
                Directory.Delete(folderPath);
            }
            catch (Exception e)
            {
                if (throwException)
                    throw e;
            }
        }

        public static void DeleteFile(string destPath)
        {
            if (File.Exists(destPath))
            {
                File.SetAttributes(destPath, FileAttributes.Normal);

                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        File.Delete(destPath);
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

        /// <summary>
        /// Remove those files which exist in dirTarget, but not exist in dirSource
        /// </summary>
        /// <param name="dirSource"></param>
        /// <param name="dirTarget"></param>
        public static void RemoveDestExtraFiles(string dirSource, string dirTarget)
        {
            if (!Directory.Exists(dirTarget))
                return;

            if (!Directory.Exists(dirSource))
            {
                FileHelper.DeleteFolderFiles(dirTarget);
                return;
            }

            string[] files = Directory.GetFiles(dirTarget);
            foreach (string file in files)
            {
                string src = Path.Combine(dirSource, Path.GetFileName(file));
                if (!File.Exists(src))
                {
                    File.Delete(file);
                }
            }
        }

        public static int CopyFolderFiles(string dirSource, string dirTarget, bool recursive = false)
        {
            if (!Directory.Exists(dirSource))
                return 0;

            string[] files = Directory.GetFiles(dirSource);
            foreach (string file in files)
            {
                string target = Path.Combine(dirTarget, Path.GetFileName(file));
                FileHelper.CopyFile(file, target);
            }

            int subCount = 0;
            if (recursive)
            {
                string[] subDirs = Directory.GetDirectories(dirSource);
                foreach (string subDir in subDirs)
                {
                    subCount += CopyFolderFiles(subDir, dirTarget + "\\" + GetSubFolderName(subDir), recursive);
                }
            }
            return files.Length + subCount;
        }

        /// <summary>
        /// Copy folder files for only those have updated
        /// </summary>
        /// <param name="dirSource"></param>
        /// <param name="dirTarget"></param>
        public static void UpdateFolderFiles(string dirSource, string dirTarget)
        {
            if (!Directory.Exists(dirSource))
                return;

            string[] files = Directory.GetFiles(dirSource);
            foreach (string file in files)
            {
                string target = Path.Combine(dirTarget, Path.GetFileName(file));
                FileHelper.UpdateFile(file, target);
            }
        }

        /// <summary>
        /// Update a file if changed to newer
        /// </summary>
        /// <param name="soureFile"></param>
        /// <param name="destFile"></param>
        /// <returns></returns>
        public static bool UpdateFile(string soureFile, string destFile)
        {
            if (soureFile == destFile)
            {
                return false;
            }
            if (!File.Exists(soureFile))
            {
                return false;
            }

            PrepareDirectory(destFile);

            if (File.Exists(destFile))
            {
                if (File.GetCreationTime(soureFile) > File.GetCreationTime(destFile))
                {
                    FileHelper.CopyFile(soureFile, destFile);
                }
            }
            else
            {
                FileHelper.CopyFile(soureFile, destFile);
            }

            return true;
        }

        /// <summary>
        /// if a file 's direcotry does not exist, create it.
        /// </summary>
        /// <param name="filePath"></param>
        public static void PrepareDirectory(string filePath)
        {
            string directory = GetFileDirectory(filePath);
            if (directory != "" && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// copy a file to another path
        /// </summary>
        /// <param name="soureFile"></param>
        /// <param name="destFile"></param>
        /// <returns></returns>
        public static bool CopyFile(string soureFile, string destFile)
        {
            if (soureFile == destFile)
            {
                return true;
            }
            if (!File.Exists(soureFile))
            {
                return false;
            }

            PrepareDirectory(destFile);

            if (File.Exists(destFile))
            {
                File.SetAttributes(destFile, FileAttributes.Normal);
            }

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    File.Copy(soureFile, destFile, true);
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

            File.SetAttributes(destFile, FileAttributes.Normal);
            return true;
        }

        /// <summary>
        /// move a file to another path
        /// </summary>
        /// <param name="soureFile"></param>
        /// <param name="distFile"></param>
        /// <returns></returns>
        public static bool MoveFile(string soureFile, string distFile)
        {
            if (CopyFile(soureFile, distFile))
            {
                File.Delete(soureFile);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Write content by lines to destination file.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="desFilePath"></param>
        public static void WriteToFile(List<string> content, string desFilePath)
        {
            StreamWriter writer = new StreamWriter(desFilePath);

            foreach (string line in content)
            {
                if (line.Length > 0)
                {
                    writer.WriteLine(line);
                }
            }

            writer.Flush();
            writer.Close();
        }

        /// <summary>
        /// Read content from source file, and restore the content into List<string> by lines
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <returns></returns>
        public static List<string> ReadFromFile(string sourceFilePath)
        {
            List<string> content = new List<string>();

            StreamReader reader = new StreamReader(sourceFilePath);

            string sLine = reader.ReadLine();
            while (sLine != null)
            {
                content.Add(sLine);
                sLine = reader.ReadLine();
            }

            reader.Close();

            return content;
        }

        /// <summary>
        /// Read content from source file to the end, restore content into string
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <returns></returns>
        public static string ReadToEndFromFile(string sourceFilePath)
        {
            StreamReader reader = new StreamReader(sourceFilePath);

            string sLine = reader.ReadToEnd();

            reader.Close();
            return sLine;
        }

        public static bool FormatXml(string inputFile, string outputFile)
        {
            // load source xml
            XmlDocument doc = new XmlDocument();
            doc.Load(inputFile);
            XmlNode root = doc.DocumentElement;
            string rootName = root.Name;

            // create new temp xml file with utf-8
            XmlDocument doc2 = new XmlDocument();
            XmlDeclaration dec = doc2.CreateXmlDeclaration("1.0", "UTF-8", "");
            doc2.AppendChild(dec);
            XmlNode root2 = doc2.ImportNode(root, true);
            doc2.AppendChild(root2);
            string tmpFile = Path.GetDirectoryName(inputFile) + "\\tmp.xml";
            doc2.Save(tmpFile);

            // open and read tmp file using utf-8 encoding, and trim '\r' or '\n' in the node text(between '<' and '>')
            StreamReader sr = new StreamReader(tmpFile, Encoding.UTF8);
            StreamWriter sw = new StreamWriter(outputFile, false, Encoding.UTF8);
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                line = line.TrimEnd();
                if (line.EndsWith(">"))
                    sw.WriteLine(line);
                else
                    sw.Write(line);
            }
            sw.Close();
            sr.Close();

            DeleteFile(tmpFile);

            return true;
        }

        /// <summary>
        /// Compare two files
        /// </summary>
        /// <param name="file1"></param>
        /// <param name="file2"></param>
        /// <returns></returns>
        public static bool CompareFiles(string file1, string file2)
        {
            byte[] content1 = File.ReadAllBytes(file1);
            byte[] content2 = File.ReadAllBytes(file2);

            if (content1.Length != content2.Length)
                return false;

            for (int i = 0; i < content1.Length; i++)
            {
                if (content1[i] != content2[i])
                    return false;
            }

            return true;
        }

        public static string GetFullFileOrFolderPath(string root, FileFolderPathTypes folderType, string folder)
        {
            if (folderType == FileFolderPathTypes.FullPath)
            {
                return folder;
            }
            else if (folderType == FileFolderPathTypes.RelativePath)
            {
                return Path.Combine(root, folder);
            }
            return string.Empty;
        }

        public static string GetSharedPath(string path)
        {
            string sharedfile = path;

            if (!path.StartsWith(@"\\"))
            {
                ManagementClass shares = new ManagementClass("Win32_Share");
                try
                {
                    string localhost = System.Net.Dns.GetHostName();
                    // Find the shared path
                    ManagementObjectCollection specificShares = shares.GetInstances();
                    foreach (ManagementObject share in specificShares)
                    {
                        string localFolder = (string)share["Path"];
                        string sharedFolder = (string)share["Name"];
                        if (sharedFolder.EndsWith("$"))
                            continue;
                        string sharedPath = string.Format(@"\\{0}\{1}", localhost, sharedFolder);
                        if (path.StartsWith(localFolder, StringComparison.OrdinalIgnoreCase))
                        {
                            sharedfile = path.ToLower().Replace(localFolder.ToLower(), sharedPath.ToLower());
                            return sharedfile;
                        }
                    }
                    // If not found in share
                    Console.WriteLine("{0} is not shared", path);
                }
                finally
                {
                    shares.Dispose();
                }
            }
            return sharedfile;
        }

        public static string ExtractTailLines(string file, int count)
        {
            if (File.Exists(file))
            {
                StringBuilder sb = new StringBuilder();
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Count(); i++)
                {
                    if (i < lines.Count() - count)
                        continue;
                    if (!string.IsNullOrEmpty(lines[i]))
                    {
                        sb.AppendLine(lines[i]);
                    }
                }
                return sb.ToString();
            }
            return null;
        }

        public static string GetSharedPathFromFile(string file)
        {
            string path = Path.GetDirectoryName(file);
            return FileHelper.GetSharedPath(path);
        }

        public static List<string> GetAndCleanLeastLines(Func<string, string, int> compareFunc, string[] lines)
        {
            List<string> leastLines = new List<string>();

            if (!lines.Any())
            {
                return leastLines;
            }

            List<int> leastLinesIndex = new List<int>();
            leastLinesIndex.Add(0);

            for (int i = 1; i < lines.Count(); i++)
            {
                string currentMinValue = lines[leastLinesIndex[0]];
                if (currentMinValue == null || compareFunc(currentMinValue, lines[i]) > 0)
                {
                    leastLinesIndex.Clear();
                    leastLinesIndex.Add(i);
                }
                else if (compareFunc(currentMinValue, lines[i]) == 0)
                {
                    leastLinesIndex.Add(i);
                }
            }

            foreach (int index in leastLinesIndex)
            {
                leastLines.Add(lines[index]);
                lines[index] = null; // clean the values for next iterator
            }

            return leastLines;
        }
    }
}
