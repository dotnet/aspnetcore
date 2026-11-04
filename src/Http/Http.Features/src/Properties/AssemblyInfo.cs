// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http.Features;

[assembly: TypeForwardedTo(typeof(IFeatureCollection))]
[assembly: TypeForwardedTo(typeof(FeatureCollection))]
[assembly: TypeForwardedTo(typeof(FeatureReference<>))]
[assembly: TypeForwardedTo(typeof(FeatureReferences<>))]
