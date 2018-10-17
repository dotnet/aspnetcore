using System;
using System.Runtime.InteropServices;

namespace Arm32TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Hello World from {RuntimeInformation.OSArchitecture}!");
        }
    }
}
