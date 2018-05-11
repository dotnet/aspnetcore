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

        // For some reason, the generated hash in main.[HASH].bundle.js is different on Windows and Linux, despite
        // the file contents being identical.  Replacing the generated hash with "[HASH]" allows the tests to pass
        // on both platforms.
        public override IEnumerable<string> FilesAfterPublish =>
            base.FilesAfterPublish.Select(f => Regex.Replace(f, @"main\.[0-9a-f]*\.bundle\.js$", "main.[HASH].bundle.js"));

        public override IEnumerable<string> ExpectedFilesAfterPublish => 
            base.ExpectedFilesAfterPublish
            .Concat(new[]
            {
                Path.Combine("wwwroot", "favicon.ico"),
                Path.Combine("ClientApp", "dist", "3rdpartylicenses.txt"),
                Path.Combine("ClientApp", "dist", "glyphicons-halflings-regular.448c34a56d699c29117a.woff2"),
                Path.Combine("ClientApp", "dist", "glyphicons-halflings-regular.89889688147bd7575d63.svg"),
                Path.Combine("ClientApp", "dist", "glyphicons-halflings-regular.e18bbf611f2a2e43afc0.ttf"),
                Path.Combine("ClientApp", "dist", "glyphicons-halflings-regular.f4769f9bdb7466be6508.eot"),
                Path.Combine("ClientApp", "dist", "glyphicons-halflings-regular.fa2772327f55d8198301.woff"),
                Path.Combine("ClientApp", "dist", "index.html"),
                Path.Combine("ClientApp", "dist", "inline.318b50c57b4eba3d437b.bundle.js"),
                Path.Combine("ClientApp", "dist", "main.[HASH].bundle.js"),
                Path.Combine("ClientApp", "dist", "polyfills.bf95165a1d5098766b92.bundle.js"),
                Path.Combine("ClientApp", "dist", "styles.2727681ffee5a66f9fe6.bundle.css"),
            });
    }
}
