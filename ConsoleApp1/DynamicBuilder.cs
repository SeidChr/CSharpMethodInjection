namespace ConsoleApp1
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.InteropServices;

    public class DynamicBuilder
    {
        public static int Dummy(string[] param)
        {
            const string x = "hallo";
            Console.WriteLine(x);
            return 0;
        }

        public static void SayHelloFromTheOtherSide()
        {
            var an = new AssemblyName
            {
                Name = "HelloReflectionEmit"
            };

            var ad = AppDomain.CurrentDomain;
            var ab = ad.DefineDynamicAssembly(an,AssemblyBuilderAccess.RunAndSave);

            // module
            var mb = ab.DefineDynamicModule(an.Name, "Hello.exe");

            var tb = mb.DefineType("Foo.Bar",
                TypeAttributes.Public | TypeAttributes.Class);

            var fb = tb.DefineMethod("Main",
                MethodAttributes.Public |
                MethodAttributes.Static,
                typeof(int), new Type[] { typeof(string[]) });

            ////Expression<Func<string[], int>> ex = new Func<string[], int>( (s) =>
            ////{
            ////    Console.WriteLine("Hello World");
            ////    return 0;
            ////}).;

            var dummyBody = GetMethodBody(typeof(DynamicBuilder), nameof(Dummy));

            fb.CreateMethodBody(dummyBody, dummyBody.Length);
            // Emit the ubiquitous "Hello, World!" method, in IL
            //ILGenerator ilg = fb.GetILGenerator();

            //ilg.Emit(OpCodes.Ldstr, "Hello, World!");
            //ilg.Emit(OpCodes.Call,
            //    typeof(Console).GetMethod("WriteLine",
            //        new Type[] { typeof(string) }));

            // return 0
            //ilg.Emit(OpCodes.Ldc_I4_0); // load 4byte integer 0 into stack
            //ilg.Emit(OpCodes.Ret); // return last value from stack (4 byte integer zero)

            var type = tb.CreateType();

            var body = GetMethodBody(typeof(DynamicBuilder), nameof(Dummy));
            SetMethodBody(type, fb.GetToken(), body);

            type.GetMethod("Main")
                ?.Invoke(
                null,
                new object[]
                {
                    new string[] {"a"}
                });
        }

        public static void GetMethod()
        {
            // https://stackoverflow.com/questions/1618682/linking-a-net-expression-tree-into-a-new-assembly
            // https://msdn.microsoft.com/en-us/library/bb345362.aspx
            // https://stackoverflow.com/a/24930138
            Expression<Func<int, bool>> ex = i => i < 2;
        }

        public static byte[] GetMethodBody(Type type, string methodName)
        {
            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
            return method
                ?.GetMethodBody()
                ?.GetILAsByteArray() 
                ?? new byte[]{};
        }

        public static void SetMethodBody(Type type, MethodToken somemethodToken, byte[] methodBytes)
        {
            //MethodToken somemethodToken = somemethod.GetToken();

            // Get the pointer to the method body.
            GCHandle methodBytesHandle = GCHandle.Alloc((Object)methodBytes, GCHandleType.Pinned);
            IntPtr methodBytesAddress = methodBytesHandle.AddrOfPinnedObject();
            int methodBytesLength = methodBytes.Length;

            // Swap the old method body with the new body.
            MethodRental.SwapMethodBody(
                type,
                somemethodToken.Token,
                methodBytesAddress,
                methodBytesLength,
                MethodRental.JitImmediate);

            ////// Verify that the modified method returns 1.
            ////int res2 = (int)tbBaked.GetMethod("My method Name").Invoke(null, new Object[] { });
            ////if (res2 != 1)
            ////{
            ////    Console.WriteLine("Err_001b, should have returned 1");
            ////}
            ////else
            ////{
            ////    Console.WriteLine("Swapped method body returned 1");
            ////}
        }
    }
}
