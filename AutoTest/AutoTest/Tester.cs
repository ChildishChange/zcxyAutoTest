﻿using System.Collections.Generic;
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
                    cmd.StandardInput.WriteLine(strInput);
                    cmd.StandardInput.WriteLine("exit");
                    cmd.StandardInput.AutoFlush = true;

                    //获取输出信息
                    
                    cmd.BeginOutputReadLine();
                    string strErr = cmd.StandardError.ReadToEnd();
                    string strOut = null;
                    cmd.OutputDataReceived += (s, e) => strOut = e.Data;

                    cmd.WaitForExit();
                    
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
                        //Logger.Error($"Program Error when running \"{strInput}\" are as follows:\n{strErr}");
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

        public static void TestCorrectness(JavaProgram javaProgram, List<string> correctTests)
        {
            Logger.Info("Start correctness tests.");
            foreach (var test in correctTests)
            {
                Logger.Info($"Start test \"{test}\"");
                if(File.Exists(@".\out.txt")){ File.Delete(@".\out.txt"); }

                CallCmd(javaProgram.runTestCmd + test);
                Thread.Sleep(2000);

                if (!File.Exists(@".\out.txt"))
                {
                    Logger.Error("File \"out.txt\" not found!");
                    continue;
                }

                CheckOutFile(@".\out.txt", test);
            }
        }

        public static void TestRobustness(JavaProgram javaProgram, List<string> robustTests)
        {
            Logger.Info("Start robustness test.");
            foreach (var test in robustTests)
            {
                //程序会主动输出info或者error，所以不需要额外的检测，只需要将所有程序运行即可
                CallCmd(javaProgram.runTestCmd + test);
            }
        }

        public static void CheckOutFile(string outFile, string testStr)
        {
            var parameters = testStr.Split(' ');
            var numOfExercise = int.Parse(parameters.First());
            var exercises = new List<string>();

            const string addMinusPattern = @"^\(\d{1,}\)\d{1,2}[+-]\d{1,2}(=)?$";
            const string divideMultiPattern = @"^\(\d{1,}\)\d{1,2}[×÷*/]\d{1,2}(=)?$";

            const string addMinusEqPattern = @"^\(\d{1,}\)\d{1,2}[+-]\d{1,2}=\d{1,2}$";
            const string divideMultiEqPattern = @"^\(\d{1,}\)\d{1,2}[×*]\d{1,2}=\d{1,2}$|^\(\d{1,}\)\d{1,2}[÷/]\d{1,2}=\d{1,2}(\.{3}\d{1,2})?$";

            var grade = (parameters.Count() > 1) ? int.Parse(parameters.Last()) : 1;

            var finalPattern = (grade == 1) ? addMinusPattern : divideMultiPattern;
            var finalEqPattern = (grade == 1) ? addMinusEqPattern : divideMultiEqPattern;

            FileStream fileStream = new FileStream(outFile, FileMode.Open, FileAccess.Read);
            StreamReader streamReader = new StreamReader(fileStream, Encoding.Default);
            fileStream.Seek(0, SeekOrigin.Begin);

            if (numOfExercise == 0){ return; }

            Dictionary<string, string> exerciseDic = new Dictionary<string, string>();
            List<string> exerciseList = new List<string>();

            var i = 1;
            for (; i <= numOfExercise; i++)
            {
                var line = streamReader.ReadLine().Replace(" ","");
                if (line == null)
                {
                    Logger.Error("Number of exercise is not enough!");
                    fileStream.Close();
                    streamReader.Close();
                    return;
                }

                if (!Regex.IsMatch(line,finalPattern))
                {
                    Logger.Error($"Wrong format in line {i} : {line}");
                    fileStream.Close();
                    streamReader.Close();
                    return;
                }

                //这里需要替换÷为/ 替换×为*
                line = line.Replace('÷', '/');
                line = line.Replace('×', '*');


                //识别出了
                var index = Regex.Match(line, "\\(\\d{1,}\\)").Value;
                var indexOfExercise = int.Parse(index.Trim('(', ')'));
                
                if (exerciseDic.ContainsValue(ExerciseHandler.Swap(line.Replace(index , ""))))
                {
                    Logger.Warning($"Duplicated : {line}");
                }
                exerciseDic.Add(line, ExerciseHandler.Swap(line.Replace(index, "")));
                
                //判断题号
                if (i != indexOfExercise)
                {
                    Logger.Error($"Wrong exercise index in line {i} : {line}\nIt supposed to be {i}");
                    fileStream.Close();
                    streamReader.Close();
                    return;
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
            //检查答案
            for (i = 1; i < numOfExercise; i++)
            {
                var line = streamReader.ReadLine().Replace(" ", "");

                if (line == null)
                {
                    Logger.Error($"Number of answer is not enough!");
                    fileStream.Close();
                    streamReader.Close();
                    return;
                }

                if (!line.StartsWith(exerciseList[i - 1]))
                {
                    Logger.Error($"Answer doesn't match exercise : {line}");
                    fileStream.Close();
                    streamReader.Close();
                    return;
                }

                //匹配剩下的部分是否符合要求
                if (!Regex.IsMatch(line, finalEqPattern))
                {
                    Logger.Error($"Wrong format in answer {i} : {line}");
                    fileStream.Close();
                    streamReader.Close();
                    return;
                }


                //计算算式的答案
                if (!ExerciseHandler.Calculate(exerciseDic[exerciseList[i - 1]], line.Replace(exerciseList[i - 1] + " = ", "")))
                {
                    Logger.Error($"Wrong answer : {line}");
                    fileStream.Close();
                    streamReader.Close();
                    return;
                }
            }
            
            fileStream.Close();
            streamReader.Close();

        }
    }
}
