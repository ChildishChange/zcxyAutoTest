using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System;

namespace AutoTest
{
    public class Tester
    {

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
            try
            {
                Stopwatch timeWatch = new Stopwatch();
                timeWatch.Start();
                using (Process cmd = Process.Start(binaryInfo))
                {
                    //TODO 添加超时kill process
                    cmd.StandardInput.WriteLine(strInput + "&exit");
                    cmd.StandardInput.AutoFlush = true;

                    //获取输出信息
                    string strOut = cmd.StandardOutput.ReadToEnd();
                    string strErr = cmd.StandardError.ReadToEnd();


                    //Start monitor
                    cmd.WaitForExit(20 * 1000);
                    timeWatch.Stop();
                    //Release all resources
                    if (!cmd.HasExited)
                    {
                        //Give system sometime to release resource
                        cmd.Kill();
                        Thread.Sleep(1000);
                    }

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
            catch(Exception e)
            {
                Logger.Error($"Error happened when calling cmd.");
                return false;
            }
            
        }

        public static string CheckJavaFile(string javaFilePath)
        {
            //返回编译应该使用的字符串


            //如果文件内含有“import java.util.Scanner;”就返回null
            return null;
        }

        public static void TestCorrectness(string javaFilePath, List<string> correctTests)
        {
            Logger.Info("Start correctness tests.");

            FileInfo javaFile = new FileInfo(javaFilePath);

            foreach (var test in correctTests)
            {
                Logger.Info($"Start test \"{test}\"");

                var outFile = new FileInfo(Path.Combine(javaFile.DirectoryName,"out.txt"));

                //测试前若存在out.txt则删除
                //if (outFile.Exists)
                if(File.Exists(@".\out.txt"))
                {
                    //File.Delete(outFile.FullName);
                    File.Delete(@".\out.txt");
                }

                //调callcmd
                CallCmd("java -classpath " + javaFile.DirectoryName + " " + javaFile.Name.Replace(".java", "") + " " + test);

                Thread.Sleep(3000);

                //测试后不存在out.txt则报错
                //if (!File.Exists(outFile.FullName))
                if(!File.Exists(@".\out.txt"))
                {
                    Logger.Error("File \"out.txt\" not found!");
                    continue;
                }

                //一项一项确认
                Tester.CheckOutFile(@".\out.txt", test);
            }
        }

        public static void TestRobustness(string javaFilePath, List<string> robustTests)
        {
            Logger.Info("Start robustness test.");

            FileInfo javaFile = new FileInfo(javaFilePath);

            foreach (var test in robustTests)
            {
                //程序会主动输出info或者error，所以不需要额外的检测，只需要将所有程序运行即可
                
                CallCmd("java -classpath " + javaFile.DirectoryName + " " + javaFile.Name.Replace(".java", "") + " " + test);
            }
        }

        public static void CheckOutFile(string outFile, string testStr)
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

            FileStream fileStream = new FileStream(outFile, FileMode.Open, FileAccess.Read);
            StreamReader streamReader = new StreamReader(fileStream, Encoding.Default);
            fileStream.Seek(0, SeekOrigin.Begin);

            //如果parameter 为 0 ，需要额外处理
            if (numOfExercise == 0)
            {
                return;
            }

            //检查题目
            Dictionary<string, string> exerciseDic = new Dictionary<string, string>();
            List<string> exerciseList = new List<string>();

            var i = 1;
            for (; i <= numOfExercise; i++)
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
                var matches = Regex.Matches(line, finalPattern);

                //没有识别出，或识别出大于一个，都属于题目格式错误
                if (matches.Count != 1)
                {
                    Logger.Error($"Wrong format in line {i}:\n{line}");
                    continue;
                }

                //识别出了
                var index = Regex.Match(line, "\\(\\d{1,}\\)").Value;
                var indexOfExercise = int.Parse(index.Trim('(', ')'));

                
                if (exerciseDic.ContainsValue(ExerciseHandler.Swap(line.Replace(index + " ", "").Replace(" ", ""))))
                {
                    Logger.Warning($"Duplicated:\n{line}");
                }
                exerciseDic.Add(line, ExerciseHandler.Swap(line.Replace(index + " ", "").Replace(" ", "")));
                
                //判断题号
                if (i != indexOfExercise)
                {
                    Logger.Error($"Wrong exercise index in line {i}:\n{line}\nIt supposed to be {i}");
                }
                exerciseList.Add(line);
            }

            //判断否是空行
            if (!string.IsNullOrWhiteSpace(streamReader.ReadLine()))
            {
                Logger.Error("Exercises and Answers are not divided by a space line.");
            }

            if(exerciseList.Count != numOfExercise)
            {
                Logger.Info("Because of the errors above, test end.");
                fileStream.Close();
                streamReader.Close();
                return;
            }

            for (i = 1; i < numOfExercise; i++)
            {
                var line = streamReader.ReadLine();

                if (line == null)
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
                if (matches.Count != 1)
                {
                    Logger.Error($"Wrong format in answer {i}:\n{line}");
                }

                //计算算式的答案
                if (!ExerciseHandler.Calculate(exerciseDic[exerciseList[i - 1]], line.Replace(exerciseList[i - 1] + " = ", "")))
                {
                    Logger.Error($"Wrong answer:\n{line}");
                }
            }
            
            fileStream.Close();
            streamReader.Close();


        }
    }
}
