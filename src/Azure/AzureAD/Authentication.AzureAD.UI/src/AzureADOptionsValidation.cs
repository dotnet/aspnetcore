// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.AzureAD.UI
{
    internal class AzureADOptionsValidation : IValidateOptions<AzureADOptions>
    {
        public ValidateOptionsResult Validate(string name, AzureADOptions options)
        {
            if (string.IsNullOrEmpty(options.Instance))
            {
                return ValidateOptionsResult.Fail($"The '{nameof(AzureADOptions.Instance)}' option must be provided.");
            }

            return ValidateOptionsResult.Success;
        }
    }
}
