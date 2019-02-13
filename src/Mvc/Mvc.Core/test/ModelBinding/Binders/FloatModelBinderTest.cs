// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
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
}
