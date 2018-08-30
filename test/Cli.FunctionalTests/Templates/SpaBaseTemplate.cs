// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cli.FunctionalTests.Templates
{
    public abstract class SpaBaseTemplate : RazorApplicationBaseTemplate
    {
        protected override string RazorPath => "Pages";

        public override IEnumerable<string> ExpectedObjFilesAfterBuild =>
            base.ExpectedObjFilesAfterBuild
            .Concat(new[]
            {
                Path.Combine("Razor", RazorPath, "Error.g.cshtml.cs"),
            }.Select(p => Path.Combine(OutputPath, p)));
    }
}
