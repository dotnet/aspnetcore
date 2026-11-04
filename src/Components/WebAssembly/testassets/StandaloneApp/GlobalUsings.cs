// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Re-add System.Net.Http usings that are removed by eng/targets/CSharp.Common.targets.
// Required by the linked BlazorWasmServiceDefaults template files.
global using System.Net.Http;
global using System.Net.Http.Headers;
