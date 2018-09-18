using System;
using System.IO;
using System.Linq;
using System.Threading;
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
            opts.ConfigPath = PathHandler.GetAbsPath(opts.ConfigPath);

            if(!string.IsNullOrEmpty(opts.ConfigPath))
            {
                var configJson = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(opts.ConfigPath));

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
                    if (Dir.Name == ".git")
                        continue;
                    string StudentID = Dir.Name.Replace("PSP","");
                    FileInfo JavaFile = new FileInfo(Path.Combine(Dir.FullName, "MathExam" + StudentID + ".java"));

                    if (Tester.CallCmd("javac " + JavaFile.FullName))
                    {
                        Logger.Info($"Start test No.{StudentID} program.");
                        Tester.TestCorrectness(JavaFile.FullName, correctTests);
                        Tester.TestRobustness(JavaFile.FullName, robustTests);
                        Logger.Info($"End test No.{StudentID} program.");
                    }
                    else
                    {
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


        
    }
}
