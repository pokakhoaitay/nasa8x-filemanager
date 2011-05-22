using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;

namespace Nasa8x.Core.FileManager
{
    /// <summary>
    /// Summary description for ZipHelper
    /// </summary>
    public class ZipHelper
    {
        private readonly Crc32 crc;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ZipHelper()
        {
            crc = new Crc32();
        }


        /// <summary>
        /// Adds files to a zip file.
        /// </summary>
        /// <param name="zipFilename">Name of the zip file. If it does not exist, it will be created. If it exists, it will be updated.</param>
        /// <param name="sourceFolder">Name of the folder from which to add files.</param>
        /// <param name="fileMask">Name of the file to add to the zip file. Can include wildcards.</param>
        /// <param name="recursive">Specifies if the files in the sub-folders of <paramref name="sourceFolder"/> should also be added.</param>
        /// <param name="password">Specifies the password to be used for the zip file.  If blank, don't use a password</param>
        /// <returns>True if successfull</returns>
        public bool AddFilesToZip(string zipFilename, string sourceFolder, string fileMask, bool recursive, string password)
        {
            try
            {
                _AddFilesToZip(zipFilename, sourceFolder, fileMask, recursive, password);
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Extract files from a zip file, preserving the structure
        /// </summary>
        /// <param name="zipFilename">Name of the zip file</param>
        /// <param name="destFolder">Name of the folder to extract to</param>
        /// <param name="password">Password for the zip file.  If blank there is no password.</param>
        /// <returns>True if successfull</returns>
        public bool ExtractFilesFromZip(string zipFilename, string destFolder, string password)
        {
            try
            {
                _ExtractFilesFromZip(zipFilename, destFolder, password, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #region Adds files to a zip file
        /// <summary>
        /// Adds files to a zip file.
        /// </summary>
        /// <param name="zipFilename">Name of the zip file. If it does not exist, it will be created. If it exists, it will be updated.</param>
        /// <param name="sourceFolder">Name of the folder from which to add files.</param>
        /// <param name="fileMask">Name of the file to add to the zip file. Can include wildcards.</param>
        /// <param name="recursive">Specifies if the files in the sub-folders of <paramref name="sourceFolder"/> should also be added.</param>
        /// <param name="password">Specifies the password to be used for the zip file.  If blank, don't use a password</param>
        private void _AddFilesToZip(
            string zipFilename,
            string sourceFolder,
            string fileMask,
            bool recursive,
            string password)
        {
            if (!Directory.Exists(sourceFolder))
            {
                throw new ArgumentException("sourceFolder does not exist", "sourceFolder");
            }

            ZipInputStream zipInputStream = null;
            if (File.Exists(zipFilename))
            {
                zipInputStream = new ZipInputStream(
                    new FileStream(zipFilename, FileMode.Open));
                zipInputStream.Password = password;
            }

            string tempFileName = Path.GetTempFileName();
            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(
                                    tempFileName,
                                    FileMode.OpenOrCreate,
                                    FileAccess.Write,
                                    FileShare.None);
                {
                    using (ZipOutputStream zipOutputStream = new ZipOutputStream(fileStream))
                    {
                        zipOutputStream.Password = password;
                        zipOutputStream.SetLevel(6); // 0 - store only to 9 - means best compression

                        ZipDirectory(zipInputStream, sourceFolder, fileMask, zipOutputStream, recursive);

                        zipOutputStream.Finish();
                        zipOutputStream.Close();
                    }

                    fileStream.Close();
                    if (zipInputStream != null) zipInputStream.Close();

                    if (File.Exists(zipFilename))
                        File.Delete(zipFilename);
                    File.Copy(tempFileName, zipFilename);
                    File.Delete(tempFileName);
                }
            }
            finally
            {
                if (fileStream != null) fileStream.Close();
                if (File.Exists(tempFileName)) File.Delete(tempFileName);
            }
        }

        /// <summary>
        /// Add directory to open archive.
        /// </summary>
        /// <param name="zipInputStream">Specifies input stream of the zip file for the update strategy.</param>
        /// <param name="sourceFolder">Name of the folder from which to add files.</param>
        /// <param name="fileMask">Name of the file to add to the zip file. Can include wildcards.</param>
        /// <param name="zipOutputStream">Specifies output stream to archive data.</param>
        /// <param name="processSubDirs">Specifies if the files in the sub-folders of <paramref name="sourceFolder"/> should also be added.</param>
        private void ZipDirectory(
            ZipInputStream zipInputStream,
            string sourceFolder,
            string fileMask,
            ZipOutputStream zipOutputStream,
            bool processSubDirs)
        {
            StringCollection zippedFilesList = new StringCollection();
            foreach (string fileName in GetListOfFilesToZip(sourceFolder, fileMask, processSubDirs))
            {
                ZipFile(sourceFolder, fileName, zipOutputStream);
                zippedFilesList.Add(PatchKnownProblems(MakePathRelative(sourceFolder, fileName)));
            }

            if (zipInputStream != null)
            {
                ZipEntry zipEntry;
                while ((zipEntry = zipInputStream.GetNextEntry()) != null)
                {
                    if (!zippedFilesList.Contains(zipEntry.Name))
                    {
                        ZipEntry newZipEntry = new ZipEntry(zipEntry.Name);
                        newZipEntry.DateTime = zipEntry.DateTime;
                        newZipEntry.Size = zipEntry.Size;

                        AddDataToZip(zipOutputStream, newZipEntry, zipInputStream);
                    }
                }
            }
        }

        /// <summary>
        /// Add file to open archive.
        /// </summary>
        /// <param name="sourceFolder">Name of the folder from which to add files.</param>
        /// <param name="fileName">Name of the file to add</param>
        /// <param name="zipOutputStream">Specifies output stream to archive data.</param>
        private void ZipFile(string sourceFolder, string fileName, ZipOutputStream zipOutputStream)
        {
            using (FileStream fileStream = File.OpenRead(fileName))
            {
                ZipEntry entry = new ZipEntry(
                    ZipEntry.CleanName(
                        PatchKnownProblems(MakePathRelative(sourceFolder, fileName))));
                entry.DateTime = DateTime.Now;
                entry.Size = fileStream.Length;

                AddDataToZip(zipOutputStream, entry, fileStream);

                fileStream.Close();
            }
        }

        /// <summary>
        /// Transform file path from full path to relative.
        /// </summary>
        /// <param name="baseFolder">Name of the folder that will be cut from path</param>
        /// <param name="path">File full path that you need to make relative.</param>
        /// <returns>Relative path to file (from base folder) or full path if the file is not belong to <paramref name="baseFolder"/></returns>
        private static string MakePathRelative(string baseFolder, string path)
        {
            if (path == null) return null;
            if (baseFolder == null) return path;

            return (path.StartsWith(baseFolder))
                    ? path.Remove(0, baseFolder.Length)
                    : path;
        }

        /// <summary>
        /// Add data from stream to open archive.
        /// </summary>
        /// <param name="zipOutputStream">Specifies output stream to archive data.</param>
        /// <param name="zipEntry">Describe the data that will be added to archive.</param>
        /// <param name="dataStream">Specifies the data that will be added to archive</param>
        private void AddDataToZip(ZipOutputStream zipOutputStream, ZipEntry zipEntry, Stream dataStream)
        {
            zipOutputStream.PutNextEntry(zipEntry);
            byte[] buffer = new byte[65535];
            crc.Reset();
            int readCount;
            while ((readCount = dataStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                crc.Update(buffer, 0, readCount);
                zipOutputStream.Write(buffer, 0, readCount);
            }

            zipEntry.Crc = crc.Value;
        }

        /// <summary>
        /// Patches known problems in file path that incorrectly processed by SharpZipLib.
        /// </summary>
        /// <param name="fileName">The name of the file</param>
        /// <returns>Patched file name</returns>
        private static string PatchKnownProblems(string fileName)
        {
            if (fileName.StartsWith(".")) fileName = fileName.Remove(0, 1);
            if (fileName.StartsWith("\\")) fileName = fileName.Remove(0, 1);
            return fileName;
        }

        /// <summary>
        /// Returns the list of files that must be added to archive.
        /// </summary>
        /// <param name="sourceFolder">Name of the folder from which to add files.</param>
        /// <param name="fileMask">Name of the file to add to the zip file. Can include wildcards.</param>
        /// <param name="processSubDirsRecursively">Specifies if the files in the sub-folders of <paramref name="sourceFolder"/> should also be added.</param>
        /// <returns>The list of file names that must be added to archive.</returns>
        private static string[] GetListOfFilesToZip(
            string sourceFolder,
            string fileMask,
            bool processSubDirsRecursively)
        {
            return Directory.GetFiles(
                sourceFolder,
                fileMask,
                processSubDirsRecursively
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly);
        }
        #endregion

        #region Extract files from a zip file, preserving the structure
        /// <summary>
        /// Extract files from a zip file, preserving the structure
        /// </summary>
        /// <param name="zipFilename">Name of the zip file</param>
        /// <param name="destFolder">Name of the folder to extract to</param>
        /// <param name="password">Password for the zip file.  If blank there is no password.</param>
        /// <param name="overwrite">Specify the action for existing files in the <paramref name="destFolder"/> during extract process.</param>
        private static void _ExtractFilesFromZip(string zipFilename, string destFolder, string password, bool overwrite)
        {
            if (!destFolder.EndsWith("\\")) destFolder = destFolder + '\\';

            ZipInputStream zipStream = new ZipInputStream(
                new FileStream(zipFilename, FileMode.Open));
            try
            {
                zipStream.Password = password;

                ZipEntry zipEntry;
                byte[] buff = new byte[65536];
                while ((zipEntry = zipStream.GetNextEntry()) != null)
                {
                    if (zipEntry.IsDirectory)
                    {
                        if (!Directory.Exists(destFolder + zipEntry.Name.TrimStart('/')))
                            Directory.CreateDirectory(destFolder + zipEntry.Name.TrimStart('/'));
                        while (zipStream.Read(buff, 0, buff.Length) > 0) { }
                    }
                    else
                    {
                        if (!File.Exists(destFolder + zipEntry.Name.TrimStart('/')) || overwrite)
                        {
                            string dirName = Path.GetDirectoryName(destFolder + zipEntry.Name);
                            if (!Directory.Exists(dirName))
                            {
                                Directory.CreateDirectory(dirName);
                            }
                            SaveFile(zipStream, destFolder + zipEntry.Name, zipEntry.Size);
                        }
                    }
                }
            }
            finally
            {
                zipStream.Close();
            }
        }

        /// <summary>
        /// Saves file from stream to disk.
        /// </summary>
        /// <param name="stream">Specify the stream to save on disk</param>
        /// <param name="fullName">Specify the filename for correspondent <paramref name="stream"/>.</param>
        /// <param name="uncompressedSize">The size of the stream.</param>
        private static void SaveFile(Stream stream, string fullName, long uncompressedSize)
        {
            if (!stream.CanRead || stream.Length <= 0) return;
            FileStream fs = null;
            try
            {
                byte[] buff = new byte[65536];
                fs = new FileStream(fullName, FileMode.Create);
                if (uncompressedSize > 0)
                {
                    int res;
                    while ((res = stream.Read(buff, 0, buff.Length)) > 0)
                        fs.Write(buff, 0, res);
                }
            }
            finally
            {
                if (fs != null) fs.Close();
            }
        }
        #endregion



        public static void ZipFoldersAndFiles(ArrayList foldersAndFiles, string destFolder, string destFileName, string password, bool autoCheckSubFolder)
        {


            ArrayList _arrListFiles = new ArrayList();

            if (autoCheckSubFolder)
            {

                foreach (string _f in foldersAndFiles)
                {
                    if (Directory.Exists(_f))
                    {
                        ArrayList _arr = GenerateFileList(_f);

                        // _arrListFiles.AddRange(_arr);
                        foreach (string _s in _arr)
                        {
                            _arrListFiles.Add(_s);

                        }
                    }
                    else
                    {
                        _arrListFiles.Add(_f);
                    }
                }
            }
            else
            {
                _arrListFiles = foldersAndFiles;
            }


            FileStream ostream;
            byte[] obuffer;
            string _fullPath = destFolder;
            Regex _regex = new Regex(@_fullPath.Replace("\\", "\\\\"), RegexOptions.IgnoreCase);

            string outPath = _fullPath + destFileName;

            ZipOutputStream oZipStream = new ZipOutputStream(File.Create(outPath)); // create zip stream
            if (!string.IsNullOrEmpty(password))
                oZipStream.Password = password;
            oZipStream.SetLevel(9); // maximum compression
            foreach (string _fileName in _arrListFiles) // for each file, generate a zipentry
            {

                string _entryKey = _regex.Replace(_fileName, string.Empty);
                ZipEntry oZipEntry = new ZipEntry(_entryKey);
                //ZipEntry _z=new ZipEntry();
                oZipStream.PutNextEntry(oZipEntry);

                if (!_fileName.EndsWith(@"/")) // if a file ends with '/' its a directory
                {
                    ostream = File.OpenRead(_fileName);
                    obuffer = new byte[ostream.Length];
                    ostream.Read(obuffer, 0, obuffer.Length);
                    oZipStream.Write(obuffer, 0, obuffer.Length);
                }
            }
            oZipStream.Finish();
            oZipStream.Close();
        }


        private static ArrayList GenerateFileList(string Dir)
        {
            ArrayList fils = new ArrayList();
            bool Empty = true;
            foreach (string file in Directory.GetFiles(Dir)) // add each file in directory
            {
                fils.Add(file);
                Empty = false;
            }

            if (Empty)
            {
                if (Directory.GetDirectories(Dir).Length == 0)
                // if directory is completely empty, add it
                {
                    fils.Add(Dir + @"/");
                }
            }

            foreach (string dirs in Directory.GetDirectories(Dir)) // recursive
            {
                foreach (object obj in GenerateFileList(dirs))
                {
                    fils.Add(obj);
                }
            }
            return fils; // return file list
        }
    }
}