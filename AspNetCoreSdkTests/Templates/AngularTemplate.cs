using AspNetCoreSdkTests.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AspNetCoreSdkTests.Templates
{
    public class AngularTemplate : SpaBaseTemplate
    {
        public AngularTemplate() { }

        public override string Name => "angular";

        protected override IEnumerable<string> NormalizeFilesAfterPublish(IEnumerable<string> filesAfterPublish)
        {
            // Remove generated hashes since they may vary by platform
            return base.NormalizeFilesAfterPublish(filesAfterPublish)
                .Select(f => Regex.Replace(f, @"\.[0-9a-f]{20}\.", ".[HASH]."));
        }

        private IDictionary<string, Func<IEnumerable<string>>> _additionalFilesAfterPublish =>
            new Dictionary<string, Func<IEnumerable<string>>>()
            {
                { "common", () => new[]
                    {
                        Path.Combine("wwwroot", "favicon.ico"),
                        Path.Combine("ClientApp", "dist", "3rdpartylicenses.txt"),
                        Path.Combine("ClientApp", "dist", "index.html"),
                    }
                },
                { "netcoreapp2.1", () =>
                    _additionalFilesAfterPublish["common"]()
                    .Concat(new[]
                    {
                        Path.Combine("ClientApp", "dist", "glyphicons-halflings-regular.[HASH].woff2"),
                        Path.Combine("ClientApp", "dist", "glyphicons-halflings-regular.[HASH].svg"),
                        Path.Combine("ClientApp", "dist", "glyphicons-halflings-regular.[HASH].ttf"),
                        Path.Combine("ClientApp", "dist", "glyphicons-halflings-regular.[HASH].eot"),
                        Path.Combine("ClientApp", "dist", "glyphicons-halflings-regular.[HASH].woff"),
                        Path.Combine("ClientApp", "dist", $"inline.[HASH].bundle.js"),
                        Path.Combine("ClientApp", "dist", $"main.[HASH].bundle.js"),
                        Path.Combine("ClientApp", "dist", $"polyfills.[HASH].bundle.js"),
                        Path.Combine("ClientApp", "dist", $"styles.[HASH].bundle.css"),
                    })
                },
                { "netcoreapp2.2", () =>
                    _additionalFilesAfterPublish["common"]()
                    .Concat(new[]
                    {
                        Path.Combine("ClientApp", "dist", $"runtime.[HASH].js"),
                        Path.Combine("ClientApp", "dist", $"main.[HASH].js"),
                        Path.Combine("ClientApp", "dist", $"polyfills.[HASH].js"),
                        Path.Combine("ClientApp", "dist", $"styles.[HASH].css"),
                    })
                },
            };

        public override IEnumerable<string> ExpectedFilesAfterPublish =>
            base.ExpectedFilesAfterPublish
            .Concat(_additionalFilesAfterPublish[DotNetUtil.TargetFrameworkMoniker]());
    }
}
