// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class DoubleModelBinderTest : FloatingPointTypeModelBinderTest<double>
{
    protected override double Twelve => 12.0;

    protected override double TwelvePointFive => 12.5;

    protected override double ThirtyTwoThousand => 32_000.0;

    protected override double ThirtyTwoThousandPointOne => 32_000.1;

    protected override IModelBinder GetBinder(NumberStyles numberStyles)
    {
        return new DoubleModelBinder(numberStyles, NullLoggerFactory.Instance);
    }
}
