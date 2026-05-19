// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

public class HelperPerformanceBenchmark : RuntimePerformanceBenchmarkBase
{
    public HelperPerformanceBenchmark() : base(
        "~/Views/HelperTyped.cshtml",
        "~/Views/HelperDynamic.cshtml",
        "~/Views/HelperPartialSync.cshtml",
        "~/Views/HelperPartialAsync.cshtml",
        "~/Views/HelperExtensions.cshtml",
        "~/Views/HelperPartialTagHelper.cshtml")
    {
    }

    protected override object Model => Random.Shared.Next().ToString(CultureInfo.InvariantCulture);
}
