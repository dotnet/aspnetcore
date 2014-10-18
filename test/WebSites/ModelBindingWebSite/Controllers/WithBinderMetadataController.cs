// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ModelBindingWebSite.Controllers
{
    public class WithMetadataController : Controller
    {
        public EmployeeWithMetadata BindWithTypeMetadata(EmployeeWithMetadata emp)
        {
            return emp;
        }

        public DerivedEmployee TypeMetadataAtDerivedTypeWinsOverTheBaseType(DerivedEmployee emp)
        {
            return emp;
        }

        public void ParameterMetadataOverridesTypeMetadata([FromBody] Employee emp)
        {
        }

        public Employee ParametersWithNoValueProviderMetadataUseTheAvailableValueProviders([FromQuery] Employee emp)
        {
            return emp;
        }
    }
}