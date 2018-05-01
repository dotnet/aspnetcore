using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public class AngularTemplate : SpaBaseTemplate
    {
        public new static AngularTemplate Instance { get; } = new AngularTemplate();

        protected AngularTemplate() { }

        public override string Name => "angular";

        public override IEnumerable<string> ExpectedFilesAfterPublish => Enumerable.Concat(base.ExpectedFilesAfterPublish, new[]
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
            Path.Combine("ClientApp", "dist", "main.d2eed1593a6df639e365.bundle.js"),
            Path.Combine("ClientApp", "dist", "polyfills.bf95165a1d5098766b92.bundle.js"),
            Path.Combine("ClientApp", "dist", "styles.2727681ffee5a66f9fe6.bundle.css"),
        });
    }
}
