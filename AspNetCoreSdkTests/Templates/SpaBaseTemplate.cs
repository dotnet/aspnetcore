using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public abstract class SpaBaseTemplate : RazorApplicationBaseTemplate
    {
        protected override string RazorPath => "Pages";

        public override IEnumerable<string> ExpectedObjFilesAfterBuild =>
            base.ExpectedObjFilesAfterBuild
            .Concat(new[]
            {
                Path.Combine("Razor", RazorPath, "Error.g.cshtml.cs"),
            }.Select(p => Path.Combine(OutputPath, p)));
    }
}
