// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Server;

internal class HttpRequestEditContext<TModel> : EditContext<TModel> where TModel : class, new()
{
    public HttpRequestEditContext(IHttpContextAccessor httpContextAccessor)
        : base(new TModel())
    {
        var httpContext = httpContextAccessor.HttpContext!;
        this.EnableDataAnnotationsValidation(httpContext.RequestServices);
        BindModel(httpContext);
    }

    private void BindModel(HttpContext httpContext)
    {
        var model = Model;
        var bindingMessages = new ValidationMessageStore(this);

        // Simplified fake model binding for demo purposes. Obviously a real implementation would not look like this.
        var request = httpContext.Request;
        if (request is not null && request.HasFormContentType)
        {
            foreach (var (name, values) in request.Form)
            {
                if (values.Any() && model.GetType().GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase) is { } propertyInfo)
                {
                    var fieldIdentifier = new FieldIdentifier(model, propertyInfo.Name);
                    NotifyFieldChanged(fieldIdentifier);
                    var parserMethodInfo = typeof(BindConverter)
                        .GetMethod(nameof(BindConverter.TryConvertTo))!
                        .MakeGenericMethod(new[] { propertyInfo.PropertyType });
                    var valueString = values.ToString();
                    var parameters = new object[] { valueString, CultureInfo.CurrentCulture, null! };
                    var success = (bool)parserMethodInfo.Invoke(null, parameters)!;
                    if (success)
                    {
                        propertyInfo.SetValue(model, parameters[2]);
                    }
                    else
                    {
                        bindingMessages.Add(fieldIdentifier,
                            $"The value '{valueString}' is not valid for the field '{propertyInfo.Name}'.");
                    }
                }
            }
        }
    }
}
