using System;

namespace classlibrarywithsatelliteassemblies
{
    public class Class1
    {
        public static void Test()
        {
            GC.KeepAlive(typeof(Microsoft.CodeAnalysis.CSharp.CSharpCompilation));
        }
    }
}