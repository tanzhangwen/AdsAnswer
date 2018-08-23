//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="ImageWorkingFile.cs">
//      Created by: tomtan at 7/13/2015 1:54:53 PM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.AnswerData.Image
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AdsAnswer.Logging;

    public class ImageWorkingFile
    {
        string FileName;
        private TraceLog log;

        public ImageWorkingFile(string filename)
        {
            this.FileName = filename;
        }

        public ImageWorkingFile(string filename, TraceLog log)
            : this(filename)
        {
            this.log = log;
        }

        public List<Image> Read()
        {
            List<Image> list = new List<Image>();

            if (!File.Exists(this.FileName))
            {
                return list;
            }

            HashSet<string> unique = new HashSet<string>();

            using (StreamReader sr = new StreamReader(this.FileName, true))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    Image image = null;

                    try
                    {
                        image = Image.FromString(line.Trim());
                    }
                    catch (Exception ex)
                    {
                        ImageLogger.LogMessage(this.log, EventType.Error, ex.Message);
                        ImageLogger.LogMessage(this.log, EventType.Warning, "Invalid image line: {0}", line.Trim());
                        continue;
                    }

                    if (string.IsNullOrEmpty(image.SourceUrl))
                    {
                        ImageLogger.LogMessage(this.log, EventType.Warning, "Invalid image line: {0}", line.Trim());
                        continue;
                    }
                    if (!unique.Contains(image.SourceUrl))  // dedup
                    {
                        list.Add(image);
                        unique.Add(image.SourceUrl);
                    }
                }
            }
            return list;
        }
    }
}
