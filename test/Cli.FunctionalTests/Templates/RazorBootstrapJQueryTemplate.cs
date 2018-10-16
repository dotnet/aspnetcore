// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cli.FunctionalTests.Templates
{
    public abstract class RazorBootstrapJQueryTemplate : RazorApplicationBaseTemplate
    {
        public override IEnumerable<string> ExpectedFilesAfterPublish =>
            base.ExpectedFilesAfterPublish
            .Concat(new[] {
                Path.Combine("wwwroot", "favicon.ico"),
                Path.Combine("wwwroot", "css", "site.css"),
                Path.Combine("wwwroot", "js", "site.js"),
                Path.Combine("wwwroot", "lib", "bootstrap", "LICENSE"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap.css"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap.css.map"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap.min.css"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap.min.css.map"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "js", "bootstrap.js"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "js", "bootstrap.min.js"),
                Path.Combine("wwwroot", "lib", "jquery", "LICENSE.txt"),
                Path.Combine("wwwroot", "lib", "jquery", "dist", "jquery.js"),
                Path.Combine("wwwroot", "lib", "jquery", "dist", "jquery.min.js"),
                Path.Combine("wwwroot", "lib", "jquery", "dist", "jquery.min.map"),
                Path.Combine("wwwroot", "lib", "jquery-validation", "LICENSE.md"),
                Path.Combine("wwwroot", "lib", "jquery-validation", "dist", "additional-methods.js"),
                Path.Combine("wwwroot", "lib", "jquery-validation", "dist", "additional-methods.min.js"),
                Path.Combine("wwwroot", "lib", "jquery-validation", "dist", "jquery.validate.js"),
                Path.Combine("wwwroot", "lib", "jquery-validation", "dist", "jquery.validate.min.js"),
                Path.Combine("wwwroot", "lib", "jquery-validation-unobtrusive", "jquery.validate.unobtrusive.js"),
                Path.Combine("wwwroot", "lib", "jquery-validation-unobtrusive", "jquery.validate.unobtrusive.min.js"),
                Path.Combine("wwwroot", "lib", "jquery-validation-unobtrusive", "LICENSE.txt"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap-grid.css"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap-grid.css.map"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap-grid.min.css"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap-grid.min.css.map"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap-reboot.css"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap-reboot.css.map"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap-reboot.min.css"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap-reboot.min.css.map"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "js", "bootstrap.bundle.js"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "js", "bootstrap.bundle.js.map"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "js", "bootstrap.bundle.min.js"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "js", "bootstrap.bundle.min.js.map"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "js", "bootstrap.js.map"),
                Path.Combine("wwwroot", "lib", "bootstrap", "dist", "js", "bootstrap.min.js.map"),
            });
    }
}
