using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public class ConsoleApplicationTemplate : ClassLibraryTemplate
    {
        public new static ConsoleApplicationTemplate Instance { get; } = new ConsoleApplicationTemplate();

        protected ConsoleApplicationTemplate() { }

        public override string Name => "console";

        public override string OutputPath { get; } = Path.Combine("Debug", "netcoreapp2.1");

        public override TemplateType Type => TemplateType.ConsoleApplication;

        public override IEnumerable<string> ExpectedBinFilesAfterBuild => Enumerable.Concat(base.ExpectedBinFilesAfterBuild, new[]
        {
            $"{Name}.runtimeconfig.dev.json",
            $"{Name}.runtimeconfig.json",
        }.Select(p => Path.Combine(OutputPath, p)));
    }
}
