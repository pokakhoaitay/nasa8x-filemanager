using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace Nasa8x.Core.FileManager
{
    public class ScriptManager : IHttpHandler, IRequiresSessionState
    {

        public static HttpContext Context;

        private const string SESSION_ID = "sessionId";

        private static NameValueCollection _fileTypes;
        public static NameValueCollection FileTypes
        {
            get
            {
                return _fileTypes ?? (_fileTypes = new NameValueCollection
                                                       {
                                                           {"images", ".bmp;.gif;.png;.jpg;.jpeg"},
                                                           {
                                                               "videos",
                                                               ".mp4;.flv;.mpg;.mpeg;.avi;.mkv;.hdmov;.mov; .mpe;.wmv;.wma;.3g2;.3gp;.asf;.vob;.rm"
                                                               },
                                                           {
                                                               "audios",
                                                               ".mp3;.mid;.wav;.aif;.midi;.mka;.mpa;.mpga;.aac;.m3u;.ra"
                                                               },
                                                           {"Flash", ".swf;.fla"}
                                                       });
            }
        }
        

        private static string UploadPath
        {
            get { return FileHelpers.UploadPath; }
        }

        private static bool Debug
        {
            get { return bool.Parse(ConfigurationManager.AppSettings["DEBUG"]); }
        }

        private static bool AllowAction
        {
            get
            {
                if(!Debug)
                {
                    string _encryptString = Context.Request[SESSION_ID];
                    FormsAuthenticationTicket _userTicket = FormsAuthentication.Decrypt(_encryptString);

                    return !_userTicket.Expired;
                }

                return true;
            }
        }
            
      /*  private static double getTimeStamp()
        {

            return DateTime.UtcNow.Ticks;
        }*/


        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/xml";
            var action = context.Request["action"];
            var path = context.Request["path"];
            var input = context.Request["input"];

            Context = context;

          //  if (AllowAction)
            {
                try
                {


                    switch (action)
                    {
                        case "folder":
                        case "files":
                        case "search":

                            FetchItems(path, context);

                            break;

                        case "upload":
                            UploadFile(context);
                            break;

                        case "del":
                            var _items = context.Request["items"].Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                            FileHelpers.Delete(_items);

                            break;

                        case "newdir":
                            //var _n = context.Request["newname"];
                            FileHelpers.CreateFolder(input, path);
                            break;

                        case "rename":

                            //var _n2 = context.Request["newname"];
                            var _old = context.Request["oldname"];
                            FileHelpers.ReNameFileOrFolder(_old, input);

                            break;

                        case "move":
                            bool _overwrite;
                            bool.TryParse(context.Request["overwrite"], out _overwrite);
                            var _itemsMove = context.Request["items"].Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                            FileHelpers.Move(_itemsMove, path, _overwrite);

                            break;


                    }


                }
                catch (Exception ex)
                {
                    Logging.Log(action + ": " + ex);

                    throw new InvalidOperationException(ex.ToString());
                }
            }

            context.Response.Flush();

            context.Response.End();

        }

        #region FetchItems
        protected void FetchItems(string path, HttpContext context)
        {


            string[] _exts = null;
            var _allowType = context.Request["allowTypes"];
            if (!string.IsNullOrWhiteSpace(_allowType) && !string.IsNullOrWhiteSpace(FileTypes[_allowType.ToLower()]))
            {
                _exts = FileTypes[_allowType.ToLower()].Split(';');

            }

            bool _searchSubFolder;

            bool.TryParse(context.Request["searchInSub"], out _searchSubFolder);

            string _search = context.Request["search"];


            FileFilter _fileFilter = FileFilter.All;

            if (!string.IsNullOrWhiteSpace(_search))
            {
                _fileFilter = FileFilter.Files;
                _search = string.Format("*{0}*", _search);
            }

            if (string.IsNullOrWhiteSpace(_search))
                _search = "*.*";



            var _items = FileHelpers.FetchItems(path, _search, _exts, _searchSubFolder, _fileFilter);

            var _sb = new StringBuilder();

            _sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");


            // _sb.AppendFormat("<folder label=\"{0}\" path=\"{0}\">", AppPath);

            _sb.Append("<root>");

            foreach (var _fileItem in _items)
            {

                string _icon = "none.gif";

                string _itemType = "none";

                // var _id = _fileItem.Created.Ticks;

                if (_fileItem.IsFolder)
                {
                    _icon = "folder.gif";
                    _itemType = "folder";
                }

                else if (ImageHelpers.IsGraphic(_fileItem.Name))
                {
                    _itemType = "img";
                }
                if (_fileItem.IsExistsIcon)
                {
                    _icon = string.Format("{0}.gif", _fileItem.Extension.Replace(".", ""));

                }

                _sb.AppendFormat("<item name=\"{0}\" type=\"{1}\" path=\"{2}\" icon=\"{3}\" extension=\"{4}\" />", context.Server.HtmlEncode(_fileItem.Name), _itemType, context.Server.HtmlEncode(_fileItem.FullName), _icon, _fileItem.Extension);


            }


            _sb.Append("</root>");
            context.Response.Write(_sb);
        }


        #endregion


        #region Upload

        private static void UploadFile(HttpContext context)
        {

            string _url = context.Request["RemoteUrls"];
            try
            {
                // string[] _allowFileTypes = ConfigurationManager.AppSettings["ALLOW_FILE_TYPES"].Replace("*", string.Empty).Split(';');
                //FormsAuthenticationTicket _userTicket = FormsAuthentication.Decrypt(_encryptString);
                //if (!_userTicket.Expired && context.Request.Files.Count > 0)
                if (context.Request.Files.Count > 0 || !string.IsNullOrEmpty(_url))
                {

                    string _uploadToPath = context.Request["uploadToPath"];

                    bool _autoNameAndPath = false;

                    bool.TryParse(context.Request["autoNameAndPath"], out _autoNameAndPath);

                    if (string.IsNullOrEmpty(_uploadToPath))
                    {
                        _uploadToPath = UploadPath;
                    }


                    string _uploadPath = _autoNameAndPath
                                             ? FileHelpers.PathCombine(UploadPath,DateTime.UtcNow.ToString("yyyy/MM/dd"))
                                             : FileHelpers.PathCombine(UploadPath, _uploadToPath);

                    _uploadPath = context.Server.MapPath(_uploadPath);


                    if (!Directory.Exists(_uploadPath))
                    {
                        Directory.CreateDirectory(_uploadPath);
                    }

                    if (!string.IsNullOrEmpty(_url))
                    {

                        var _urls = _url.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);


                        context.Response.Write(string.Join(";", (from _s in _urls
                                                                 let _fileName = string.Format("{0}{1}", DateTime.UtcNow.Ticks, ".jpg")
                                                                 where Download(_s, Path.Combine(_uploadPath, _fileName))
                                                                 select _fileName).ToArray()));


                    }
                    else
                    {
                        // loop through all the uploaded files
                        for (int j = 0; j < context.Request.Files.Count; j++)
                        {

                            HttpPostedFile _uploadFile = context.Request.Files[j];
                            string _fileName = _uploadFile.FileName;
                            string _fileExt = Path.GetExtension(_fileName);

                            //  bool _allowUpload = _allowFileTypes.Any(fileType => fileType.ToLower() == _fileExt);
                            // Check allow upload with file extention

                            if (_uploadFile.ContentLength > 0)
                            {
                                _fileName = string.Format("{0}{1}", DateTime.UtcNow.Ticks, _fileExt);// DateTime.UtcNow.ToString("yyyy-MM-ddxHHmmss") + DateTime.UtcNow.Millisecond + _fileExt;

                                string _outPath = Path.Combine(_uploadPath, _fileName);

                                var _maxSize = context.Request["MaxImageSize"];

                                if (ImageHelpers.IsGraphic(_fileName) && !string.IsNullOrEmpty(_maxSize) && _maxSize.IndexOf("x") > 0)
                                {
                                    string[] _params = _maxSize.Split('x');
                                    int _w = int.Parse(_params[0]);
                                    int _h = int.Parse(_params[1]);

                                    ImageHelpers.ResizeStream(_uploadFile.InputStream, _outPath, _w, _h, null, 0, 0);
                                }
                                else
                                {

                                    UploadFile(_uploadFile.InputStream, _outPath);

                                }

                            }

                            System.Threading.Thread.Sleep(150);


                        }// en loop
                    }


                }
            }
            catch (Exception ex)
            {

                Logging.Log("Upload: " + ex);

                Logging.Log(_url);

                throw new IdentityNotMappedException(ex.ToString());
            }

            context.Response.Flush();
            context.Response.End();
        }


        public static void UploadFile(Stream data, string filepath)
        {
            // TODO: just hardcode filename for now
            //var filepath = HttpContext.Current.Server.MapPath(@"~\_test\testfile.txt");
            using (Stream file = File.OpenWrite(filepath))
            {
                CopyStream(data, file);
            }
        }
        private static void CopyStream(Stream input, Stream output)
        {
            var buffer = new byte[2 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }


        #endregion


        public static bool Download(string url, string fileName)
        {
            System.Threading.Thread.Sleep(100);

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.1.5) Gecko/20091102 Firefox/3.5.5";
            request.AllowWriteStreamBuffering = true;
            request.Referer = "http://www.google.com/";

            var response = (HttpWebResponse)request.GetResponse();

            // Check that the remote file was found. The ContentType
            // check is performed since a request for a non-existent
            // image file might be redirected to a 404-page, which would
            // yield the StatusCode "OK", even though the image was not
            // found.
            if ((response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Moved ||
                response.StatusCode == HttpStatusCode.Redirect) &&
                response.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
            {

                try
                {
                    // if the remote file was found, download oit
                    using (Stream inputStream = response.GetResponseStream())
                    using (Stream outputStream = File.OpenWrite(fileName))
                    {
                        var buffer = new byte[4096];
                        int bytesRead;
                        do
                        {
                            bytesRead = inputStream.Read(buffer, 0, buffer.Length);
                            outputStream.Write(buffer, 0, bytesRead);
                        } while (bytesRead != 0);
                    }

                    return true;

                }
                catch (Exception ex)
                {

                    Logging.Log("Download: " + ex);
                    return false;
                }

            }
            return false;



        }


        public Stream Download(string _URL)
        {
            Stream _stream = null;

            try
            {
                // Open a connection
                var _HttpWebRequest = (HttpWebRequest)WebRequest.Create(_URL);

                _HttpWebRequest.AllowWriteStreamBuffering = true;

                // You can also specify additional header values like the user agent or the referer: (Optional)
                _HttpWebRequest.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.1.5) Gecko/20091102 Firefox/3.5.5";
                _HttpWebRequest.Referer = "http://www.google.com/";

                // set timeout for 20 seconds (Optional)
                //_HttpWebRequest.Timeout = 20000;

                // Request response:
                WebResponse _WebResponse = _HttpWebRequest.GetResponse();

                // Open data stream:
                _stream = _WebResponse.GetResponseStream();

                // convert webstream to image
                // _tmpImage = Image.FromStream(_WebStream);

                // Cleanup
                _WebResponse.Close();
                _WebResponse.Close();



            }
            catch (Exception ex)
            {
                // Error
                //Console.WriteLine("Exception caught in process: {0}", _Exception);

                Logging.Log("Download: " + ex);

                return null;
            }

            return _stream;
        }

        public bool IsReusable
        {
            get { return true; }
        }
    }
}
