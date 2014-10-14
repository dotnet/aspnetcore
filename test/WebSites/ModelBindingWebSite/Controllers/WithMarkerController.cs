// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace ModelBindingWebSite.Controllers
{
    public class WithMarkerController : Controller
    {
        public EmployeeWithMarker BindWithTypeMarker(EmployeeWithMarker emp)
        {
            return emp;
        }

        public DerivedEmployee TypeMarkerAtDerivedTypeWinsOverTheBaseType(DerivedEmployee emp)
        {
            return emp;
        }

        public void ParameterMarkerOverridesTypeMarker([FromBody] Employee emp)
        {
        }

        public Employee ParametersWithNoMarkersUseTheAvailableValueProviders([FromQuery] Employee emp)
        {
            return emp;
        }
    }
}