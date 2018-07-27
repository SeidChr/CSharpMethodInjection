using System;
using System.Diagnostics;

namespace ConsoleApp1
{
    public class TestMethodsPublicStatic
    {
        public static int NeedsAnalysis(int someParameter)
        {
            return someParameter + 2;
        }

        public static int Placeholder(int someParameter)
        {
            // generate dynamically
            // shouldnt do anything really
            // all calls to this method will end up in calls to "needs analysis"
            return someParameter;
        }

        public static int DoesAnalysis(int someParameter)
        {
            // generate dynamically
            Console.WriteLine("Method Enter");
            var sw = Stopwatch.StartNew();
            // calling "needs analysis"
            var result = Placeholder(someParameter);
            sw.Stop();

            Console.WriteLine("Method Exit after " + sw.ElapsedTicks + " Ticks");

            return result;
        }
    }
}