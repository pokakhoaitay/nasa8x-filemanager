using System;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Web;

namespace Nasa8x.Core.FileManager
{
    public class ImageHandler : IHttpHandler
    {

        //private static string AppPath
        //{
        //    get { return ConfigurationManager.AppSettings["APP_PATH"]; }
        //}

        //private static string UploadPath
        //{
        //    get { return string.Format("{0}{1}",AppPath,ConfigurationManager.AppSettings["UPLOAD_PATH"]) Path.Combine(AppPath, ); }
        //}

        //private static string CachePath
        //{
        //    get { return Path.Combine(AppPath,ConfigurationManager.AppSettings["CACHE_PATH"]); }
        //}


       /* private static DateTime ConvertTimestamp(double timestamp)
        {
            return Convert.ToDateTime(timestamp);// Utils.ConvertFromUnixTimestamp(timestamp);
        }*/


        private static string GetFilePath(HttpContext context)
        {
            var _photoName = context.Request.QueryString["f"] ?? context.Request.QueryString[0];

            if (string.IsNullOrEmpty(_photoName) || _photoName == "notfound" || _photoName == "notfound.jpg")
            {

                return null;
            }


            var _f = _photoName.Substring(0, _photoName.IndexOf("."));

            long _date;


            long.TryParse(_f, out _date);


            if (long.TryParse(_f, out _date))
            {
                string _subPhotoPath = new DateTime(_date).ToString("yyyy/MM/dd");

                //var _p = new string[] {UploadPath, _subPhotoPath, _photoName};
               return Path.Combine(FileHelpers.UploadPath,_subPhotoPath, _photoName);
                

                //Logging.Log(p);
                //return p;
            }

            if (!string.IsNullOrWhiteSpace(FileHelpers.AppPath))
                return FileHelpers.PathCombine(FileHelpers.AppPath,_photoName);

            return _photoName;


        }


        private static Size GetSize(HttpContext context)
        {

            int _height=0;
            int _width=0;

            int.TryParse(context.Request["h"], out _height);
            int.TryParse(context.Request["w"], out _width);

            if (context.Request["size"] != null)
            {
                int _size;
                int.TryParse(context.Request["size"], out _size);

                _width = _height = _size;
            }


            return new Size(_width, _height);
        }



        #region Class Methods

        /// <summary>
        /// Check if the ETag that sent from the client is match to the current ETag.
        /// If so, set the status code to 'Not Modified' and stop the response.
        /// </summary>
        private static bool CheckETag(HttpContext context, string eTagCode)
        {
            string ifNoneMatch = context.Request.Headers["If-None-Match"];
            if (eTagCode.Equals(ifNoneMatch, StringComparison.Ordinal))
            {
                context.Response.AppendHeader("Content-Length", "0");
                context.Response.StatusCode = (int)HttpStatusCode.NotModified;
                context.Response.StatusDescription = "Not modified";
                context.Response.SuppressContent = true;
                context.Response.Cache.SetCacheability(HttpCacheability.Public);
                context.Response.Cache.SetETag(eTagCode);
                context.Response.Cache.SetExpires(DateTime.Now.AddYears(1));
                context.Response.End();
                return true;
            }

            return false;
        }
        /*
        private static ImageFormat GetImageFormat(HttpContext context)
        {
            string imageTypeParameter = context.Request["format"];
            if (!string.IsNullOrEmpty(imageTypeParameter))
            {
                switch (imageTypeParameter.ToLowerInvariant())
                {
                    case "png":
                        return ImageFormat.Png;
                    case "gif":
                        return ImageFormat.Gif;
                    case "jpg":
                        return ImageFormat.Jpeg;
                    case "tif":
                        return ImageFormat.Tiff;
                }
            }

            return ImageFormat.Jpeg;
        }
         * 
         * */

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

        #endregion

        public void ProcessRequest(HttpContext context)
        {
            string filePath = GetFilePath(context);

            if (string.IsNullOrEmpty(filePath) || !ImageHelpers.IsGraphic(filePath))
            {
                context.Response.Redirect("/blank.gif");
                return;
            }


            //string cachePath = context.Server.MapPath(UploadPath + filePath);// Path.Combine(UploadPath, filePath);
            string cachePath = context.Server.MapPath(filePath);// Path.Combine(UploadPath, filePath);

            var _size = GetSize(context);


            if (_size.Width != 0 || _size.Height != 0)
            {
                string _s = string.Format(".{0}x{1}", _size.Width, _size.Height);

                string _x = context.Request["rotate"];

                _s += !string.IsNullOrEmpty(_x) ? "x" + _x : string.Empty;

                _x = context.Request["zoom"];

                _s += !string.IsNullOrEmpty(_x) ? "x" + _x : string.Empty;

                _x = context.Request["greyscale"];

                _s += !string.IsNullOrEmpty(_x) ? "xGreyscale" + _x : string.Empty;

                cachePath = context.Server.MapPath(FileHelpers.CachePath + filePath.Insert(filePath.LastIndexOf("."), _s));
            }


            string eTag = "\"" + cachePath.GetHashCode() + "\"";
            if (CheckETag(context, eTag))
            {
                return;
            }

            ImageFormat imageFormat = GetImageFormat(Path.GetExtension(filePath));


            byte[] imageData = FileHelpers.Read(cachePath);
            if (imageData == null)
            {

                //var _source = context.Server.MapPath(UploadPath+ filePath);
                var _source = context.Server.MapPath(filePath);
                ImageHelpers.ResizeImage(_source, cachePath, _size);
                imageData = FileHelpers.Read(cachePath);

            }

            context.Response.Cache.SetCacheability(HttpCacheability.Public);
            context.Response.Cache.SetETag(eTag);
            context.Response.Cache.SetExpires(DateTime.Now.AddYears(1));
            context.Response.ContentType = "image/" + imageFormat.ToString().ToLower();
            context.Response.AppendHeader("Content-Length", imageData.Length.ToString());
            context.Response.OutputStream.Write(imageData, 0, imageData.Length);
            context.Response.Flush();


        }

        public bool IsReusable
        {
            get { return true; }
        }
    }
}
