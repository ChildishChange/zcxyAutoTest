using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoTest
{
    public class JavaProgram
    {
        public string packageName { set; get; }
        //public string compileCmd { set; get; }
        public string runTestCmd { set; get; }
        public string javaFilePath { set; get; }
        public bool canRunTest { set; get; }
        public string dirName { set; get; }

        /// <summary>
        /// 根据传入的PSP目录实例化JAVAProgram对象
        /// 如果 路径/命名 不符合要求 或 代码使用stdin 则不编译
        /// </summary>
        /// <param name="PSPDirPath"></param>
        public JavaProgram(DirectoryInfo dir)
        {
            Logger.Info($"Start initializing {dir}");
            this.dirName = dir.Name;
            
            CheckName(dir);
            if (!canRunTest) { return; }


            CheckCompile();
            if (!canRunTest)
            {
                Logger.Error($"Error happened when compiling {dir}");
                return;
            }

            CheckScannerAndPackage();
            if (!canRunTest) { return; }

            SetupTestEnv(dir);

            
        }

        private void SetupTestEnv(DirectoryInfo dir)
        {
            if (!string.IsNullOrEmpty(this.packageName))
            {
                if (Directory.Exists(Path.Combine(dir.FullName, this.packageName.Replace('.', '\\'))))
                    Directory.Delete(Path.Combine(dir.FullName, this.packageName.Replace('.', '\\')), true);
                Directory.CreateDirectory(Path.Combine(dir.FullName, this.packageName.Replace('.', '\\')));
                //移动class文件
                File.Move(javaFilePath.Replace(".java", ".class"),
                          javaFilePath.Replace(dir.FullName,
                                               Path.Combine(dir.FullName, this.packageName.Replace('.', '\\'))).Replace(".java", ".class"));

                this.runTestCmd = $"java -classpath {dir.FullName} {this.packageName}.{new FileInfo(this.javaFilePath).Name.Replace(".java", "")} ";
            }
            else
            {
                this.runTestCmd = $"java -classpath {dir.FullName} {new FileInfo(this.javaFilePath).Name.Replace(".java", "")} ";
            }
        }



        /// <summary>
        /// 检查目录名与文件名是否符合PSP要求
        /// </summary>
        /// <param name="dir"></param>
        public void CheckName(DirectoryInfo dir)
        {
            //var _dirNamePattern = @"^PSP\d{4}$";
            var _dirNamePattern = @"^Pair_\d{9}_\d{9}$";

            this.canRunTest = Regex.IsMatch(dir.Name, _dirNamePattern);

            if (!this.canRunTest)
            {
                Logger.Error($"Directory name doesn't meet the requirement:{dir.Name}");
                return ;
            }
            FileInfo _javaFile = new FileInfo(
                Path.Combine(
                    dir.FullName,
                    //"MathExam" + dir.Name.Replace("PSP", "") + ".java")
                    "MathExam.java")
                    );
            this.canRunTest = _javaFile.Exists;

            if (!this.canRunTest)
            {
                Logger.Error($"File {_javaFile.Name} doesn't exist");
                return ;
            }

            this.javaFilePath = _javaFile.FullName;      
        }



        public void CheckCompile()
        {
            //this.canRunTest = Tester.CallCmd($"javac {javaFilePath.Replace("MathExam"+dirName.Remove(0, 3),"*")}") ||
            //                  Tester.CallCmd($"javac -encoding UTF-8 {javaFilePath.Replace("MathExam" + dirName.Remove(0, 3), "*")}");
            this.canRunTest = Tester.CallCmd($"javac {javaFilePath.Replace("MathExam", "*")}") ||
                              Tester.CallCmd($"javac -encoding UTF-8 {javaFilePath.Replace("MathExam", "*")}");
        }




        /// <summary>
        /// 检查java文件中含有对scanner的引用，如果有则不自动测试本份java代码
        /// 检查内容中是否含有package
        /// </summary>
        public void CheckScannerAndPackage()
        {
            string _fileContent = File.ReadAllText(this.javaFilePath);
            
            //如果代码中含有package信息，则保存package信息
            var match = Regex.Match(_fileContent, @"(?:^|\n)(\s*)package(\s+)(.*)(\s*);");
            this.packageName = match.Value.Replace("package","").Replace(";","").Trim();
            
            //如果代码中含有scanner，则
            this.canRunTest = !_fileContent.Contains("import java.util.Scanner;");
            if (!this.canRunTest)
            {
                Logger.Error($"{dirName} used Scanner.");
                return;
            }
        }
    }
}
