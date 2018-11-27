// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ApiExplorerWebSite.Controllers
{
    [Route("ApiExplorerParameters/[action]")]
    public class ApiExplorerParametersController : Controller
    {
        public void SimpleParameters(int i, string s)
        {
        }

        public void SimpleParametersWithBinderMetadata([FromQuery] int i, [FromRoute] string s)
        {
        }

        public void SimpleModel(Product product)
        {
        }

        [Route("{id}")]
        public void SimpleModelFromBody(int id, [FromBody] Product product)
        {
        }

        public void ComplexModel([FromQuery] OrderDTO order)
        {
        }

        public void DefaultValueParameters(string searchTerm, int top = 10, DayOfWeek searchDay = DayOfWeek.Wednesday)
        {
        }

        public void IsRequiredParameters([BindRequired] string requiredParam, string notRequiredParam, Product product)
        {
        }
    }
}