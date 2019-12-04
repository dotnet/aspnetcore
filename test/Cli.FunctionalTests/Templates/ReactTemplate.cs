// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cli.FunctionalTests.Templates
{
    public class ReactTemplate : SpaBaseTemplate
    {
        public ReactTemplate() { }

        public override string Name => "react";

        protected override IEnumerable<string> NormalizeFilesAfterPublish(IEnumerable<string> filesAfterPublish)
        {
            // Remove generated hashes since they may vary by platform
            return base.NormalizeFilesAfterPublish(filesAfterPublish)
                .Select(f => Regex.Replace(f, @"\.[0-9a-f]{8}\.", ".[HASH]."));
        }

        public override IEnumerable<string> ExpectedFilesAfterPublish =>
            base.ExpectedFilesAfterPublish
            .Concat(new[] {
                Path.Combine("ClientApp", "build", "asset-manifest.json"),
                Path.Combine("ClientApp", "build", "favicon.ico"),
                Path.Combine("ClientApp", "build", "index.html"),
                Path.Combine("ClientApp", "build", "manifest.json"),
                Path.Combine("ClientApp", "build", "service-worker.js"),
                Path.Combine("ClientApp", "build", "static", "css", "main.[HASH].css"),
                Path.Combine("ClientApp", "build", "static", "css", "main.[HASH].css.map"),
                Path.Combine("ClientApp", "build", "static", "js", "main.[HASH].js"),
                Path.Combine("ClientApp", "build", "static", "js", "main.[HASH].js.map"),
            });
    }
}
