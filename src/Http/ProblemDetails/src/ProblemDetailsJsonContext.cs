// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Http;

[JsonSerializable(typeof(ProblemDetails))]
internal sealed partial class ProblemDetailsJsonContext : JsonSerializerContext
{ }
