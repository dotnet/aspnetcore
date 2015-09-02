// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class TestMvcOptions : IOptions<MvcOptions>
    {
        public TestMvcOptions()
        {
            Value = new MvcOptions();
            MvcCoreMvcOptionsSetup.ConfigureMvc(Value);
            MvcDataAnnotationsMvcOptionsSetup.ConfigureMvc(Value);
            MvcJsonMvcOptionsSetup.ConfigureMvc(Value, SerializerSettingsProvider.CreateSerializerSettings());
        }

        public MvcOptions Value { get; }
    }
}