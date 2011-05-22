using System;

namespace Nasa8x.Core.FileManager
{
    public class FileItem
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public bool IsFolder { get; set; }
        public bool IsExistsIcon { get; set; }
        public object Size { get; set; }
        public DateTime LastAccessDate { get; set; }
        public DateTime LastWriteDate { get; set; }
        public DateTime Created { get; set; }
        public string Attributes { get; set; }
        public string Extension { get; set; }
        public long FileCount { get; set; }
        public long SubFolderCount { get; set; }
    }
}
