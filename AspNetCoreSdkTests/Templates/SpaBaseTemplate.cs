using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public abstract class SpaBaseTemplate : RazorApplicationBaseTemplate
    {
        protected override string RazorPath => "Pages";

        public override IEnumerable<string> ExpectedObjFilesAfterBuild => Enumerable.Concat(base.ExpectedObjFilesAfterBuild, new[]
        {
            Path.Combine("Razor", RazorPath, "Error.g.cshtml.cs"),
        }.Select(p => Path.Combine(OutputPath, p)));
    }
}
