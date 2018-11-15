// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration.Json;

namespace Microsoft.Extensions.SecretManager.Tools.Internal
{
    public class ReadableJsonConfigurationProvider : JsonConfigurationProvider
    {
        public ReadableJsonConfigurationProvider()
            : base(new JsonConfigurationSource())
        {
        }

        public IDictionary<string, string> CurrentData => Data;
    }
}