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
                
                //获取测试指令
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
                    if(CallCmd("javac " + JavaFile.FullName))
                    {
                        Logger.Info($"Start test No.{StudentID} program.");
                        TestCorrectness(JavaFile.FullName, correctTests);
                        TestRobustness(JavaFile.FullName, robustTests);
                        Logger.Info($"End test No.{StudentID} program.");
                    }
                    else
                    {
                        //编译失败
                        Logger.Error($"Error happened when compiling No.{StudentID}");
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
                //TODO 添加超时kill process
                cmd.StandardInput.WriteLine(strInput + "&exit");
                cmd.StandardInput.AutoFlush = true;

                //获取输出信息
                string strOut = cmd.StandardOutput.ReadToEnd();
                string strErr = cmd.StandardError.ReadToEnd();
                
                cmd.Close();
                if (!string.IsNullOrEmpty(strOut))
                {
                    Logger.Info($"Program output follows when running \"{strInput}\":\n{strOut}");
                }

                if (!string.IsNullOrEmpty(strErr))
                {
                    Logger.Error($"Program Error when running \"{strInput}\" are as follows:\n{strErr}");
                    return false;
                }
                return true;
            }
        }
       

        public static void TestCorrectness(string javaFilePath, List<string> correctTests)
        {
            Logger.Info("Start correctness tests.");
            foreach (var test in correctTests)
            {
                Logger.Info($"Start test \"{test}\"");

                var outFile = new FileInfo(
                                  Path.Combine(
                                       new FileInfo(javaFilePath).DirectoryName,
                                       "out.txt"));
                
                //测试前若存在out.txt则删除
                if(outFile.Exists)
                {
                    File.Delete(outFile.FullName);
                }

                //调callcmd
                CallCmd("java " + javaFilePath + " " + test);

                //测试后不存在out.txt则报错
                if(!outFile.Exists)
                {
                    Logger.Error("File \"out.txt\" not found!");
                    continue;
                }

                //一项一项确认
                CheckOutFile(outFile, test);
            }
        }

        public static void TestRobustness(string javaFilePath, List<string> robustTests)
        {
            Logger.Info("Start robustness test.");
            foreach (var test in robustTests)
            {
                //程序会主动输出info或者error，所以不需要额外的检测，只需要将所有程序运行即可
                CallCmd("java " + javaFilePath + " " + test);
            }
        }


        public static void CheckOutFile(FileInfo outFile, string testStr)
        {
            /*
            - 题目数量是否正确 
            - 答案与题目是否一一对应 -> 字符串能够匹配到等号
            - 答案是否正确 -> 需要解析题目
            - 题目与答案所使用的数字范围是否合理 -> 用正则表达式筛选所有等式，是否能筛出题目中大于三位且开头不为零的数字
            - 题目是否重复
                - a + b 等价于 b + a
                - a × b 等价于 b × a
            - 输出格式是否正确 -> 用两种正则，有余数和没有余数
                - 题号 -> 读取前n行，/直接判断子串位置
                - 题目和答案空行 -> 读取文件的前N行，判断第N+1行是否是空格 是空格 则题目数量正确 不是，判断有没有等号，没有则题目数量错误
            */

            /*
            
            从testStr中读取出参数，参数二默认为1
            设置两个字符串，根据字符串选择题目测试的正则

            for i = 1;i<=参数;i++ 
               一次读一行
               判断题目正则 -> 不符合 ->logger
               判断重复
            
            判断第N+1行是否是空格 是空格 则题目数量正确 不是，判断有没有等号，没有则题目数量错误

            for 继续
                如果到文件末尾 -> 数量有问题
                一一对应是否匹配
                答案是否有问题
            */
        }
    }
}
