using System;
using System.IO;
using System.Web;
using System.Drawing;
using System.Drawing.Imaging;

namespace Nasa8x.Core.FileManager
{
    public sealed class ImageHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            string _relativePath = context.Request.AppRelativeCurrentExecutionFilePath.Replace(".img.axd", string.Empty);
            string _action = context.Request.QueryString["act"];

            if (!string.IsNullOrEmpty(_action) && (string.Compare(_action.ToLower(), "thumbview") == 0))
            {
                Thumbview(context, _relativePath);
            }
            else
            {
                WriteFile(context, _relativePath);

            }
        }


        private static void WriteFile(HttpContext context, string _relativePath)
        {
            string absolutePath = context.Server.MapPath(_relativePath);

            SetContentType(_relativePath, context);
            FileInfo info = new FileInfo(absolutePath);
            if (info.Exists)
            {

                SetConditionalGetHeaders(info.CreationTime);
                context.Response.TransmitFile(absolutePath);
            }
        }



        #region Thumbview

        private static void Thumbview(HttpContext context, string _relativePath)
        {


            if (!string.IsNullOrEmpty(_relativePath))
            {
                int quality, width = 0, height = 0;

                if (!string.IsNullOrEmpty(context.Request.QueryString["thumbsize"]))
                {
                    if (!int.TryParse(context.Request.QueryString["thumbsize"].Replace("px", string.Empty), out height))
                        height = 0;

                    if (!int.TryParse(context.Request.QueryString["thumbsize"].Replace("px", string.Empty), out width))
                        width = 0;

                }

                if (!string.IsNullOrEmpty(context.Request.QueryString["w"]))
                {
                    if (!int.TryParse(context.Request.QueryString["w"].Replace("px", string.Empty), out width))
                        width = 0;
                }
                if (!string.IsNullOrEmpty(context.Request.QueryString["h"]))
                {
                    if (!int.TryParse(context.Request.QueryString["h"].Replace("px", string.Empty), out height))
                        height = 0;
                }

                if (!int.TryParse(context.Request.QueryString["q"], out quality))
                    quality = 100;

                bool cache = true;
                if (context.Request.QueryString.ToString().IndexOf("nocache", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    cache = false;
                }

                string fileName = _relativePath;

                if (fileName[0].Equals('/'))
                {
                    fileName = fileName.Substring(1);
                }
                try
                {
                    string root = fileName.Contains("/") ? fileName.Substring(0, fileName.LastIndexOf('/'))
                        : "~/";

                    FileInfo fi = new FileInfo(context.Server.MapPath(root) + fileName.Substring(fileName.LastIndexOf('/')));
                    if (fi.Exists)
                    {
                        if (cache)
                        {
                            context.Response.Cache.SetCacheability(HttpCacheability.Public);
                            context.Response.Cache.AppendCacheExtension("must-revalidate, proxy-revalidate");
                            context.Response.Cache.SetMaxAge(TimeSpan.FromDays(30));   // One month
                            context.Response.Cache.SetExpires(DateTime.Now.AddDays(30));
                            context.Response.AddCacheDependency(new System.Web.Caching.CacheDependency(fi.FullName));
                            context.Response.Cache.SetLastModified(fi.LastWriteTimeUtc);

                            if (SetConditionalGetHeaders(fi.LastWriteTimeUtc, string.Format("|{0}|{1}|{2}|", quality, width, height), context))
                                return;
                        }
                        else
                        {
                            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                            context.Response.Cache.SetExpires(DateTime.Now.AddDays(-1));
                        }
                        int index = fileName.LastIndexOf(".") + 1;
                        string extension = fileName.Substring(index).ToUpperInvariant();

                        context.Response.AddHeader("content-disposition", "attachment; filename=" + fi.Name);

                        if (fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.ContentType = "image/jpeg";
                        }
                        else
                        {
                            context.Response.ContentType = "image/" + extension;

                        }
                        if (width == 0 && height == 0)
                        {
                            context.Response.TransmitFile(fi.FullName);
                        }
                        else
                        {
                            ProcessImage(quality, width, height, fi, context, extension);
                        }
                    }

                }
                catch (Exception)
                {
                    //    context.Response.Redirect("errorpage.aspx");
                }
            }


        }

        #endregion


        private static string GetFileExtension(string fileName)
        {
            int index = fileName.LastIndexOf(".");
            string extension = fileName.Substring(index).ToUpper();
            return extension;

        }

        private static void SetContentType(string fileName, HttpContext context)
        {

            string extension = GetFileExtension(fileName);
            // Fix for IE not handling jpg image types
            if (string.Compare(extension, "JPG", true) == 0)
                context.Response.ContentType = "image/jpeg";
            else
                context.Response.ContentType = "image/" + extension;
        }

        /// <summary>
        /// Writes ETag and Last-Modified headers and sets the conditional get headers.
        /// </summary>
        /// <param name="date">The date.</param>
        public static void SetConditionalGetHeaders(DateTime date)
        {
            HttpResponse response = HttpContext.Current.Response;
            HttpRequest request = HttpContext.Current.Request;

            string etag = "\"" + date.Ticks + "\"";
            string incomingEtag = request.Headers["If-None-Match"];

            response.Cache.SetETag(etag);
            response.Cache.SetLastModified(date);

            if (String.Compare(incomingEtag, etag) == 0)
            {
                response.Clear();
                response.StatusCode = (int)System.Net.HttpStatusCode.NotModified;
                response.End();
            }
        }


        #region IHttpHandler Members



        /// <summary>
        /// Writes ETag and Last-Modified headers and sets the conditional get headers.
        /// </summary>
        /// <param name="date">The date.</param>
        public static bool SetConditionalGetHeaders(DateTime date, string extraInfo, HttpContext context)
        {
            HttpResponse response = context.Response;
            HttpRequest request = context.Request;

            if (response == null || request == null)
            {
                return false;
            }
            string etag = string.Concat("\"", date.Ticks, extraInfo, "\"");
            string incomingEtag = request.Headers["If-None-Match"];

            response.AppendHeader("ETag", etag);

            if (etag.Equals(incomingEtag, StringComparison.Ordinal))
            {
                response.Clear();
                response.StatusCode = (int)System.Net.HttpStatusCode.NotModified;
                response.SuppressContent = true;
                return true;
            }

            return false;
        }

        private static void ProcessImage(int quality, int width, int height, FileInfo file, HttpContext context, string extension)
        {
            using (Image img = Image.FromFile(file.FullName))
            {
                Size newSize = CalculateSize(width, height, img);
                // Check if need to resize
                if (newSize.Width >= img.Width && newSize.Height >= img.Height)
                {
                    context.Response.TransmitFile(file.FullName);
                    return;
                }

                using (Bitmap _bitmap = new Bitmap(newSize.Width, newSize.Height))
                using (Graphics _graphic = Graphics.FromImage(_bitmap))
                {
                    if (quality > 80)
                    {
                        // Quality properties - hight quality (slower)
                        _graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        _graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        _graphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        _graphic.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    }
                    else
                    {
                        // Quality properties - low quality (faster)
                        _graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                        _graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                        _graphic.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
                        _graphic.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                    }
                    _graphic.DrawImage(img, 0, 0, newSize.Width, newSize.Height);

                    if (extension.Equals("bmp", StringComparison.OrdinalIgnoreCase) || extension.Equals("png", StringComparison.OrdinalIgnoreCase))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            _bitmap.Save(ms, GetImageFormat(extension));
                            StreamCopy(ms, context.Response.OutputStream);
                        }
                    }
                    else
                    {
                        if (quality >= 50)
                        {
                            using (EncoderParameters encoderParameters = new EncoderParameters(1))
                            {
                                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);
                                _bitmap.Save(context.Response.OutputStream, GetImageCodec(extension), encoderParameters);
                            }
                        }
                        else
                        {
                            _bitmap.Save(context.Response.OutputStream, GetImageFormat(extension));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// copy one stream to another
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        internal static void StreamCopy(Stream input, Stream output)
        {
            input.Position = 0;
            byte[] buffer = new byte[1024];
            int read;
            do
            {
                read = input.Read(buffer, 0, buffer.Length);
                output.Write(buffer, 0, read);
            } while (read > 0);
        }


        private static ImageCodecInfo GetImageCodec(string extension)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FilenameExtension.Contains(extension))
                {
                    return codec;
                }
            }
            return codecs[1];
        }

        private static ImageFormat GetImageFormat(string extension)
        {
            switch (extension)
            {
                case "JPG":
                case "JPEG":
                    return ImageFormat.Jpeg;
                case "GIF":
                    return ImageFormat.Gif;
                case "BMP":
                    return ImageFormat.Bmp;
                case "PNG":
                    return ImageFormat.Png;
                case "TIFF":
                case "TIF":
                    return ImageFormat.Tiff;
                default:
                    return ImageFormat.Jpeg;
            }
        }


        /// <summary>
        /// Calculate the final image dimentions
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        private static Size CalculateSize(int width, int height, Image image)
        {
            // Only width defined
            if (width > 0 && height < 1)
            {
                if (image.Width <= width)
                {
                    return new Size(image.Width, image.Height);
                }
                float dim = image.Height / (float)image.Width;
                return new Size(width, (int)(dim * width));
            }

            // Only height defined
            if (width < 1 && height > 0)
            {
                if (image.Height <= height)
                {
                    return new Size(image.Width, image.Height);
                }
                else
                {
                    float dim = (float)image.Width / (float)image.Height;
                    return new Size((int)(dim * (float)height), height);
                }
            }
            // Widht and height defined
            if (width > 0 && height > 0)
            {
                /*
                if (image.Width < width && image.Height < height)
                {
                    return new Size(image.Width, image.Height);
                }
                
                float dim = (float)image.Width / image.Height;
                int originWidth = width;
                int originHeight = height;
                //Check if the width is ok
                if (image.Width < originWidth)
                    width = image.Width;
                height = (int)(width / dim);
                // Check if still the height is too big
                if (height > originHeight)
                {
                    height = image.Height >= originHeight ? originHeight : image.Height;
                    width = (int)(height * dim);
                }
                 * */
                return new Size(width, height);

            }
            // Not width and not height defined so use original image sizes
            return new Size(image.Width, image.Height);
        }

        #endregion

        public bool IsReusable
        {
            get {return true; }
        }
    }
}
