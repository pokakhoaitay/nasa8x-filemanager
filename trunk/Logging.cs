using System;
using System.Configuration;
using System.IO;
using System.Web;

namespace Nasa8x.Core.FileManager
{
    public class Logging
    {

        public static HttpContext Context
        {
            get { return HttpContext.Current; }
        }

        private static String LoggingPath
        {
            get
            {
                // return HttpContext.Current.Server.MapPath();

                string _path = ConfigurationManager.AppSettings["LOGGING_PATH"];

                if (!Directory.Exists(Context.Server.MapPath(_path)))
                {

                    Directory.CreateDirectory(Context.Server.MapPath(_path));
                }

                return Context.Server.MapPath(_path);
            }
        }

        public static void Log(String msg)
        {

            string _fileLog = DateTime.Now.ToString("yyyy-MM-dd") + ".txt";

            //string 

            using (StreamWriter w = File.AppendText(Path.Combine(LoggingPath, _fileLog)))
            {
                Log(msg, w);
                // Close the writer and underlying file.
                w.Close();
            }

        }


        public static void Log(String logMessage, TextWriter w)
        {

            w.WriteLine("[{0}] : {1}", DateTime.Now.ToString("yyyyMMdd HH:mm:ss tt"), logMessage);
            w.WriteLine("-------------------------------");
            w.Flush();
        }

    }
}
