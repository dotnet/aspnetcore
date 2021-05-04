// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Server.JsonSourceGeneration
{
    internal partial class JsonContext : JsonSerializerContext, WebEventData.IWebEventJsonSerializerContext
    {
    }
}
