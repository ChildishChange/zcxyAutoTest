using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTest
{
    public class ExerciseHandler
    {
        public static char[] reversibleOps = { '+', '*' };
        public static char[] Ops = { '+', '*','-','/' };



        public static bool Calculate(string exercise, string answer)
        {
            if (exercise.Contains('+'))
            {
                var ops = exercise.Split('+');
                return int.Parse(answer) == int.Parse(ops[0]) + int.Parse(ops[1]);
            }
            if (exercise.Contains('-'))
            {
                var ops = exercise.Split('-');
                return int.Parse(answer) == int.Parse(ops[0]) - int.Parse(ops[1]);
            }
            if (exercise.Contains('×'))
            {
                var ops = exercise.Split('×');
                return int.Parse(answer) == int.Parse(ops[0]) * int.Parse(ops[1]);
            }
            if (exercise.Contains('÷'))
            {
                var ops = exercise.Split('÷');
                if (int.Parse(ops[0]) % int.Parse(ops[1]) == 0)
                {
                    if (answer.Contains('.')) return false;
                    return int.Parse(ops[0]) / int.Parse(ops[1]) == int.Parse(answer);
                }
                else
                {
                    if (!answer.Contains('.')) return false;

                    var ans = answer.Replace("...", " ").Split(' ');

                    return int.Parse(ops[1]) * int.Parse(ans[0]) + int.Parse(ans[1]) == int.Parse(ops[0]);

                }
            }

            return false;
        }
        public static string Swap(string line)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char op in reversibleOps)
            {
                if (line.Contains(op))
                {
                    line = line.TrimEnd('=');
                    var ops = line.Split(op);
                    sb.Append((int.Parse(ops[0]) > int.Parse(ops[1])) ? ops[1] + op + ops[0] : line);
                    return sb.ToString();
                }
            }
            return line;
        }
    }
}
