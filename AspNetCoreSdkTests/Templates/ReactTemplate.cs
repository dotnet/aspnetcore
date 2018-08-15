using AspNetCoreSdkTests.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AspNetCoreSdkTests.Templates
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

        private IDictionary<string, Func<IEnumerable<string>>> _additionalFilesAfterPublish =>
            new Dictionary<string, Func<IEnumerable<string>>>()
            {
                { "common", () => new[]
                    {
                        Path.Combine("ClientApp", "build", "asset-manifest.json"),
                        Path.Combine("ClientApp", "build", "favicon.ico"),
                        Path.Combine("ClientApp", "build", "index.html"),
                        Path.Combine("ClientApp", "build", "manifest.json"),
                        Path.Combine("ClientApp", "build", "service-worker.js"),
                        Path.Combine("ClientApp", "build", "static", "css", "main.[HASH].css"),
                        Path.Combine("ClientApp", "build", "static", "css", "main.[HASH].css.map"),
                        Path.Combine("ClientApp", "build", "static", "js", "main.[HASH].js"),
                        Path.Combine("ClientApp", "build", "static", "js", "main.[HASH].js.map"),
                    }
                },
                { "netcoreapp2.1", () =>
                    _additionalFilesAfterPublish["common"]()
                    .Concat(new[]
                    {
                        Path.Combine("ClientApp", "build", "static", "media", "glyphicons-halflings-regular.[HASH].woff2"),
                        Path.Combine("ClientApp", "build", "static", "media", "glyphicons-halflings-regular.[HASH].svg"),
                        Path.Combine("ClientApp", "build", "static", "media", "glyphicons-halflings-regular.[HASH].ttf"),
                        Path.Combine("ClientApp", "build", "static", "media", "glyphicons-halflings-regular.[HASH].eot"),
                        Path.Combine("ClientApp", "build", "static", "media", "glyphicons-halflings-regular.[HASH].woff"),
                    })
                },
                { "netcoreapp2.2", () => _additionalFilesAfterPublish["common"]() },
            };

        public override IEnumerable<string> ExpectedFilesAfterPublish =>
            base.ExpectedFilesAfterPublish
            .Concat(_additionalFilesAfterPublish[DotNetUtil.TargetFrameworkMoniker]());
    }
}
