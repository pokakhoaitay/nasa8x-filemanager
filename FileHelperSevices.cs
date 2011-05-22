using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;


namespace Nasa8x.Core.FileManager
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [ScriptService]
    public class FileHelperServices : System.Web.Services.WebService
    {

       private JavaScriptSerializer _serializer;


        [WebMethod(EnableSession = true)]
       [ScriptMethod(ResponseFormat = ResponseFormat.Xml)]
        public string FetchFolders(string _path)
        {
            // System.Threading.Thread.Sleep(1000);

            var _items= FileHelpers.FetchItems(_path, null, null,false, FileFilter.Directories);

            var _sb = new StringBuilder();
            _sb.Append("<ul>");
            foreach (FileItem _fileItem in _items)
            {
                _sb.AppendFormat("<li id=\"{0}\" path=\"{1}\"><span>{2}</span>", _fileItem.FullName.Replace("/", "_"), _fileItem.FullName, _fileItem.Name);

                _sb.AppendFormat("<ul path=\"{0}\" class=\"ajax\"></ul>", _fileItem.FullName);

                _sb.Append("</li>");

            }
            _sb.Append("</ul>");


            var _html=_sb.ToString();



            var _object = new
                              {

                                  html =_html


                              };


            _serializer=new JavaScriptSerializer();
            return _serializer.Serialize(_object);




        }


        //[WebMethod(EnableSession = true)]
        //[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        //public List<FileItem> FetchFolders(string _path)
        //{
        //    // System.Threading.Thread.Sleep(1000);

        //    return FileHelpers.FetchItems(_path, null, null, FileFilter.Directories);

          

        //   //return new JavaScriptSerializer().Serialize(_items);


        //    //DataContractJsonSerializer serializer = new DataContractJsonSerializer(products.GetType());
        //    ////create a memory stream
        //    //MemoryStream ms = new MemoryStream();
        //    ////serialize the object to memory stream
        //    //serializer.WriteObject(ms, products);
        //    ////convert the serizlized object to string
        //    //string jsonString = Encoding.Default.GetString(ms.ToArray());
        //    ////close the memory stream
        //    //ms.Close();
        //    //return jsonString;

        //   //if (_items.Count == 0) return "";

        //   //var _sb = new StringBuilder();
        //   //_sb.Append("<ul>");
        //   //foreach (FileItem _fileItem in _items)
        //   //{
        //   //    _sb.AppendFormat("<li id=\"{0}\" path=\"{1}\"><span>{2}</span>", _fileItem.FullName.Replace("/", "_"), _fileItem.FullName, _fileItem.Name);

        //   //    _sb.AppendFormat("<ul path=\"{0}\" class=\"ajax\"></ul>", _fileItem.FullName);

        //   //    _sb.Append("</li>");

        //   //}
        //   //_sb.Append("</ul>");
        //   //return _sb.ToString();


        //    //return Server.HtmlEncode("<li id='6'><span>Tree Node 2-1</span><ul><li id='7'><span>Tree Node 2-1-1</span></li></ul></li>");

        //}


        //[WebMethod(EnableSession = true)]
        //[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        //public List<FileItem> FetchFolders(string _path)
        //{
        //    // System.Threading.Thread.Sleep(1000);

        //    //return FileHelpers.FetchItems(_path, null, null, FileFilter.Directories);

        //}

        //[WebMethod(EnableSession = true, BufferResponse = true)]
        //[ScriptMethod(ResponseFormat = ResponseFormat.Json)]

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<FileItem> FetchItems(string _path, string _searchTxt, string _sortOrder, int _filter)
        {
            // System.Threading.Thread.Sleep(1000);

            return FileHelpers.FetchItems(_path, _searchTxt, null,false, (FileFilter)_filter);

           

            //_serializer=new JavaScriptSerializer();

           // return _serializer.Serialize("");

            //return "/Upload";

        }
        


        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public bool NewFolder(string _name, string _currentPath)
        {
           // string _path = Server.MapPath(_currentPath + "/" + _name);
          
            return FileHelpers.CreateFolder(_name,_currentPath);

        }


        [WebMethod(EnableSession = true)]
       [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string Move(string _filesOrFolders, string _destination, bool _overwrite)
        {
            string[] _items = _filesOrFolders.Split('|');
            FileHelpers.Move(_items, _destination,_overwrite);

            return _items.Length.ToString();
        }
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public int Copy(string _filesOrFolders, string _destination,bool _overwrite)
        {
            string[] _items = _filesOrFolders.Split('|');
            return FileHelpers.Copy(_items, _destination, _overwrite);
        }

        [WebMethod(EnableSession = true)]
        
        public void ReName(string _oldName,string _newName)
        {
           FileHelpers.ReNameFileOrFolder(_oldName,_newName);

        }


        [WebMethod(EnableSession = true)]
        public int Delete(string _filesOrFolders)
        {
            string[] _items = _filesOrFolders.Split('|');
            return FileHelpers.Delete(_items);
        }

        [WebMethod(EnableSession = true)]
        public void Zip(string _filesOrFolders, string _name,string _pwd,string _destination)
        {

            FileHelpers.Zip(_filesOrFolders, _name, _pwd, _destination);

           
        }

    }
}
