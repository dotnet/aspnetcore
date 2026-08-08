// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Forms;

namespace Microsoft.AspNetCore.Components.Web.Internal;

// Contracts for the state the framework persists on an application's behalf but cannot name from the
// assembly that owns the persistence options. Without them a native application silently loses the
// antiforgery token across the prerender boundary, because state whose contract cannot be resolved is
// skipped rather than persisted.
[JsonSerializable(typeof(AntiforgeryRequestToken))]
internal sealed partial class WebPersistentStateJsonContext : JsonSerializerContext;
