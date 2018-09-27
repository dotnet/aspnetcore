// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cli.FunctionalTests.Util;

namespace Cli.FunctionalTests.Templates
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
                        Path.Combine(DotNetUtil.TargetFrameworkMoniker, RuntimeIdentifier.Path, "host", $"{Name}.exe"),
                    })
                },
                { RuntimeIdentifier.Linux_x64, () =>
                    _additionalObjFilesAfterBuild[RuntimeIdentifier.None]()
                    .Concat(new[]
                    {
                        Path.Combine(DotNetUtil.TargetFrameworkMoniker, RuntimeIdentifier.Path, "host", $"{Name}"),
                    })
                },
                { RuntimeIdentifier.OSX_x64, () => _additionalObjFilesAfterBuild[RuntimeIdentifier.Linux_x64]() },
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
            .Concat(RazorUtil.GetExpectedFilesAfterPublish(this));
    }
}
