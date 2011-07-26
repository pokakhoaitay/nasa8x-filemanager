using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

namespace Nasa8x.Core.FileManager
{
    public class FileHelpers
    {


        protected static HttpContext Context
        {

            get { return HttpContext.Current; }
        }

        public static string AppPath
        {
            get
            {

                string _p = ConfigurationManager.AppSettings["APP_PATH"];
                if (string.IsNullOrEmpty(_p))
                    _p = Context.Request.ApplicationPath;
                return _p;


            }
        }
      
        public static string UploadPath
        {
            get
            {

                return PathCombine(AppPath, ConfigurationManager.AppSettings["UPLOAD_PATH"]);


            }
        }

        public static string CachePath
        {
            get
            {

                return PathCombine(AppPath, ConfigurationManager.AppSettings["CACHE_PATH"]);


            }
        }
        


        private static string IconPath
        {
            get
            {
                return Context.Server.MapPath(PathCombine(AppPath, ConfigurationManager.AppSettings["ICON_PATH"]));
            }
        }


        public static string PathCombine(string a,string b)
        {
            return string.Format("{0}/{1}",a,b).Replace("//", "/");
        }

        public static string RelativePath(string _fullPath,bool isFolder)
        {
            string _appFullPath = !String.IsNullOrEmpty(AppPath) ? Context.Server.MapPath(AppPath) : Context.Request.PhysicalApplicationPath;//  Context.Server.MapPath("/");

            if(isFolder)
            {
                _appFullPath = Context.Server.MapPath(UploadPath);
            }
           
            return _fullPath.Replace(_appFullPath, "/").Replace("\\", "/");

           /* if(ImageHelpers.IsGraphic(_fullPath))
            {
                string _appFullPath = !String.IsNullOrEmpty(AppPath) ? Context.Server.MapPath(AppPath) : Context.Request.PhysicalApplicationPath;//  Context.Server.MapPath("/");
                //string _appFullPath =  Context.Request.PhysicalApplicationPath;//  Context.Server.MapPath("/");
                string _path = _fullPath.Replace(_appFullPath, "/").Replace("\\", "/");
                return _path;      
            }
            else
            {

                string _appFullPath = Context.Server.MapPath(AppPath);
                return _fullPath.Replace(_appFullPath, "/").Replace("\\", "/");

                
            }*/


          

        }




        #region FormatSize

        public static string FormatSize(long size)
        {
            double s = Convert.ToDouble(size);

            string[] format = new string[] { "{0} Bytes", "{0} Kb", "{0} Mb", "{0} Gb", "{0} Tb", "{0} Pb", "{0} Eb" };

            int i = 0;


            while (i < format.Length && s >= 1024)
            {
                s = (int)(100 * s / 1024) / 100.0;


                i++;
            }


            return string.Format(format[i], s);
        }

        #endregion


        public static bool IsExistsIcon(string _extension)
        {
            return File.Exists(IconPath + _extension.Replace(".", "") + ".gif");
        }


        #region FetchItems

