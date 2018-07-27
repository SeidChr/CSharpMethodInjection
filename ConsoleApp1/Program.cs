using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    using System.CodeDom.Compiler;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Microsoft.CSharp;
    using System.Reflection;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Emit;

    class Program
    {
        static async Task Main(string[] args)
        {
            //////
            /// Task 1: (automatically) Create a dummy method with same signature as NeedsAnalysis
            /// Task 2: (automatically) Create a "DoesAnalysis" method with the same signature and a target call to the dummy method.
            ////// 

            // dynamic methods
            // https://msdn.microsoft.com/de-de/library/system.reflection.emit.dynamicmethod(v=vs.110).aspx
            // needsAnalysisMethod.CreateDelegate(typeof(Program)).

            TestPublicStatic();
            await TestPublicStaticAsync();
            await TestPrivateStaticAsync(4);
            await TestPrivateAsync(5);

            //var injectionType = GetMethodType();
            //doesAnalysisMethod = injectionType.GetMethod("MethodA");
            //placeholderMethod = injectionType.GetMethod("DummyA");

            Console.ReadLine();
        }

        private static void TestPublicStatic(int value)
        {
            var needsAnalysisMethod = typeof(TestMethodsPublicStatic).GetMethod(nameof(TestMethodsPublicStatic.NeedsAnalysis));
            var doesAnalysisMethod = typeof(TestMethodsPublicStatic).GetMethod(nameof(TestMethodsPublicStatic.DoesAnalysis));
            var placeholderMethod = typeof(TestMethodsPublicStatic).GetMethod(nameof(TestMethodsPublicStatic.Placeholder));
            Console.WriteLine(TestMethodsPublicStatic.NeedsAnalysis(value));
            ReplaceMethod(placeholderMethod, needsAnalysisMethod);
            ReplaceMethod(needsAnalysisMethod, doesAnalysisMethod);
            Console.WriteLine(TestMethodsPublicStatic.NeedsAnalysis(value));
        }

        private static async Task TestPublicStaticAsync(int value)
        {
            var needsAnalysisAsyncMethod = typeof(TestMethodsPublicStaticAsync).GetMethod(nameof(TestMethodsPublicStaticAsync.NeedsAnalysisAsync));
            var doesAnalysisAsyncMethod = typeof(TestMethodsPublicStaticAsync).GetMethod(nameof(TestMethodsPublicStaticAsync.DoesAnalysisAsync));
            var placeholderAsyncMethod = typeof(TestMethodsPublicStaticAsync).GetMethod(nameof(TestMethodsPublicStaticAsync.PlaceholderAsync));
            Console.WriteLine(await TestMethodsPublicStaticAsync.NeedsAnalysisAsync(value));
            ReplaceMethod(placeholderAsyncMethod, needsAnalysisAsyncMethod);
            ReplaceMethod(needsAnalysisAsyncMethod, doesAnalysisAsyncMethod);
            Console.WriteLine(await TestMethodsPublicStaticAsync.NeedsAnalysisAsync(value));
        }

        private static async Task TestPrivateStaticAsync(int value)
        {
            var needsAnalysisAsyncMethod = typeof(TestMethodsPrivateStaticAsync).GetMethod("NeedsAnalysisAsync", BindingFlags.NonPublic | BindingFlags.Static);
            var doesAnalysisAsyncMethod = typeof(TestMethodsPrivateStaticAsync).GetMethod("DoesAnalysisAsync", BindingFlags.NonPublic | BindingFlags.Static);
            var placeholderAsyncMethod = typeof(TestMethodsPrivateStaticAsync).GetMethod("PlaceholderAsync", BindingFlags.NonPublic | BindingFlags.Static);
            Console.WriteLine(await TestMethodsPrivateStaticAsync.TestCall(value));
            ReplaceMethod(placeholderAsyncMethod, needsAnalysisAsyncMethod);
            ReplaceMethod(needsAnalysisAsyncMethod, doesAnalysisAsyncMethod);
            Console.WriteLine(await TestMethodsPrivateStaticAsync.TestCall(value));
        }

        private static async Task TestPrivateAsync(int value)
        {
            var instance = new TestMethodsPrivateAsync();
            var needsAnalysisAsyncMethod = typeof(TestMethodsPrivateAsync).GetMethod("NeedsAnalysisAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var doesAnalysisAsyncMethod = typeof(TestMethodsPrivateAsync).GetMethod("DoesAnalysisAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var placeholderAsyncMethod = typeof(TestMethodsPrivateAsync).GetMethod("PlaceholderAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Console.WriteLine(await instance.TestCall(value));
            ReplaceMethod(placeholderAsyncMethod, needsAnalysisAsyncMethod);
            ReplaceMethod(needsAnalysisAsyncMethod, doesAnalysisAsyncMethod);
            Console.WriteLine(await instance.TestCall(value));
        }

        ////public static string GetMethodSignature(MethodInfo methodInfo)
        ////{
        ////    var builder = new StringBuilder("public static " + methodInfo.ReturnType.ToString() + " " + methodInfo.Name + "Facade");

        ////    methodInfo.
        ////    foreach(var p in methodInfo.GetParameters())
        ////    {
        ////        p.
        ////    }
        ////}

        public static Type GetMethodType()
        {
            //https://stackoverflow.com/a/29417053

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace Injection
{
    using System;
    using System.Diagnostics;

    public class MethodInjection
    {

        public static int DummyA(int someParameter)
        {
            // generate dynamically
            // shouldnt do anything really
            // all calls to this method will end up in calls to needs analysis
            return someParameter;
        }

        public static int MethodA(int someParameter)
        {
            // generate dynamically
            Console.WriteLine(""Method Enter"");
            var sw = Stopwatch.StartNew();
            // calling needs analysis
            var result = DummyA(someParameter);
            sw.Stop();

            Console.WriteLine(""Method Exit after "" + sw.ElapsedTicks + "" Ticks"");

            return result;
        }
    }
}");

            var assemblyName = Path.GetRandomFileName();
            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Stopwatch).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            Type resultType = null;

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);

                if (!result.Success)
                {
                    var failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (var diagnostic in failures)
                    {
                        Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    var assembly = Assembly.Load(ms.ToArray());

                    var type = assembly.GetType("Injection.MethodInjection");
                    resultType = type;
                    //object obj = Activator.CreateInstance(type);
                    //type.InvokeMember("Write",
                    //    BindingFlags.Default | BindingFlags.InvokeMethod,
                    //    null,
                    //    obj,
                    //    new object[] { "Hello World" });
                }

                return resultType;
            }
        }

        public static void ReplaceMethod(MethodInfo methodToReplace, MethodInfo methodToInject)
        {
            //MethodInfo methodToReplace = typeof(Program).GetMethod("targetMethod" + funcNum, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            //MethodInfo methodToInject = typeof(Program).GetMethod("injectionMethod" + funcNum, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            RuntimeHelpers.PrepareMethod(methodToReplace.MethodHandle);
            RuntimeHelpers.PrepareMethod(methodToInject.MethodHandle);

            unsafe
            {
                if (IntPtr.Size == 4)
                {
                    int* inj = (int*)methodToInject.MethodHandle.Value.ToPointer() + 2;
                    int* tar = (int*)methodToReplace.MethodHandle.Value.ToPointer() + 2;
#if DEBUG
                    ////Console.WriteLine("\nVersion x86 Debug\n");

                    byte* injInst = (byte*)*inj;
                    byte* tarInst = (byte*)*tar;

                    int* injSrc = (int*)(injInst + 1);
                    int* tarSrc = (int*)(tarInst + 1);

                    *tarSrc = (((int)injInst + 5) + *injSrc) - ((int)tarInst + 5);
#else
                    Console.WriteLine("\nVersion x86 Release\n");
                    *tar = *inj;
#endif
                }
                else
                {

                    long* injectionPointer = (long*)methodToInject.MethodHandle.Value.ToPointer() + 1;
                    long* targetPointer = (long*)methodToReplace.MethodHandle.Value.ToPointer() + 1;
#if DEBUG
                    ////Console.WriteLine("\nVersion x64 Debug\n");
                    byte* injectionInstance = (byte*)*injectionPointer;
                    byte* targetInstance = (byte*)*targetPointer;

                    int* injSrc = (int*)(injectionInstance + 1);
                    int* tarSrc = (int*)(targetInstance + 1);

                    *tarSrc = (((int)injectionInstance + 5) + *injSrc) - ((int)targetInstance + 5);
#else
                    Console.WriteLine("\nVersion x64 Release\n");
                    *tar = *inj;
#endif
                }
            }
        }
    }
}
