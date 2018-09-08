using CommandLine;

namespace AutoTest
{
    [Verb("test", HelpText = "Run test program.")]
    public class TestOptions
    {
        [Option("config", Required = true, HelpText = "Path of config file.")]
        public string ConfigPath { get; set; }
        
        [Option("all", Required = false, Default = true, HelpText = "Test all program in the directory")]
        public bool TestAllFlag { get; set; }
        
        [Option("single", Required = false, HelpText = "Test single program in the path.")]
        public string StudentID { get; set; }
    }
}
