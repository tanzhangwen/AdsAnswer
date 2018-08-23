//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="Image.cs">
//      Created by: tomtan at 7/10/2015 10:02:34 AM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.AnswerData.Image
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Drawing;
    using System.Drawing.Imaging;
    using AdsAnswer.Logging;

    public class Image
    {
        public string SourceUrl;
        public int Width;
        public int Height;
        public string DownloadImageName;
        public string LocalImageName;

        public override string ToString()
        {
            return string.Format("{0}\t{1}\t{2}",
                Width, Height, SourceUrl);
        }

        public static Image FromString(string line)
        {
            Image image = new Image();
            string[] tmp = line.Split(new char[] { '\t' });

            if (tmp.Length >= 1)
            {
                image.Width = int.Parse(tmp[0]);
                if (tmp.Length >= 2)
                    image.Height = int.Parse(tmp[1]);
                if (tmp.Length >= 3)
                    image.SourceUrl = tmp[2];
            }
            return image;
        }

        public string GetExtFromUrl()
        {
            string ext = string.Empty;
            int idx = this.SourceUrl.LastIndexOf('.');
            if (idx < 0)
            {
                // If url like http://renlifang.msra.cn/portrait.aspx?id=9410
                return ext;
            }
            int extLen = this.SourceUrl.Length - idx;
            if (extLen <= 5 && extLen >= 3)
            {
                ext = this.SourceUrl.Substring(idx);
            }
            return ext;
        }

        public bool ExtractImageDimension(out int width, out int height)
        {
            width = height = 0;

            try
            {
                using (System.Drawing.Image image = new Bitmap(DownloadImageName))
                {
                    width = image.Width;
                    height = image.Height;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ScaleDefault()
        {
            if (string.IsNullOrEmpty(this.DownloadImageName) || !File.Exists(this.DownloadImageName))
                return false;

            using (System.Drawing.Image imageOrigin = new Bitmap(this.DownloadImageName))
            {
                if (this.Width != 0 && this.Height != 0)
                {
                    // Crop if needed, 1/3 on top and 2/3 on bot, or 1/2 on left and right
                    int x = 0, y = 0;
                    int tw = this.Width, th = this.Height;

                    int nx = 0, ny = 0;
                    int nw = imageOrigin.Width, nh = imageOrigin.Height;

                    if (this.Width * imageOrigin.Height != this.Height * imageOrigin.Width)
                    {
                        double originWHRatio = (double)(imageOrigin.Width) / imageOrigin.Height;
                        double targetWHRatio = (double)this.Width / this.Height;

                        if (targetWHRatio > originWHRatio)
                        {
                            // I'm thinner than target, crop height
                            nh = this.Height * imageOrigin.Width / this.Width;
                            ny = (imageOrigin.Height - nh) / 3;
                        }
                        else
                        {
                            // I'm fatter than target, crop width
                            nw = this.Width * imageOrigin.Height / this.Height;
                            nx = (imageOrigin.Width - nw) / 2;
                        }
                    }

                    using (Bitmap imageNew = new Bitmap(this.Width, this.Height, PixelFormat.Format16bppRgb565))
                    {
                        using (Graphics g = Graphics.FromImage(imageNew))
                        {
                            // Clear background to white
                            g.Clear(Color.White);

                            // set draw quality
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                            g.DrawImage(imageOrigin, new Rectangle(x, y, tw, th), new Rectangle(nx, ny, nw, nh), GraphicsUnit.Pixel);

                            imageNew.Save(this.LocalImageName, ImageFormat.Jpeg);
                            return true;
                        }
                    }
                }
                else
                {
                    using (Bitmap imageNew = new Bitmap(imageOrigin.Width, imageOrigin.Height, PixelFormat.Format16bppRgb565))
                    {
                        using (Graphics g = Graphics.FromImage(imageNew))
                        {
                            // Clear background to white
                            g.Clear(Color.White);

                            // set draw quality
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            g.DrawImage(imageOrigin, 0, 0);

                            imageNew.Save(this.LocalImageName, ImageFormat.Jpeg);
                            return true;
                        }
                    }
                }
            }
        }

        public bool ScaleNone()
        {
            if (this.DownloadImageName == null || this.LocalImageName == null)
                return false;

            if (!File.Exists(this.DownloadImageName))
                return false;

            try
            {
                File.Copy(this.DownloadImageName, this.LocalImageName, true);
                return true;
            }
            catch (Exception ex)
            {
                //ImageLogger.LogMessage(this.log, EventType.Warning, "Exception when copy from original image {0}", this.DownloadImageName);
                //ImageLogger.LogMessage(this.log, EventType.Error, ex.ToString());
                return false;
            }
        }
    }
}
