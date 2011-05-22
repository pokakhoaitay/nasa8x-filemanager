using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.SessionState;


namespace Nasa8x.Core.FileManager
{
    public sealed class Upload : IHttpHandler, IRequiresSessionState
    {
        private const string FILE_UPLOAD_SESSIONID = "FileUploadSessionId";


        public static string AppPath
        {
            get
            {

                string _p = ConfigurationManager.AppSettings["AppPath"];
                if (string.IsNullOrEmpty(_p))
                    _p = HttpContext.Current.Request.ApplicationPath;


                return _p;


            }
        }





        public void ProcessRequest(HttpContext context)
        {
            UploadFile(context);
        }

        #region Upload File

        private void UploadFile(HttpContext context)
        {

            try
            {

                //string _encryptString = context.Request.QueryString[FILE_UPLOAD_SESSIONID];
                //FormsAuthenticationTicket _userTicket = FormsAuthentication.Decrypt(_encryptString);

                string[] _allowFileTypes = ConfigurationManager.AppSettings["AllowFileTypes"].Split(';');

                //if (!_userTicket.Expired && context.Request.Files.Count > 0)
                if (context.Request.Files.Count > 0)
                {

                    string _uploadPath = null;// = context.Server.MapPath(CurrentFolder);

                    string _dir = context.Request["dir"];
                    string _autoName = context.Request["autoname"];

                    if (string.IsNullOrEmpty(_dir))
                    {

                        _dir = "/";


                    }


                    _dir = AppPath + Nasa8x.Core.Encryptor.Decode(context.Request.QueryString["s"]) + _dir;

                    _uploadPath = context.Server.MapPath(_dir);


                    if (!Directory.Exists(_uploadPath))
                    {
                        Directory.CreateDirectory(_uploadPath);
                    }



                    // loop through all the uploaded files
                    for (int j = 0; j < context.Request.Files.Count; j++)
                    {

                        HttpPostedFile _uploadFile = context.Request.Files[j];
                        string _fileName = _uploadFile.FileName;
                        string _fileExt = GetFileExtension(_fileName);
                        bool _allowUpload = _allowFileTypes.Any(_fileType => _fileType.ToLower().Replace("*", "") == _fileExt);

                        // Check allow upload with file extention

                        if (_allowUpload)
                        {
                            if (_autoName == "1")
                                _fileName = DateTime.UtcNow.ToString("yyyy-MM-dd-hhmmss") + DateTime.UtcNow.Millisecond + _fileExt;

                            if (_uploadFile.ContentLength > 0)
                            {

                                _uploadFile.SaveAs(Path.Combine(_uploadPath, _fileName));

                            }
                        }


                    }
                }
            }
            catch (Exception ex)
            {

                ForumContext.Log(ex.ToString());
                throw new InvalidOperationException(ex.ToString());
            }


            HttpContext.Current.Response.Write(" ");
        }

        private static string GetFileExtension(string fileName)
        {
            int _index = fileName.LastIndexOf(".");
            return fileName.Substring(_index).ToLower();
        }

        #endregion

        public bool IsReusable
        {
            get { return true; }
        }
    }
}
