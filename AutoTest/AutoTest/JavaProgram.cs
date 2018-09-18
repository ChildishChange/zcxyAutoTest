using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoTest
{
    public class JavaProgram
    {
        public string packageName { set; get; }
        public string compileCmd { set; get; }
        public string runTestCmd { set; get; }
        public string javaFilePath { set; get; }
        public bool canRunTest { set; get; }

        /// <summary>
        /// 根据传入的PSP目录实例化JAVAProgram对象
        /// 如果 路径/命名 不符合要求 或 代码使用stdin 则不编译
        /// </summary>
        /// <param name="PSPDirPath"></param>
        public JavaProgram(DirectoryInfo pspDir)
        {
            CheckPSP(pspDir);
            if (!canRunTest) { return; }

            CheckScannerAndPackage();
            if (!canRunTest) { return; }

            if (GetEncoding() == Encoding.UTF8)
            {
                this.compileCmd = $"javac -encoding UTF-8 {javaFilePath}";
            }
            else
            {
                this.compileCmd = $"javac {javaFilePath}";
            }

            this.canRunTest = Tester.CallCmd(this.compileCmd);
            if (!canRunTest)
            {
                Logger.Error($"Error happened when compiling {pspDir}");
                return;
            }

            //moveClassToPath
            if(this.packageName!=null)
            {
                Directory.CreateDirectory(Path.Combine(pspDir.FullName, this.packageName.Replace('.', '\\')));
                this.runTestCmd = $"java -classpath {pspDir.FullName} {this.packageName}.{new FileInfo(this.javaFilePath).Name.Replace(".class", "")} ";
            }
            else
            {
                this.runTestCmd = $"java -classpath {pspDir.FullName} {new FileInfo(this.javaFilePath).Name.Replace(".class", "")} ";
            }
        }

        /// <summary>
        /// 获取java文件编码
        /// code from https://www.cnblogs.com/guyun/p/4262587.html
        /// </summary>
        /// <returns></returns>
        public Encoding GetEncoding()
        {
            //byte[] Unicode = new byte[] { 0xFF, 0xFE, 0x41 };
            //byte[] UnicodeBIG = new byte[] { 0xFE, 0xFF, 0x00 };
            //byte[] UTF8 = new byte[] { 0xEF, 0xBB, 0xBF }; //带BOM 
            Encoding reVal = Encoding.Default;
            FileStream fs = new FileStream(this.javaFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader r = new BinaryReader(fs, Encoding.Default);
            int i;
            int.TryParse(fs.Length.ToString(), out i);
            byte[] ss = r.ReadBytes(i);
            if (ss.Length > 3 && ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF)
            {
                reVal = Encoding.UTF8;
            }
            else if (ss.Length > 3 && ss[0] == 0xFE && ss[1] == 0xFF && ss[2] == 0x00)
            {
                reVal = Encoding.BigEndianUnicode;
            }
            else if (ss.Length > 3 && ss[0] == 0xFF && ss[1] == 0xFE && ss[2] == 0x41)
            {
                reVal = Encoding.Unicode;
            }
            r.Close();
            fs.Close();
            return reVal;
        }

        /// <summary>
        /// 检查目录名与文件名是否符合PSP要求
        /// </summary>
        /// <param name="pspDir"></param>
        public void CheckPSP(DirectoryInfo pspDir)
        {
            var _dirNamePattern = @"^PSP\d{4}$";

            this.canRunTest = Regex.IsMatch(pspDir.Name, _dirNamePattern);

            if (!this.canRunTest)
            {
                Logger.Error($"Directory name doesn't meet the requirement:\n{pspDir.Name}");
                return ;
            }
            FileInfo _javaFile = new FileInfo(
                Path.Combine(
                    pspDir.FullName,
                    "MathExam" + pspDir.Name.Replace("PSP", "") + ".java")
                    );
            this.canRunTest = _javaFile.Exists;
            //this.PathMeetDemand = File.Exists(_javaFile.FullName);

            if (!this.canRunTest)
            {
                Logger.Error($"File {_javaFile.Name} doesn't exist");
                return ;
            }

            this.javaFilePath = _javaFile.FullName;      
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
            
            this.packageName = match.Groups[0].Value.Trim();
            this.packageName = match.Value.Replace("package","").Replace(";","").Trim();
            
            //如果代码中含有scanner，则设置
            this.canRunTest = _fileContent.Contains("import java.util.Scanner;");
            
        }
    }
}
