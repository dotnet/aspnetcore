// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    internal static class RequestParametersHelper
    {
        internal static (string value, OpenIdConnectMessage error) ValidateOptionalParameterIsUnique(
            IDictionary<string, string[]> requestParameters,
            string parameterName, ProtocolErrorProvider provider)
        {
            if (requestParameters.TryGetValue(parameterName, out var currentParameter))
            {
                if (currentParameter.Count() != 1)
                {
                    return (null, provider.TooManyParameters(parameterName));
                }

                return (currentParameter.Single(),null);
            }

            return (null, null);
        }

        internal static (string value, OpenIdConnectMessage error) ValidateParameterIsUnique(
            IDictionary<string, string[]> requestParameters,
            string parameterName,
            ProtocolErrorProvider provider)
        {
            if (requestParameters.TryGetValue(parameterName, out var currentParameter))
            {
                if (currentParameter.Length > 1)
                {
                    return (null, provider.TooManyParameters(parameterName));
                }

                var parameterValue = currentParameter.SingleOrDefault();
                if (string.IsNullOrEmpty(parameterValue))
                {
                    return (null, provider.MissingRequiredParameter(parameterName));
                }

                return (parameterValue,null);
            }
            else
            {
                return (null, provider.MissingRequiredParameter(parameterName));
            }
        }
    }
}
