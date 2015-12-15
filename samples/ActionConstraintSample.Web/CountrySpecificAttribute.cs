// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ActionConstraints;
using Microsoft.AspNet.Mvc.Infrastructure;

namespace ActionConstraintSample.Web
{
    public class CountrySpecificAttribute : RouteConstraintAttribute, IActionConstraint
    {
        private readonly string _countryCode;
        public CountrySpecificAttribute(string countryCode)
            : base("country", countryCode, blockNonAttributedActions: false)
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