// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ApiExplorerWebSite
{
    [Route("[controller]/[Action]")]
    public class ApiExplorerResponseTypeWithAttributeController : Controller
    {
        [HttpGet]
        [Produces(typeof(Customer))]
        public void GetVoid()
        {
        }

        [HttpGet]
        [Produces("application/json", Type = typeof(Product))]
        public object GetObject()
        {
            return null;
        }

        [HttpGet]
        [Produces("application/json", Type = typeof(string))]
        public IActionResult GetIActionResult()
        {
            return new EmptyResult();
        }

        [HttpGet]
        [Produces("application/json", Type = typeof(int))]
        public Task GetTask()
        {
            return null;
        }

        [HttpGet]
        [Produces(typeof(Customer))] // It's possible to lie about what type you return
        public Product GetProduct()
        {
            return null;
        }

        [ProducesResponseType(typeof(Product), 201)]
        [ProducesResponseType(typeof(ModelStateDictionary), 400)]
        public Product CreateProductWithDefaultResponseContentTypes(Product product)
        {
            return null;
        }

        [Produces("text/xml")] // Has status code as 200 but is not applied as it does not set 'Type'
        [ProducesResponseType(typeof(Product), 201)]
        [ProducesResponseType(typeof(ModelStateDictionary), 400)]
        public Product CreateProductWithLimitedResponseContentTypes(Product product)
        {
            return null;
        }

        [ProducesResponseType(typeof(Product), 200)]
        [ProducesResponseType(typeof(ModelStateDictionary), 400)]
        public Product UpdateProductWithDefaultResponseContentTypes(Product product)
        {
            return null;
        }

        [Produces("text/xml", Type = typeof(Product))] // Has status code as 200
        [ProducesResponseType(typeof(ModelStateDictionary), 400)]
        public Product UpdateProductWithLimitedResponseContentTypes(Product product)
        {
            return null;
        }
    }
}