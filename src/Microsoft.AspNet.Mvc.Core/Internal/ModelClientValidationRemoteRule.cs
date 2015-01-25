// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Web.Mvc
{
    [TypeForwardedFrom("System.Web.Mvc, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public class ModelClientValidationRemoteRule : ModelClientValidationRule
    {
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "The value is a not a regular URL since it may contain ~/ ASP.NET-specific characters")]
        public ModelClientValidationRemoteRule(string errorMessage, string url, string httpMethod, string additionalFields)
        {
            ErrorMessage = errorMessage;
            ValidationType = "remote";
            ValidationParameters["url"] = url;

            if (!String.IsNullOrEmpty(httpMethod))
            {
                ValidationParameters["type"] = httpMethod;
            }

            ValidationParameters["additionalfields"] = additionalFields;
        }
    }
}
