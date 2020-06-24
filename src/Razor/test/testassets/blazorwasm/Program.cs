using System;

namespace standalone
{
    public class Program
    {
        public static void Main(string[] args)
        {
            GC.KeepAlive(typeof(System.Text.Json.JsonSerializer));
            GC.KeepAlive(typeof(RazorClassLibrary.Class1));
#if REFERENCE_classlibrarywithsatelliteassemblies
            GC.KeepAlive(typeof(classlibrarywithsatelliteassemblies.Class1));
#endif
        }
    }
}
