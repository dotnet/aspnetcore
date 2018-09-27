// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cli.FunctionalTests.Templates
{
    public class RazorTemplate : RazorBootstrapJQueryTemplate
    {
        public RazorTemplate() { }

        public override string Name => "razor";

        protected override string RazorPath => "Pages";

        public override IEnumerable<string> ExpectedObjFilesAfterBuild =>
            base.ExpectedObjFilesAfterBuild
            .Concat(new[] {
                Path.Combine("Razor", RazorPath, "_ViewStart.g.cshtml.cs"),
                Path.Combine("Razor", RazorPath, "Error.g.cshtml.cs"),
                Path.Combine("Razor", RazorPath, "Index.g.cshtml.cs"),
                Path.Combine("Razor", RazorPath, "Privacy.g.cshtml.cs"),
                Path.Combine("Razor", RazorPath, "Shared", "_CookieConsentPartial.g.cshtml.cs"),
                Path.Combine("Razor", RazorPath, "Shared", "_Layout.g.cshtml.cs"),
                Path.Combine("Razor", RazorPath, "Shared", "_ValidationScriptsPartial.g.cshtml.cs"),
            }.Select(p => Path.Combine(OutputPath, p)));
    }
}
