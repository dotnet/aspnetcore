// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

public class TagBuilderBenchmark
{
    [Benchmark]
    public string ValidFieldName() => TagBuilder.CreateSanitizedId("LongIdForFieldWithMaxLength", "z");

    [Benchmark]
    public string InvalidFieldName() => TagBuilder.CreateSanitizedId("LongIdForField$WithMaxLength", "z");
}
