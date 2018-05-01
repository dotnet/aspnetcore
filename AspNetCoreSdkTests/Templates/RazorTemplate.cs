using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public class RazorTemplate : RazorBootstrapJQueryTemplate
    {
        public new static RazorTemplate Instance { get; } = new RazorTemplate();

        protected RazorTemplate() { }

        public override string Name => "razor";

        protected override string RazorPath => "Pages";

        public override IEnumerable<string> ExpectedObjFilesAfterBuild => Enumerable.Concat(base.ExpectedObjFilesAfterBuild, new[]
        {
            Path.Combine("Razor", RazorPath, "_ViewStart.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "About.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Contact.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Error.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Index.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Privacy.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Shared", "_CookieConsentPartial.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Shared", "_Layout.g.cshtml.cs"),
            Path.Combine("Razor", RazorPath, "Shared", "_ValidationScriptsPartial.g.cshtml.cs"),
        }.Select(p => Path.Combine(OutputPath, p)));
    }
}
