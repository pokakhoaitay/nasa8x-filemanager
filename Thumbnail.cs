using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web;

namespace Nasa8x.Core.FileManager
{
    public class Thumbnail : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {

            Thumbview(context);
           
        }

        public bool IsReusable
        {
            get { return true; }
        }

        #region Thumbview

        private static void Thumbview(HttpContext ctx)
        {

            string _file = ctx.Request.QueryString["f"];

            int _height = 0;
            int _width = 0;
            int _d = 100;
            bool _useThumbSize = false;
           // bool _isFullSize = false;
            //if (ctx.Request.QueryString["fullsize"] != null)
            //{
            //    try
            //    {
            //        _isFullSize = Convert.ToBoolean(ctx.Request["fullsize"]);
            //    }
            //    catch { }

            //}

            if (ctx.Request.QueryString["thumbsize"] != null)
            {
                _d = Int32.Parse(ctx.Request["thumbsize"]);
                _useThumbSize = true;
            }

            if (ctx.Request["h"] != null)
                _height = Int32.Parse(ctx.Request["h"]);


            if (ctx.Request["w"] != null)
                _width = Int32.Parse(ctx.Request["w"]);


            if (_useThumbSize)
            {
                _width = _d;
                _height = _d;
            }

            Bitmap _bitmap = CreateThumbnail(_file, _width, _height);

            SetContentType(_file, ctx);
            ctx.Response.Cache.SetCacheability(HttpCacheability.Public);
            ctx.Response.Cache.SetExpires(DateTime.Now.AddDays(1));
            _bitmap.Save(ctx.Response.OutputStream, GetImageFormat(GetFileExtension(_file)));
        }

        #endregion

        public static Bitmap CreateThumbnail(string fileName, int width, int height)
        {



            Bitmap _bmpOut;

            try
            {

                var _loBmp = new Bitmap(fileName);

                ImageFormat _loFormat = _loBmp.RawFormat;



                decimal _lnRatio;

                int _lnNewWidth = 0;

                int _lnNewHeight = 0;



                //*** If the image is smaller than a thumbnail just return it

                if (_loBmp.Width < width && _loBmp.Height < height)

                    return _loBmp;





                if (_loBmp.Width > _loBmp.Height)
                {

                    _lnRatio = (decimal)width / _loBmp.Width;

                    _lnNewWidth = width;

                    decimal lnTemp = _loBmp.Height * _lnRatio;

                    _lnNewHeight = (int)lnTemp;

                }

                else
                {

                    _lnRatio = (decimal)height / _loBmp.Height;

                    _lnNewHeight = height;

                    decimal _lnTemp = _loBmp.Width * _lnRatio;

                    _lnNewWidth = (int)_lnTemp;

                }



                // System.Drawing.Image imgOut =

                //      loBMP.GetThumbnailImage(lnNewWidth,lnNewHeight,

                //                              null,IntPtr.Zero);



                // *** This code creates cleaner (though bigger) thumbnails and properly

                // *** and handles GIF files better by generating a white background for

                // *** transparent images (as opposed to black)

                _bmpOut = new Bitmap(_lnNewWidth, _lnNewHeight);

                Graphics g = Graphics.FromImage(_bmpOut);

                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                g.FillRectangle(Brushes.White, 0, 0, _lnNewWidth, _lnNewHeight);

                g.DrawImage(_loBmp, 0, 0, _lnNewWidth, _lnNewHeight);

                _loBmp.Dispose();

            }

            catch
            {

                return null;

            }



            return _bmpOut;

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


        



        private static string GetFileExtension(string _fileName)
        {
            int _index = _fileName.LastIndexOf(".");
            string _extension = _fileName.Substring(_index).ToUpper();
            return _extension;

        }

        private static void SetContentType(string _fileName, HttpContext _context)
        {

            string _extension = GetFileExtension(_fileName);
            // Fix for IE not handling jpg image types
            if (string.Compare(_extension, "JPG", true) == 0)
                _context.Response.ContentType = "image/jpeg";
            else
                _context.Response.ContentType = "image/" + _extension;
        }


    }
}
