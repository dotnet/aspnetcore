using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public abstract class RazorApplicationBaseTemplate : RazorBaseTemplate
    {
        protected abstract string RazorPath { get; }

        public override string OutputPath { get; } = Path.Combine("Debug", "netcoreapp2.1");

        public override TemplateType Type => TemplateType.WebApplication;

        public override IEnumerable<string> ExpectedObjFilesAfterBuild => Enumerable.Concat(base.ExpectedObjFilesAfterBuild, new[]
         {
            Path.Combine("Razor", RazorPath, "_ViewImports.g.cshtml.cs"),
        }.Select(p => Path.Combine(OutputPath, p)));

        public override IEnumerable<string> ExpectedBinFilesAfterBuild => Enumerable.Concat(base.ExpectedBinFilesAfterBuild, new[]
        {
            $"{Name}.runtimeconfig.dev.json",
            $"{Name}.runtimeconfig.json",
        }.Select(p => Path.Combine(OutputPath, p)));

        public override IEnumerable<string> ExpectedFilesAfterPublish => Enumerable.Concat(base.ExpectedFilesAfterPublish, new[]
        {
            "appsettings.Development.json",
            "appsettings.json",
            $"{Name}.runtimeconfig.json",
            "web.config",
        });
    }
}