        public static List<FileItem> FetchItems(string _path, string _searchTxt, string[] _extensions, bool _searchSubFolder, FileFilter _filter)
        {

            // if (IsRemoveAppPath)
            _path = Context.Server.MapPath(PathCombine(UploadPath, _path));

           
            var _list = new List<FileItem>();

            if (Directory.Exists(_path))
            {

                // List Folders

                if (_filter == FileFilter.All || _filter == FileFilter.Directories)
                {
                    string[] _folders = Directory.GetDirectories(_path);


                    foreach (string _f in _folders)
                    {
                        var _item = new FileItem();
                        var _dir = new DirectoryInfo(_f);
                        _item.Name = _dir.Name;
                        _item.FullName = RelativePath(_dir.FullName,true);
                        _item.Created = _dir.CreationTimeUtc;
                        _item.FileCount = _dir.GetFiles().LongLength;
                        _item.SubFolderCount = _dir.GetDirectories().LongLength;
                        _item.IsFolder = true;
                        _list.Add(_item);
                    }

                }

                // List Files

                if (_filter == FileFilter.All || _filter == FileFilter.Files)
                {


                    //string[] _files = Directory.GetFiles(_path, _searchTxt, _searchSubFolder?SearchOption.AllDirectories:SearchOption.TopDirectoryOnly);

                    var _files = Directory.EnumerateFiles(_path, _searchTxt,_searchSubFolder? SearchOption.AllDirectories: SearchOption.TopDirectoryOnly);

                    if(_extensions!=null && _extensions.Length>0)
                          _files =  _files.Where(s => _extensions.Any(ext => Path.GetExtension(s)!=null && ext.ToLower() == Path.GetExtension(s).ToLower())).ToList();


                    foreach (string _f in _files)
                    {

                        var _item = new FileItem();
                        var _file = new FileInfo(_f);
                        _item.Name = _file.Name;
                        _item.FullName = RelativePath(_file.FullName,false);
                        _item.Created = _file.CreationTimeUtc;
                        _item.LastAccessDate = _file.LastAccessTimeUtc;
                        _item.LastWriteDate = _file.LastWriteTimeUtc;
                        _item.IsFolder = false;
                        _item.IsExistsIcon = IsExistsIcon(_file.Extension);
                        _item.Size = FormatSize(_file.Length);
                        _item.Extension = _file.Extension;
                        _item.Attributes = _file.Attributes.ToString();
                        _list.Add(_item);
                    }
                }


            }

            return _list;


        }

        #endregion

        #region ReName File Or Folder

        public static void ReNameFileOrFolder(string _oldName, string _newName)
        {


            _oldName = UploadPath + _oldName;
            _newName = UploadPath + _newName;


            string _path = Context.Server.MapPath(_oldName);
            string _newPath = Context.Server.MapPath(_newName);
            try
            {
                if (Directory.Exists(_path) && _path != _newPath)
                {
                    Directory.Move(_path, _newPath);
                }

                if (File.Exists(_path) && _path != _newPath)
                {
                    File.Move(_path, _newPath);

                }


            }
            catch (Exception ex)
            {

                throw new IOException(ex.ToString());



            }


        }

        #endregion

        #region Create Folder
        public static bool CreateFolder(string _name, string _currentPath)
        {
           // string _path = _currentPath + "/" + _name;


            string  _path = UploadPath + _currentPath+"/"+_name;


            _path = Context.Server.MapPath(_path);

            try
            {
                if (!Directory.Exists(_path))
                {
                    Directory.CreateDirectory(_path);
                }
                return true;
            }
            catch (Exception ex)
            {

                throw new IOException(ex.ToString());

            }


        }
        #endregion

        #region Move Items

        public static void Move(string[] _items, string _destination, bool _overwrite)
        {
            try
            {
                string _destinationPath = _destination;

                _destinationPath = UploadPath + _destination;

                _destinationPath = Context.Server.MapPath(_destinationPath);


                foreach (string _item in _items)
                {
                    if (_item != _destination)
                    {

                        string _itemPath = _item;

                        _itemPath = UploadPath + _item;

                        _itemPath = Context.Server.MapPath(_itemPath);
                        // Check Item is Folder
                        if (Directory.Exists(_itemPath))
                        {
                            var _directoryInfo = new DirectoryInfo(_itemPath);

                            if (Directory.Exists(_destinationPath + "\\" + _directoryInfo.Name) && _overwrite)
                            {
                                Move(_directoryInfo, _destinationPath + "\\" + _directoryInfo.Name);
                            }
                            else
                            {
                                Directory.Move(_itemPath, _destinationPath + "\\" + _directoryInfo.Name);
                            }


                        }
                        // Check File Item
                        if (File.Exists(_itemPath))
                        {
                            var _fileInfo = new FileInfo(_itemPath);

                            if (File.Exists(_destinationPath + "\\" + _fileInfo.Name) && _overwrite)
                            {
                                File.Delete(_destinationPath + "\\" + _fileInfo.Name);
                            }

                            File.Move(_itemPath, _destinationPath + "\\" + _fileInfo.Name);

                        }


                    }
                }


            }
            catch (Exception ex)
            {

                throw new IOException(ex.ToString());
            }



        }

        #endregion

        #region MoveFolder

