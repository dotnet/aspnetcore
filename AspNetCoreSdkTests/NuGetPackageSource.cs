using System;

namespace AspNetCoreSdkTests
{
    public class NuGetPackageSource
    {
        public static NuGetPackageSource None { get; } = new NuGetPackageSource
        {
            Name = nameof(None),
            SourceArgumentLazy = new Lazy<string>(string.Empty),
        };

        public static NuGetPackageSource NuGetOrg { get; } = new NuGetPackageSource
        {
            Name = nameof(NuGetOrg),
            SourceArgumentLazy = new Lazy<string>("--source https://api.nuget.org/v3/index.json"),
        };

        public static NuGetPackageSource DotNetCore { get; } = new NuGetPackageSource
        {
            Name = nameof(DotNetCore),
            SourceArgumentLazy = new Lazy<string>("--source https://dotnet.myget.org/F/dotnet-core/api/v3/index.json"),
        };

        public static NuGetPackageSource EnvironmentVariable { get; } = new NuGetPackageSource
        {
            Name = nameof(EnvironmentVariable),
            SourceArgumentLazy = new Lazy<string>(() => $"--source {GetPackageSourceFromEnvironment()}"),
        };

        public static NuGetPackageSource EnvironmentVariableAndNuGetOrg { get; } = new NuGetPackageSource
        {
            Name = nameof(EnvironmentVariableAndNuGetOrg),
            SourceArgumentLazy = new Lazy<string>(() => string.Join(" ", EnvironmentVariable.SourceArgument, NuGetOrg.SourceArgument)),
        };

        private NuGetPackageSource() { }

        public string Name { get; private set; }
        public string SourceArgument => SourceArgumentLazy.Value;
        private Lazy<string> SourceArgumentLazy { get; set; }

        public override string ToString() => Name;

        private static string GetPackageSourceFromEnvironment()
        {
            return Environment.GetEnvironmentVariable("NUGET_PACKAGE_SOURCE") ??
                throw new InvalidOperationException("Environment variable NUGET_PACKAGE_SOURCE is required but not set");
        }
    }
}
