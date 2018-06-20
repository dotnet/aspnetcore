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

        // Remove generated hashes since they may var by platform
        public override IEnumerable<string> FilesAfterPublish =>
            base.FilesAfterPublish.Select(f => Regex.Replace(f, @"\.[0-9a-f]{8}\.", ".[HASH]."));

        public override IEnumerable<string> ExpectedFilesAfterPublish =>
            base.ExpectedFilesAfterPublish
            .Concat(new[]
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
                Path.Combine("ClientApp", "build", "static", "media", "glyphicons-halflings-regular.[HASH].woff2"),
                Path.Combine("ClientApp", "build", "static", "media", "glyphicons-halflings-regular.[HASH].svg"),
                Path.Combine("ClientApp", "build", "static", "media", "glyphicons-halflings-regular.[HASH].ttf"),
                Path.Combine("ClientApp", "build", "static", "media", "glyphicons-halflings-regular.[HASH].eot"),
                Path.Combine("ClientApp", "build", "static", "media", "glyphicons-halflings-regular.[HASH].woff"),
            });
    }
}
