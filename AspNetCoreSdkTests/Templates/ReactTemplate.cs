using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public class ReactTemplate : SpaBaseTemplate
    {
        public new static ReactTemplate Instance { get; } = new ReactTemplate();

        protected ReactTemplate() { }

        public override string Name => "react";

        public override IEnumerable<string> ExpectedFilesAfterPublish => Enumerable.Concat(base.ExpectedFilesAfterPublish, new[]
        {
            Path.Combine("ClientApp", "build", "asset-manifest.json"),
            Path.Combine("ClientApp", "build", "favicon.ico"),
            Path.Combine("ClientApp", "build", "index.html"),
            Path.Combine("ClientApp", "build", "manifest.json"),
            Path.Combine("ClientApp", "build", "service-worker.js"),
            Path.Combine("ClientApp", "build", "static", "css", "main.8302bbea.css"),
            Path.Combine("ClientApp", "build", "static", "css", "main.8302bbea.css.map"),
            Path.Combine("ClientApp", "build", "static", "js", "main.31eb739b.js"),
            Path.Combine("ClientApp", "build", "static", "js", "main.31eb739b.js.map"),
            Path.Combine("ClientApp", "build", "static", "media", "glyphicons-halflings-regular.448c34a5.woff2"),
            Path.Combine("ClientApp", "build", "static", "media", "glyphicons-halflings-regular.89889688.svg"),
            Path.Combine("ClientApp", "build", "static", "media", "glyphicons-halflings-regular.e18bbf61.ttf"),
            Path.Combine("ClientApp", "build", "static", "media", "glyphicons-halflings-regular.f4769f9b.eot"),
            Path.Combine("ClientApp", "build", "static", "media", "glyphicons-halflings-regular.fa277232.woff"),
        });
    }
}
