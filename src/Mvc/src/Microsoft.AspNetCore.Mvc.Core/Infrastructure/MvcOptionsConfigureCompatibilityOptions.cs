// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    internal class MvcOptionsConfigureCompatibilityOptions : ConfigureCompatibilityOptions<MvcOptions>
    {
        public MvcOptionsConfigureCompatibilityOptions(
            ILoggerFactory loggerFactory,
            IOptions<MvcCompatibilityOptions> compatibilityOptions)
            : base(loggerFactory, compatibilityOptions)
        {
        }

        protected override IReadOnlyDictionary<string, object> DefaultValues
        {
            get
            {
                var values = new Dictionary<string, object>();

                if (Version >= CompatibilityVersion.Version_2_1)
                {
                    values[nameof(MvcOptions.AllowCombiningAuthorizeFilters)] = true;
                    values[nameof(MvcOptions.AllowBindingHeaderValuesToNonStringModelTypes)] = true;
                    values[nameof(MvcOptions.AllowValidatingTopLevelNodes)] = true;
                    values[nameof(MvcOptions.InputFormatterExceptionPolicy)] = InputFormatterExceptionPolicy.MalformedInputExceptions;
                    values[nameof(MvcOptions.SuppressBindingUndefinedValueToEnumType)] = true;
                }

                if (Version >= CompatibilityVersion.Version_2_2)
                {
                    values[nameof(MvcOptions.EnableEndpointRouting)] = true;

                    // Matches JsonSerializerSettingsProvider.DefaultMaxDepth
                    values[nameof(MvcOptions.MaxValidationDepth)] = 32;

                    values[nameof(MvcOptions.AllowShortCircuitingValidationWhenNoValidatorsArePresent)] = true;

                }

                return values;
            }
        }
    }
}
