using System.Reflection;
using BenchmarkDotNet.Running;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
        }
    }
}