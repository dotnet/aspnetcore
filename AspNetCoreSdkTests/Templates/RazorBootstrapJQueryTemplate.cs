using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public abstract class RazorBootstrapJQueryTemplate : RazorApplicationBaseTemplate
    {
        public override IEnumerable<string> ExpectedFilesAfterPublish => Enumerable.Concat(base.ExpectedFilesAfterPublish, new[]
        {
            Path.Combine("wwwroot", "favicon.ico"),
            Path.Combine("wwwroot", "css", "site.css"),
            Path.Combine("wwwroot", "css", "site.min.css"),
            Path.Combine("wwwroot", "images", "banner1.svg"),
            Path.Combine("wwwroot", "images", "banner2.svg"),
            Path.Combine("wwwroot", "images", "banner3.svg"),
            Path.Combine("wwwroot", "js", "site.js"),
            Path.Combine("wwwroot", "js", "site.min.js"),
            Path.Combine("wwwroot", "lib", "bootstrap", ".bower.json"),
            Path.Combine("wwwroot", "lib", "bootstrap", "LICENSE"),
            Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap-theme.css"),
            Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap-theme.css.map"),
            Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap-theme.min.css"),
            Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap-theme.min.css.map"),
            Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap.css"),
            Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap.css.map"),
            Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap.min.css"),
            Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap.min.css.map"),
            Path.Combine("wwwroot", "lib", "bootstrap", "dist", "fonts", "glyphicons-halflings-regular.eot"),
            Path.Combine("wwwroot", "lib", "bootstrap", "dist", "fonts", "glyphicons-halflings-regular.svg"),
            Path.Combine("wwwroot", "lib", "bootstrap", "dist", "fonts", "glyphicons-halflings-regular.ttf"),
            Path.Combine("wwwroot", "lib", "bootstrap", "dist", "fonts", "glyphicons-halflings-regular.woff"),
            Path.Combine("wwwroot", "lib", "bootstrap", "dist", "fonts", "glyphicons-halflings-regular.woff2"),
            Path.Combine("wwwroot", "lib", "bootstrap", "dist", "js", "bootstrap.js"),
            Path.Combine("wwwroot", "lib", "bootstrap", "dist", "js", "bootstrap.min.js"),
            Path.Combine("wwwroot", "lib", "bootstrap", "dist", "js", "npm.js"),
            Path.Combine("wwwroot", "lib", "jquery", ".bower.json"),
            Path.Combine("wwwroot", "lib", "jquery", "LICENSE.txt"),
            Path.Combine("wwwroot", "lib", "jquery", "dist", "jquery.js"),
            Path.Combine("wwwroot", "lib", "jquery", "dist", "jquery.min.js"),
            Path.Combine("wwwroot", "lib", "jquery", "dist", "jquery.min.map"),
            Path.Combine("wwwroot", "lib", "jquery-validation", ".bower.json"),
            Path.Combine("wwwroot", "lib", "jquery-validation", "LICENSE.md"),
            Path.Combine("wwwroot", "lib", "jquery-validation", "dist", "additional-methods.js"),
            Path.Combine("wwwroot", "lib", "jquery-validation", "dist", "additional-methods.min.js"),
            Path.Combine("wwwroot", "lib", "jquery-validation", "dist", "jquery.validate.js"),
            Path.Combine("wwwroot", "lib", "jquery-validation", "dist", "jquery.validate.min.js"),
            Path.Combine("wwwroot", "lib", "jquery-validation-unobtrusive", ".bower.json"),
            Path.Combine("wwwroot", "lib", "jquery-validation-unobtrusive", "jquery.validate.unobtrusive.js"),
            Path.Combine("wwwroot", "lib", "jquery-validation-unobtrusive", "jquery.validate.unobtrusive.min.js"),
            Path.Combine("wwwroot", "lib", "jquery-validation-unobtrusive", "LICENSE.txt"),
        });
    }
}
