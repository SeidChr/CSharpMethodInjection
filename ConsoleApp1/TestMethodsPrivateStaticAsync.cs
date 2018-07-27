
namespace ConsoleApp1
{
    using System;
    using System.Diagnostics;

    using System.Threading.Tasks;

    public class TestMethodsPrivateStaticAsync
    {
        public static async Task DoNotOptimizeMethod()
        {
            await NeedsAnalysisAsync(1);
            await PlaceholderAsync(1);
            await DoesAnalysisAsync(1);
        }

        public static async Task<int> TestCall(int value)
        {
            return await NeedsAnalysisAsync(value);
        }

        private static async Task<int> NeedsAnalysisAsync(int someParameter)
        {
            await Task.Delay(1000);
            return someParameter + 2;
        }

        private static async Task<int> PlaceholderAsync(int someParameter)
        {
            // generate dynamically
            // shouldnt do anything really
            // all calls to this method will end up in calls to "needs analysis"
            return someParameter;
        }

        private static async Task<int> DoesAnalysisAsync(int someParameter)
        {
            // generate dynamically
            Console.WriteLine("Method Enter");
            var sw = Stopwatch.StartNew();
            // calling "needs analysis"
            var result = await PlaceholderAsync(someParameter);
            sw.Stop();

            Console.WriteLine("Method Exit after " + sw.ElapsedTicks + " Ticks");

            return result;
        }
    }
}