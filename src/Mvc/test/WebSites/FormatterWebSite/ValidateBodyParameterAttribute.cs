// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FormatterWebSite;

public class ValidateBodyParameterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var bodyParameter = context.ActionDescriptor
                                      .Parameters
                                      .FirstOrDefault(parameter => IsBodyBindingSource(
                                          parameter.BindingInfo?.BindingSource));
            if (bodyParameter != null)
            {
                // Body model binder normally reports errors for parameters using the empty name.
                var parameterBindingErrors = context.ModelState[bodyParameter.Name]?.Errors ??
                    context.ModelState[string.Empty]?.Errors;
                if (parameterBindingErrors != null && parameterBindingErrors.Count != 0)
                {
                    var errorInfo = new ErrorInfo
                    {
                        ActionName = ((ControllerActionDescriptor)context.ActionDescriptor).ActionName,
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

    private bool IsBodyBindingSource(BindingSource bindingSource)
    {
        return bindingSource?.CanAcceptDataFrom(BindingSource.Body) ?? false;
    }
}
