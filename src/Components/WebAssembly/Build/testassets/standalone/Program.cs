using System;

namespace standalone
{
    public class Program
    {
        public static void Main(string[] args)
        {
            GC.KeepAlive(typeof(RazorClassLibrary.Class1));
#if REFERENCE_classlibrarywithsatelliteassemblies
            GC.KeepAlive(typeof(classlibrarywithsatelliteassemblies.Class1));
#endif
#if REFERENCE_RclWithNoDeps
            GC.KeepAlive(typeof(RclWithNoDeps.Class2));
#endif
#if REFERENCE_rclwithpackages
            GC.KeepAlive(typeof(rclwithpackages.Component1));
#endif
#if REFERENCE_Newtonsoft
            GC.KeepAlive(typeof(Newtonsoft.Json.JsonSerializer));
#endif
        }
    }
}
