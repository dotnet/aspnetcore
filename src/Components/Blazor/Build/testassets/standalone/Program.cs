using System;

namespace standalone
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if REFERENCE_classlibrarywithsatelliteassemblies
            GC.KeepAlive(typeof(classlibrarywithsatelliteassemblies.Class1));
#endif
        }
    }
}
