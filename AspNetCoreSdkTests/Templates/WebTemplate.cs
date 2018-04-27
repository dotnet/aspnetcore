using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public class WebTemplate : ConsoleApplicationTemplate
    {
        public override string Name => "web";

        public override TemplateType Type => TemplateType.Application;

        public override IEnumerable<string> ExpectedObjFilesAfterBuild => Enumerable.Concat(base.ExpectedObjFilesAfterBuild, new[]
        {
            $"{Name}.RazorAssemblyInfo.cache",
            $"{Name}.RazorAssemblyInfo.cs",
            $"{Name}.RazorTargetAssemblyInfo.cache",
        }.Select(p => Path.Combine(OutputPath, p)));

    }
}
