using System;
using System.Reflection;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class Core12Toolchain : Toolchain
    {
        private const string TargetFrameworkMoniker = "netcoreapp1.1";

        public static readonly IToolchain Instance = new Core12Toolchain();

        public Core12Toolchain()
            : base("Core12",
                (IGenerator) Activator.CreateInstance(typeof(Toolchain).GetTypeInfo().Assembly.GetType("BenchmarkDotNet.Toolchains.DotNetCli.DotNetCliGenerator"),
                    TargetFrameworkMoniker,
                    GetExtraDependencies(),
                    (Func<Platform, string>) (_ => "x64"), // dotnet cli supports only x64 compilation now
                    GetImports(),
                    GetRuntime()),
                new DotNetCliBuilder(TargetFrameworkMoniker),
                (IExecutor) Activator.CreateInstance(typeof(Toolchain).GetTypeInfo().Assembly.GetType("BenchmarkDotNet.Toolchains.Executor")))
        {
        }

        public override bool IsSupported(Benchmark benchmark, ILogger logger, IResolver resolver)
        {
            if (!base.IsSupported(benchmark, logger, resolver))
            {
                return false;
            }

            return true;
        }

        private static string GetExtraDependencies()
        {
            // do not set the type to platform in order to produce exe
            // https://github.com/dotnet/core/issues/77#issuecomment-219692312
            return "\"dependencies\": { \"Microsoft.NETCore.App\": { \"version\": \"1.2-*\" } },";
        }

        private static string GetImports()
        {
            return "[ \"dnxcore50\", \"portable-net45+win8\", \"dotnet5.6\", \"netcore50\" ]";
        }

        private static string GetRuntime()
        {
            var currentRuntime = Microsoft.DotNet.InternalAbstractions.RuntimeEnvironment.GetRuntimeIdentifier(); ;
            if (!string.IsNullOrEmpty(currentRuntime))
            {
                return $"\"runtimes\": {{ \"{currentRuntime}\": {{ }} }},";
            }

            return string.Empty;
        }
    }
}
