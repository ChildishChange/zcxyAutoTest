using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace AutoTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<TestOptions>(args)
                .MapResult((TestOptions opts) => RunTestOptions(opts),
                            errs => 1);
        }

        public static int RunTestOptions(TestOptions opts)
        {
            opts.ConfigPath = GetAbsPath(opts.ConfigPath);

            if(!string.IsNullOrEmpty(opts.ConfigPath))
            {
                //读取json
                
                //判断是否有目标文件夹
                //有
                    //略
                //没有
                    //下载
            }
            else
            {
                Console.WriteLine("[ERROR]Program exit because of error above.");
                return 1;
            }

            

            return 0;
        }

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
                    Console.WriteLine($"[ERROR]Not exists! Please check the directory path: {directoryInfo.FullName}");
                }
                else
                {
                    if (fileInfo.Exists) return fileInfo.FullName;
                    Console.WriteLine($"[ERROR]Not exists! Please check the file path: {fileInfo.FullName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR]{ex.Message} Please check the path: {path}");
            }
            return null;
        }

        public bool DownloadCode(string Url)
        {
            string codePath = Directory.GetCurrentDirectory() + @"\temp";
            Directory.CreateDirectory(codePath);
            
            
            /*
             需要添加一个解析json，读取下载信息的函数，来替换此处的test目录 
             */
            string codeFile = codePath + @"\" + "test";

            if (File.Exists(codeFile))
            {
                Console.WriteLine("[INFO]Already downloaded!");
                return true;
            }
            
            try
            {
                HttpWebRequest request = WebRequest.Create(Url) as HttpWebRequest;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                
                Stream responseStream = response.GetResponseStream();

                FileStream fs = new FileStream(codeFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

                byte[] bArr = new byte[1024];
                int size = responseStream.Read(bArr, 0, (int)bArr.Length);                
                while (size > 0)
                {
                    fs.Write(bArr, 0, size);
                    size = responseStream.Read(bArr, 0, (int)bArr.Length);
                }

                fs.Close();
                responseStream.Close();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }

            // https://github.com/ChildishChange/BlogPublishTool/archive/master.zip
            return true;
        }
    }
}
