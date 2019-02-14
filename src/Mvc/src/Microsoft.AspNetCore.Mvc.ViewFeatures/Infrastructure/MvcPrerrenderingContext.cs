// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.Environment;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal class MvcPrerrenderingContext
    {
        public MvcPrerrenderingContext(
            ComponentEnvironment environment,
            HtmlEncoder encoder)
        {
            // It's safe to initialize the environment here as this is a scoped service, equivalent to ComponentEnvironment
            InitializeEnvironment(environment);
            Environment = environment;
            Encoder = encoder;
        }

        private void InitializeEnvironment(ComponentEnvironment environment)
        {
            environment.Name = ComponentEnvironment.Prerrender;
        }

        public ComponentEnvironment Environment { get; }

        public HtmlEncoder Encoder { get; }
    }
}
