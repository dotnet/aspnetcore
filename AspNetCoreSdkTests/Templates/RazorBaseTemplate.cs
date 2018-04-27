using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public abstract class RazorBaseTemplate : WebTemplate
    {
        protected abstract string RazorPath { get; }

        public override IEnumerable<string> ExpectedObjFilesAfterBuild => Enumerable.Concat(base.ExpectedObjFilesAfterBuild, new[]
        {
            $"{Name}.RazorCoreGenerate.cache",
            $"{Name}.RazorTargetAssemblyInfo.cs",
            $"{Name}.TagHelpers.input.cache",
            $"{Name}.TagHelpers.output.cache",
            $"{Name}.Views.dll",
            $"{Name}.Views.pdb",
            Path.Combine("Razor", RazorPath, "_ViewImports.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "_ViewStart.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Shared", "_CookieConsentPartial.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Shared", "_Layout.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Shared", "_ValidationScriptsPartial.g.cshtml.cs"),
        }.Select(p => Path.Combine(OutputPath, p)));

        public override IEnumerable<string> ExpectedBinFilesAfterBuild => Enumerable.Concat(base.ExpectedBinFilesAfterBuild, new[]
        {
            $"{Name}.Views.dll",
            $"{Name}.Views.pdb",
        }.Select(p => Path.Combine(OutputPath, p)));
    }
}
