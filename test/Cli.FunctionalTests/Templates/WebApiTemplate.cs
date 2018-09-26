// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Cli.FunctionalTests.Templates
{
    public class WebApiTemplate : WebTemplate
    {
        public WebApiTemplate() { }

        public override string Name => "webapi";

        public override string RelativeUrl => "/api/values";

        public override IEnumerable<string> ExpectedFilesAfterPublish =>
            base.ExpectedFilesAfterPublish
            .Concat(new[]
            {
                "appsettings.Development.json",
                "appsettings.json",
            });
    }
}
