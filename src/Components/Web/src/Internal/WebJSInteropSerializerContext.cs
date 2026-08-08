// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.Web.Internal;

// Contracts for the types the JS interop plumbing itself round-trips, independent of any
// particular call: the void result every 'InvokeVoidAsync' completes with, and the primitives that
// identifiers, element references and simple arguments are made of. Application types are not
// covered here — an application that exchanges its own types supplies their contracts.
//
// The primitive set is deliberately complete rather than limited to what the framework's own
// interop happens to use today. Every one of these can appear as a '[JSInvokable]' parameter or an
// 'InvokeAsync<T>' result on a framework component, and a missing one surfaces only as a runtime
// failure inside a JS callback, where it is very hard to attribute.
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
// Framework types that cross the interop boundary by value, rather than through a converter.
[JsonSerializable(typeof(NavigationOptions))]
[JsonSerializable(typeof(BrowserFile))]
[JsonSerializable(typeof(BrowserFile[]))]
internal sealed partial class WebJSInteropSerializerContext : JsonSerializerContext;
