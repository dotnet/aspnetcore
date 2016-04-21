// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace ActionConstraintSample.Web
{
    public class CountrySpecificAttribute : Attribute, IActionConstraint
    {
        private readonly string _countryCode;

        public CountrySpecificAttribute(string countryCode)
        {
            _countryCode = countryCode;
        }

        public int Order
        {
            get
            {
                return 0;
            }
        }

        public bool Accept(ActionConstraintContext context)
        {
            return string.Equals(
                context.RouteContext.RouteData.Values["country"].ToString(),
                _countryCode,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}