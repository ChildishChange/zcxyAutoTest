using System;
using System.IO;

namespace AutoTest
{
    //From https://github.com/SivilTaram/SudokuAutoTest/blob/86de8f44d063379445e59b22416c68bad337e3d7/SudokuAutoTest/Logger.cs\
    public static class Logger
    {
        private static readonly string logPath = Path.Combine(Directory.GetCurrentDirectory(), "log-"+ DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")+".txt");

        public static void Error(string message)
        {
            WriteEntry(message, "ERROR");
        }

        public static void Warning(string message)
        {
            WriteEntry(message, "WARNING");
        }

        public static void Info(string message)
        {
            WriteEntry(message, "INFO");
        }

        private static void WriteEntry(string message, string type)
        {
            Console.WriteLine($"{type} FROM [{logPath}] : {message}");
            using (var sw = new StreamWriter(logPath, true))
            {
                sw.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}  {type}  {message}");
                sw.Close();
            }
        }
    }

}
