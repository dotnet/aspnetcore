// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.AzureAD.UI
{
    [Obsolete("This is obsolete and will be removed in a future version. Use Microsoft.Identity.Web instead. See https://aka.ms/ms-identity-web.")]
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
