// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.Mvc;

namespace BasicWebSite
{
    public class OrderController : Controller
    {
        public int GetServiceOrder(string serviceType, string actualType)
        {
            var elementType = Type.GetType(serviceType);

            var queryType = typeof(IEnumerable<>).MakeGenericType(elementType);

            var services = (IEnumerable<object>)Resolver.GetService(queryType);
            foreach (var service in services)
            {
                if (actualType != null && service.GetType().AssemblyQualifiedName == actualType)
                {
                    var orderProperty = elementType.GetTypeInfo().GetDeclaredProperty("Order");
                    return (int)orderProperty.GetValue(service);
                }
                else if (actualType == null)
                {
                    var orderProperty = elementType.GetProperty("Order");
                    return (int)orderProperty.GetValue(service);
                }
            }

            throw new InvalidOperationException();
        }
    }
}