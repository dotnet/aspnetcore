using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public class MvcTemplate : RazorBootstrapJQueryTemplate
    {
        public new static MvcTemplate Instance { get; } = new MvcTemplate();

        protected MvcTemplate() { }

        public override string Name => "mvc";

        protected override string RazorPath => "Views";

        public override IEnumerable<string> ExpectedObjFilesAfterBuild => Enumerable.Concat(base.ExpectedObjFilesAfterBuild, new[]
        {
            Path.Combine("Razor", RazorPath, "_ViewStart.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Home", "About.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Home", "Contact.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Home", "Index.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Home", "Privacy.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Shared", "_CookieConsentPartial.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Shared", "_Layout.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Shared", "_ValidationScriptsPartial.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Shared", "Error.g.cshtml.cs"),
        }.Select(p => Path.Combine(OutputPath, p)));
    }
}
