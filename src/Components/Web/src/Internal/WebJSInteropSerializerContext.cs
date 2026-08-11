// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
#if !COMPONENTS_WEBASSEMBLY
using Microsoft.AspNetCore.Components.Forms;
#endif
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.Web.Internal;

[JsonSerializable(typeof(IJSVoidResult))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(byte))]
[JsonSerializable(typeof(sbyte))]
[JsonSerializable(typeof(short))]
[JsonSerializable(typeof(ushort))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(uint))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(ulong))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(char))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(TimeSpan))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(object[]))]
[JsonSerializable(typeof(byte[]))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(NavigationOptions))]
#if !COMPONENTS_WEBASSEMBLY
[JsonSerializable(typeof(BrowserFile))]
[JsonSerializable(typeof(BrowserFile[]))]
#endif
internal sealed partial class WebJSInteropSerializerContext : JsonSerializerContext;
