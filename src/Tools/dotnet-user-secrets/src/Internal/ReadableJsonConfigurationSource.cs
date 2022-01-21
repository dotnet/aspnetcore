// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration.Json;

namespace Microsoft.Extensions.SecretManager.Tools.Internal;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
public class ReadableJsonConfigurationProvider : JsonConfigurationProvider
{
    public ReadableJsonConfigurationProvider()
        : base(new JsonConfigurationSource())
    {
    }

    public IDictionary<string, string> CurrentData => Data;
}
