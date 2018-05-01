using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public class WebTemplate : ConsoleApplicationTemplate
    {
        public new static WebTemplate Instance { get; } = new WebTemplate();

        protected WebTemplate() { }

        public override string Name => "web";

        public override TemplateType Type => TemplateType.WebApplication;

        public override IEnumerable<string> ExpectedObjFilesAfterBuild => Enumerable.Concat(base.ExpectedObjFilesAfterBuild, new[]
        {
            $"{Name}.RazorAssemblyInfo.cache",
            $"{Name}.RazorAssemblyInfo.cs",
            $"{Name}.RazorTargetAssemblyInfo.cache",
        }.Select(p => Path.Combine(OutputPath, p)));

        public override IEnumerable<string> ExpectedFilesAfterPublish => Enumerable.Concat(base.ExpectedFilesAfterPublish, new[]
        {
            "web.config",
        });
    }
}
