using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
                    if (Dir.Name == ".git")
                        continue;
                    //获取学生学号
                    string StudentID = Dir.Name.Replace("PSP","");
                    //获取文件夹内指定java文件
                    FileInfo JavaFile = new FileInfo(Path.Combine(Dir.FullName, "MathExam" + StudentID + ".java"));
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

            FileInfo javaFile = new FileInfo(javaFilePath);

            foreach (var test in correctTests)
            {
                Logger.Info($"Start test \"{test}\"");

                var outFile = new FileInfo(
                                  Path.Combine(
                                       javaFile.DirectoryName,
                                       "out.txt"));
                
                //测试前若存在out.txt则删除
                if(outFile.Exists)
                {
                    File.Delete(outFile.FullName);
                }

                //调callcmd
                CallCmd("java -classpath " + javaFile.DirectoryName + " " + javaFile.Name.Replace(".java","") + " " + test);

                //测试后不存在out.txt则报错
                if(!File.Exists(outFile.FullName))
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
            var parameters = testStr.Split(' ');
            var numOfExercise = int.Parse(parameters.First());
            var exercises = new List<string>();
            
            const string addMinusPattern = @"^\(\d{1,}\)\s\d{1,2}\s[+-]\s\d{1,2}$";
            const string divideMultiPattern = @"^\(\d{1,}\)\s\d{1,2}\s[×÷]\s\d{1,2}$";
            const string addMinusEqPattern = @"^\(\d{1,}\)\s\d{1,2}\s[+-]\s\d{1,2}\s=\s\d{1,2}$";
            const string divideMultiEqPattern = @"^\(\d{1,}\)\s\d{1,2}\s[×]\s\d{1,2}\s=\s\d{1,2}$|^\(\d{1,}\)\s\d{1,2}\s[÷]\s\d{1,2}\s=\s\d{1,2}(\.{3}\d{1,2})?$";

            var grade = (parameters.Count() > 1) ? int.Parse(parameters.Last()) : 1;

            var finalPattern = (grade == 1) ? addMinusPattern : divideMultiPattern;
            var finalEqPattern = (grade == 1) ? addMinusEqPattern : divideMultiEqPattern;

            FileStream fileStream = new FileStream(outFile.FullName, FileMode.Open, FileAccess.Read);
            StreamReader streamReader = new StreamReader(fileStream, Encoding.Default);
            fileStream.Seek(0, SeekOrigin.Begin);

            //如果parameter 为 0 ，需要额外处理
            if(numOfExercise == 0)
            {
                return;
            }

            //检查题目
            Dictionary<string,string> exerciseDic = new Dictionary<string, string>();
            List<string> exerciseList = new List<string>();

            var i = 1;
            for(; i <= numOfExercise; i++)
            {
                var line = streamReader.ReadLine();
                if (line == null)
                {
                    Logger.Error("Number of exercise is not enough!");
                    fileStream.Close();
                    streamReader.Close();

                    return;
                }
                //判断题目是否符合格式
                var matches = Regex.Matches(line,finalPattern);
                
                //没有识别出，或识别出大于一个，都属于题目格式错误
                if(matches.Count!=1)
                {
                    Logger.Error($"Wrong format in line {i}:\n{line}");
                }
                
                //识别出了
                var index = Regex.Match(line, "\\(\\d{1,}\\)").Value;
                var indexOfExercise = int.Parse(index.Trim('(', ')'));
                
                //不重复则加入set
                if (exerciseDic.ContainsValue(Swap(line.Replace(index + " ", "").Replace(" ",""))))
                {
                    Logger.Warning($"Duplicated:\n{line}");
                }
                else
                {
                    exerciseDic.Add(line,Swap(line.Replace(index + " ", "").Replace(" ", "")));
                }
                //判断题号
                if (i != indexOfExercise)
                {
                    Logger.Error($"Wrong exercise index in line {i}:\n{line}\nIt supposed to be {i}");
                }
                exerciseList.Add(line);
            }

            //判断否是空行
            if(!string.IsNullOrWhiteSpace(streamReader.ReadLine()))
            {
                Logger.Error("Exercises and Answers are not divided by a space line.");
            }

            for(i = 1; i < numOfExercise; i ++)
            {
                var line = streamReader.ReadLine();

                if(line == null)
                {
                    Logger.Error($"Number of answer is not enough!");
                    fileStream.Close();
                    streamReader.Close();

                    return;
                }

                if (!line.StartsWith(exerciseList[i - 1]))
                {
                    Logger.Error($"Answer doesn't match exercise:\n{line}");
                    continue;
                }

                //匹配剩下的部分是否符合要求
                var matches = Regex.Matches(line, finalEqPattern);
                if(matches.Count!=1)
                {
                    Logger.Error($"Wrong format in answer {i}:\n{line}");
                }

                //计算算式的答案
                if(!Calculate(exerciseDic[exerciseList[i-1]],line.Replace(exerciseList[i-1]+" = ","")))
                {
                    Logger.Error($"Wrong answer:\n{line}");
                }
            }


            fileStream.Close();
            streamReader.Close();


        }

        public static bool Calculate(string exercise, string answer)
        {
            if(exercise.Contains('+'))
            {
                var ops = exercise.Split('+');
                return int.Parse(answer) == int.Parse(ops[0]) + int.Parse(ops[1]);
            }
            if(exercise.Contains('-'))
            {
                var ops = exercise.Split('-');
                return int.Parse(answer) == int.Parse(ops[0]) - int.Parse(ops[1]);
            }
            if(exercise.Contains('×'))
            {
                var ops = exercise.Split('×');
                return int.Parse(answer) == int.Parse(ops[0]) * int.Parse(ops[1]);
            }
            if(exercise.Contains('÷'))
            {
                var ops = exercise.Split('÷');
                if(int.Parse(ops[0])%int.Parse(ops[1]) == 0 )
                {
                    if (answer.Contains('.')) return false;
                    return int.Parse(ops[0]) / int.Parse(ops[1]) == int.Parse(answer);
                }
                else
                {
                    if (!answer.Contains('.')) return false;

                    var ans = answer.Replace("..."," ").Split(' ');

                    return int.Parse(ops[1]) * int.Parse(ans[0]) + int.Parse(ans[1]) == int.Parse(ops[0]);

                }
            }
            
            return false;
        }
        public static string Swap(string line)
        {
            StringBuilder sb = new StringBuilder();
            if(line.Contains('+'))
            {
                var ops = line.Split('+');
                sb.Append((int.Parse(ops[0]) > int.Parse(ops[1])) ? ops[1] + "+" + ops[0] : line);
                return sb.ToString();
            }
            if(line.Contains('×'))
            {
                var ops = line.Split('×');
                sb.Append((int.Parse(ops[0]) > int.Parse(ops[1])) ? ops[1] + "×" + ops[0] : line);
                return sb.ToString();
            }
            return line;
        }
    }
}
