using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public class AngularTemplate : RazorApplicationBaseTemplate
    {
        public new static AngularTemplate Instance { get; } = new AngularTemplate();

        protected AngularTemplate() { }

        public override string Name => "angular";

        protected override string RazorPath => "Pages";

        public override IEnumerable<string> ExpectedObjFilesAfterBuild => Enumerable.Concat(base.ExpectedObjFilesAfterBuild, new[]
        {
            Path.Combine("Razor", RazorPath, "Error.g.cshtml.cs"),
        }.Select(p => Path.Combine(OutputPath, p)));
    }
}
