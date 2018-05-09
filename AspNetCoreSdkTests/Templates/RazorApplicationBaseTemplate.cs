using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public abstract class RazorApplicationBaseTemplate : WebTemplate
    {
        protected abstract string RazorPath { get; }
    
        private IDictionary<RuntimeIdentifier, Func<IEnumerable<string>>> _additionalObjFilesAfterBuild =>
            new Dictionary<RuntimeIdentifier, Func<IEnumerable<string>>>()
            {
                { RuntimeIdentifier.None, () => new[]
                    {
                        Path.Combine("Razor", RazorPath, "_ViewImports.g.cshtml.cs"),
                    }.Select(p => Path.Combine(OutputPath, p))
                },
                { RuntimeIdentifier.Win_x64, () =>
                    _additionalObjFilesAfterBuild[RuntimeIdentifier.None]()
                    .Concat(new[]
                    {
                        Path.Combine("netcoreapp2.1", RuntimeIdentifier.Path, "host", $"{Name}.exe"),
                    })
                },
                { RuntimeIdentifier.Linux_x64, () =>
                    _additionalObjFilesAfterBuild[RuntimeIdentifier.None]()
                    .Concat(new[]
                    {
                        Path.Combine("netcoreapp2.1", RuntimeIdentifier.Path, "host", $"{Name}"),
                    })
                },
            };

        public override IEnumerable<string> ExpectedObjFilesAfterBuild =>
            base.ExpectedObjFilesAfterBuild
            .Concat(RazorUtil.GetExpectedObjFilesAfterBuild(this))
            .Concat(_additionalObjFilesAfterBuild[RuntimeIdentifier]())
            // Some files are duplicated in WebTemplate and RazorUtil, since they are needed by RazorClassLibraryTemplate
            .Distinct();

        public override IEnumerable<string> ExpectedBinFilesAfterBuild =>
            base.ExpectedBinFilesAfterBuild
            .Concat(RazorUtil.GetExpectedBinFilesAfterBuild(this));

        public override IEnumerable<string> ExpectedFilesAfterPublish =>
            base.ExpectedFilesAfterPublish
            .Concat(RazorUtil.GetExpectedFilesAfterPublish(this))
            .Concat(new[]
            {
                "appsettings.Development.json",
                "appsettings.json",
            });
    }
}
