using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using LibGit2Sharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AutoTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Info("**** Auto test Program started. ****");
            Parser.Default.ParseArguments<TestOptions>(args)
                .MapResult((TestOptions opts) => RunTestOptions(opts),
                            errs => 1);
            Logger.Info("***** Auto test Program ended. *****");
        }

        public static int RunTestOptions(TestOptions opts)
        {
            opts.ConfigPath = GetAbsPath(opts.ConfigPath);

            if(!string.IsNullOrEmpty(opts.ConfigPath))
            {
                var configJson = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(opts.ConfigPath));
                //获取仓库
                string url = (string)configJson["repo"];
                string clonePath = Path.Combine(Directory.GetCurrentDirectory(), "temp");
                try
                {
                    CloneRepo(url, clonePath);
                }
                catch (Exception e)
                {
                    Logger.Error("Clone repository failed.");
                    return 1;
                }
                var DirectoryList = new DirectoryInfo(clonePath).GetDirectories().ToList();
                //TODO:获取测试指令
                var correctTests = configJson["correct"].Select(x => x.ToString()).ToList();
                var robustTests = configJson["robust"].Select(x => x.ToString()).ToList();


                //开始测试
                foreach (var Dir in DirectoryList)
                {
                    //获取学生学号
                    string StudentID = Dir.Name.Replace("PSP","");
                    //获取文件夹内指定java文件
                    FileInfo JavaFile = new FileInfo(Path.Combine(Dir.FullName, "MathExam", StudentID, ".java"));
                    //编译目标代码
                    if(CallCmd("javac " + JavaFile.Name))
                    {
                        TestCorrectness(JavaFile.Name, correctTests);
                        TestRobustness(JavaFile.Name, robustTests);
                    }
                    else
                    {
                        //编译失败
                        Logger.Error($"Error happened when compiling {JavaFile.Name}.");
                        continue;
                    }
                }
            }
            else
            {
                Logger.Info("Program exit because of error above.");
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

        public static void CloneRepo(string url, string path)
        {
            if (Directory.Exists(path))
            {
                Logger.Warning("Already cloned this repository before.");
                return;
            }
            Repository.Clone(url, path);
            Logger.Info("Cloned this repository successfully.");
            
            Thread.Sleep(1000);
        }

        public static bool CallCmd(string strInput)
        {
            var binaryInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            };
            
            using (Process cmd = Process.Start(binaryInfo))
            {
                cmd.StandardInput.WriteLine(strInput + "&exit");
                cmd.StandardInput.AutoFlush = true;

                //获取输出信息
                string strOut = cmd.StandardOutput.ReadToEnd();
                string strErr = cmd.StandardError.ReadToEnd();
                
                cmd.Close();
                if (!string.IsNullOrEmpty(strOut))
                {
                    Logger.Info($"Program output follows:\n{strOut}");
                }

                if (!string.IsNullOrEmpty(strErr))
                {
                    Logger.Error($"Program Error as follows:\n{strErr}");
                    return false;
                }
                return true;
            }
        }
       

        public static void TestCorrectness(string javaName, List<string> correctTests)
        {
            //TODO:正确性测试
            foreach (var test in correctTests)
            {

            }
        }
        public static void TestRobustness(string javaName, List<string> robustTests)
        {
            //TODO:鲁棒性测试
            foreach (var test in robustTests)
            {

            }
        }
    }
}
