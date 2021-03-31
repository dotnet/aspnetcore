// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Build",
    "xUnit1013:Public method 'Quirks_CatchAllParameter' on test class 'FullFeaturedMatcherConformanceTest' should be marked as a Theory.",
    Justification = "This is a bug in the xUnit analyzer. This method is already marked as a theory.",
    Scope = "member",
    Target = "~M:Microsoft.AspNetCore.Routing.Matching.FullFeaturedMatcherConformanceTest.Quirks_CatchAllParameter(System.String,System.String,System.String[],System.String[])~System.Threading.Tasks.Task")]
