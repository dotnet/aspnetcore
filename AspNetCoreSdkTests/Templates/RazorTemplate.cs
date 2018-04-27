using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public class RazorTemplate : WebTemplate
    {
        public override string Name => "razor";

        public override IEnumerable<string> ExpectedObjFilesAfterBuild => Enumerable.Concat(base.ExpectedObjFilesAfterBuild, new[]
        {
            $"{Name}.RazorCoreGenerate.cache",
            $"{Name}.RazorTargetAssemblyInfo.cs",
            $"{Name}.TagHelpers.input.cache",
            $"{Name}.TagHelpers.output.cache",
            $"{Name}.Views.dll",
            $"{Name}.Views.pdb",
            Path.Combine("Razor", "Pages", "_ViewImports.g.cshtml.cs"),
            Path.Combine("Razor", "Pages", "_ViewStart.g.cshtml.cs"),
            Path.Combine("Razor", "Pages", "About.g.cshtml.cs"),
            Path.Combine("Razor", "Pages", "Contact.g.cshtml.cs"),
            Path.Combine("Razor", "Pages", "Error.g.cshtml.cs"),
            Path.Combine("Razor", "Pages", "Index.g.cshtml.cs"),
            Path.Combine("Razor", "Pages", "Privacy.g.cshtml.cs"),
            Path.Combine("Razor", "Pages", "Shared", "_CookieConsentPartial.g.cshtml.cs"),
            Path.Combine("Razor", "Pages", "Shared", "_Layout.g.cshtml.cs"),
            Path.Combine("Razor", "Pages", "Shared", "_ValidationScriptsPartial.g.cshtml.cs"),
        }.Select(p => Path.Combine(OutputPath, p)));

        public override IEnumerable<string> ExpectedBinFilesAfterBuild => Enumerable.Concat(base.ExpectedBinFilesAfterBuild, new[]
        {
            $"{Name}.Views.dll",
            $"{Name}.Views.pdb",
        }.Select(p => Path.Combine(OutputPath, p)));
    }
}
