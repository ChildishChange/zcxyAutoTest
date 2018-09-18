using System;
using System.IO;
using System.Net;

namespace AutoTest
{
    public class PathHandler
    {
        public static string GetAbsPath(string path)
        {
            if (path == null) return null;
            try
            {
                var absPath = !Path.IsPathRooted(path) ? Path.Combine(Directory.GetCurrentDirectory(), path) : path;
                absPath = WebUtility.UrlDecode(absPath);
                var fileInfo = new FileInfo(absPath);

                if ((fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    var directoryInfo = new DirectoryInfo(absPath);
                    if (directoryInfo.Exists) return directoryInfo.FullName;
                    Logger.Error($"Directory not exists! Please check the directory path: {directoryInfo.FullName}");
                }
                else
                {
                    if (fileInfo.Exists) return fileInfo.FullName;
                    Logger.Error($"File not exists! Please check the file path: {fileInfo.FullName}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message} Please check the path: {path}");
            }
            return null;
        }

    }
}
