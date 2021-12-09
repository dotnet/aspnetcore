// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class FloatModelBinderTest : FloatingPointTypeModelBinderTest<float>
{
    protected override float Twelve => 12.0F;

    protected override float TwelvePointFive => 12.5F;

    protected override float ThirtyTwoThousand => 32_000.0F;

    protected override float ThirtyTwoThousandPointOne => 32_000.1F;

    protected override IModelBinder GetBinder(NumberStyles numberStyles)
    {
        return new FloatModelBinder(numberStyles, NullLoggerFactory.Instance);
    }
}
