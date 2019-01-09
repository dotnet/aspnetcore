// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure
{
    internal class DefaultTempDataSerializer : TempDataSerializer
    {
        public override IDictionary<string, object> Deserialize(byte[] unprotectedData)
        {
            throw new InvalidOperationException(Core.Resources.FormatReferenceToNewtonsoftJsonRequired(
                Resources.DeserializingTempData,
                "Microsoft.AspNetCore.Mvc.NewtonsoftJson",
                nameof(IMvcBuilder),
                "AddNewtonsoftJson",
                "ConfigureServices(...)"));
        }

        public override byte[] Serialize(IDictionary<string, object> values)
        {
            throw new InvalidOperationException(Core.Resources.FormatReferenceToNewtonsoftJsonRequired(
                Resources.SerializingTempData,
                "Microsoft.AspNetCore.Mvc.NewtonsoftJson",
                nameof(IMvcBuilder),
                "AddNewtonsoftJson",
                "ConfigureServices(...)"));
        }
    }
}
