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

        // Remove generated hashes since they may vary by platform
        public override IEnumerable<string> FilesAfterPublish =>
            base.FilesAfterPublish.Select(f => Regex.Replace(f, @"\.[0-9a-f]{20}\.", ".[HASH]."));

        public override IEnumerable<string> ExpectedFilesAfterPublish => 
            base.ExpectedFilesAfterPublish
            .Concat(new[]
            {
                Path.Combine("wwwroot", "favicon.ico"),
                Path.Combine("ClientApp", "dist", "3rdpartylicenses.txt"),
                Path.Combine("ClientApp", "dist", "glyphicons-halflings-regular.[HASH].woff2"),
                Path.Combine("ClientApp", "dist", "glyphicons-halflings-regular.[HASH].svg"),
                Path.Combine("ClientApp", "dist", "glyphicons-halflings-regular.[HASH].ttf"),
                Path.Combine("ClientApp", "dist", "glyphicons-halflings-regular.[HASH].eot"),
                Path.Combine("ClientApp", "dist", "glyphicons-halflings-regular.[HASH].woff"),
                Path.Combine("ClientApp", "dist", "index.html"),
                Path.Combine("ClientApp", "dist", "inline.[HASH].bundle.js"),
                Path.Combine("ClientApp", "dist", "main.[HASH].bundle.js"),
                Path.Combine("ClientApp", "dist", "polyfills.[HASH].bundle.js"),
                Path.Combine("ClientApp", "dist", "styles.[HASH].bundle.css"),
            });
    }
}