        public static void Move(DirectoryInfo _directoryInfo, string _destination)
        {
            DirectoryInfo[] _subs = _directoryInfo.GetDirectories();

            foreach (DirectoryInfo _sub in _subs)
            {

                if (Directory.Exists(_destination + "\\" + _sub.Name))
                {
                    Move(_sub, _destination + "\\" + _sub.Name);
                }
                else
                {
                    Directory.Move(_sub.FullName, _destination);
                }

            }
            //Move Files
            FileInfo[] _files = _directoryInfo.GetFiles();
            foreach (FileInfo _fileInfo in _files)
            {
                if (File.Exists(_destination + "\\" + _fileInfo.Name))
                {
                    File.Delete(_destination + "\\" + _fileInfo.Name);

                }
                File.Move(_fileInfo.FullName, _destination);

            }


        }

        #endregion

        #region Copy Items

        public static int Copy(string[] _items, string _destination, bool _overwrite)
        {
            string _destinationPath = UploadPath + _destination;
            _destinationPath = Context.Server.MapPath(_destinationPath);

            int _itemCount = 0;

            foreach (string _item in _items)
            {
                if (_item != _destination)
                {
                    string _itemPath = _item;
                   
                        _itemPath = UploadPath + _item;


                    _itemPath = Context.Server.MapPath(_itemPath);

                    if (Directory.Exists(_itemPath))
                    {
                        CopyFolder(_itemPath, _destinationPath, _overwrite);
                    }
                    if (File.Exists(_itemPath))
                    {
                        var _fileInfo = new FileInfo(_itemPath);

                        File.Copy(_itemPath, _destinationPath + "\\" + _fileInfo.Name, _overwrite);
                    }

                    _itemCount++;
                }
            }

            return _itemCount;


        }

        #endregion

        #region Delete Items

        public static int Delete(string[] _items)
        {
            try
            {
                foreach (string _item in _items)
                {
                    string _path = _item;
             
                        _path = UploadPath + _item;
                    _path = Context.Server.MapPath(_path);
                    if (Directory.Exists(_path))
                    {
                        Directory.Delete(_path, true);
                    }
                    else
                    {
                        File.Delete(_path);
                    }
                }
                return _items.Length;
            }
            catch (Exception ex)
            {

                throw new IOException(ex.ToString());
            }



        }

        #endregion

        #region Copy Folder

        public static void CopyFolder(string _source, string _destination, bool _overwriteExisting)
        {
            try
            {
                if (_destination[_destination.Length - 1] != Path.DirectorySeparatorChar)
                    _destination += Path.DirectorySeparatorChar;
                if (!Directory.Exists(_destination)) Directory.CreateDirectory(_destination);
                string[] _files = Directory.GetFileSystemEntries(_source);
                foreach (string _element in _files)
                {
                    if (Directory.Exists(_element))
                        CopyFolder(_element, _destination + Path.GetFileName(_element), _overwriteExisting);
                    else
                        File.Copy(_element, _destination + Path.GetFileName(_element), _overwriteExisting);
                }

            }
            catch (Exception ex)
            {

                throw new IOException(ex.ToString());
            }


        }
        #endregion


        #region Zip

        public static void Zip(string _filesOrFolders, string _name, string _pwd, string _destination)
        {
            string[] _items = _filesOrFolders.Split('|');


            
                _destination = UploadPath + _destination;
          

            string _currentF = Context.Server.MapPath(_destination) + "\\";

            var _lists = new ArrayList();

            foreach (string _item in _items)
            {


                string _itemPath = _item;
              
                    _itemPath = UploadPath + _item;

                string _path = Context.Server.MapPath(_itemPath);

                _lists.Add(_path);

            }


            //Context.Response.Write(_currentFolder);
            //Context.Response.Write(string.Join(",", (string[])_lists.ToArray(typeof(string))));


            if (string.IsNullOrEmpty(_name))
            {

                _name = DateTime.UtcNow.ToString("yyyy-MM-dd-hhmmss") + DateTime.UtcNow.Millisecond;
            }

            ZipHelper.ZipFoldersAndFiles(_lists, _currentF, "\\" + _name + ".zip", _pwd, true);
        }

        #endregion

        public static byte[] Read(string filePath)
        {

            return File.Exists(filePath) ? File.ReadAllBytes(filePath) : null;
        }

       
        
    }
}
