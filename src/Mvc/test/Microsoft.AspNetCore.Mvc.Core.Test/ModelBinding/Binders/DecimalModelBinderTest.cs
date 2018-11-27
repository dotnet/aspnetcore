// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class DecimalModelBinderTest : FloatingPointTypeModelBinderTest<decimal>
    {
        protected override decimal Twelve => 12M;

        protected override decimal TwelvePointFive => 12.5M;

        protected override decimal ThirtyTwoThousand => 32_000M;

        protected override decimal ThirtyTwoThousandPointOne => 32_000.1M;

        protected override IModelBinder GetBinder(NumberStyles numberStyles)
        {
            return new DecimalModelBinder(numberStyles, NullLoggerFactory.Instance);
        }
    }
}
