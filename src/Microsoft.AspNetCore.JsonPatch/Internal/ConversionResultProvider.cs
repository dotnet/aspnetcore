// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public static class ConversionResultProvider
    {
        public static ConversionResult ConvertTo(object value, Type typeToConvertTo)
        {
            try
            {
                var deserialized = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(value), typeToConvertTo);
                return new ConversionResult(true, deserialized);
            }
            catch
            {
                return new ConversionResult(canBeConverted: false, convertedInstance: null);
            }
        }
    }
}
