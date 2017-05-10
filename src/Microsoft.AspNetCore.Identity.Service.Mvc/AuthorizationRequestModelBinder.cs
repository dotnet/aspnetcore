// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Identity.Service.Mvc
{
    public class AuthorizationRequestModelBinder : IModelBinder
    {
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (!bindingContext.IsTopLevelObject)
            {
                return;
            }

            if (bindingContext.ModelType.Equals(typeof(AuthorizationRequest)))
            {
                var httpContext = bindingContext.HttpContext;
                var httpRequest = httpContext.Request;

                IEnumerable<KeyValuePair<string, StringValues>> source = null;
                if (httpRequest.Method.Equals("GET"))
                {
                    source = httpRequest.Query;
                }
                else if (httpRequest.Method.Equals("POST"))
                {
                    source = await httpRequest.ReadFormAsync();
                }

                if (source == null)
                {
                    bindingContext.Result = ModelBindingResult.Failed();
                }
                else
                {
                    var requestParameters = source.ToDictionary(
                        kvp => kvp.Key,
                        kvp => (string[])kvp.Value);

                    var factory = httpContext.RequestServices.GetRequiredService<IAuthorizationRequestFactory>();
                    bindingContext.Result = ModelBindingResult.Success(await factory.CreateAuthorizationRequestAsync(requestParameters));
                }
            }
        }
    }
}
