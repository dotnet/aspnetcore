// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace FormatterWebSite
{
    public class ValidateBodyParameterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var bodyParameter = context.ActionDescriptor
                                          .Parameters
                                          .FirstOrDefault(parameter => parameter.BinderMetadata is IFormatterBinderMetadata);
                if (bodyParameter != null)
                {
                    var parameterBindingErrors = context.ModelState[bodyParameter.Name].Errors;
                    if (parameterBindingErrors.Count != 0)
                    {
                        var errorInfo = new ErrorInfo
                        {
                            ActionName = context.ActionDescriptor.Name,
                            ParameterName = bodyParameter.Name,
                            Errors = parameterBindingErrors.Select(x => x.ErrorMessage).ToList(),
                            Source = "filter"
                        };

                        context.Result = new ObjectResult(errorInfo);
                    }
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
