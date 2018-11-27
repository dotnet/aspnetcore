// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ControllersFromServicesWebSite.Components
{
    public class ComponentFromServicesViewComponent : ViewComponent
    {
        private readonly ValueService _value;

        public ComponentFromServicesViewComponent(ValueService value)
        {
            _value = value;
        }

        public string Invoke()
        {
            return $"Value = {_value.Value}";
        }
    }
}
