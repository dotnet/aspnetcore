// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Microsoft.AspNetCore.Identity.UI;

internal sealed class IdentityPageModelConvention<TUser> : IPageApplicationModelConvention where TUser : class
{
    public void Apply(PageApplicationModel model)
    {
        var defaultUIAttribute = model.ModelType?.GetCustomAttribute<IdentityDefaultUIAttribute>();
        if (defaultUIAttribute == null)
        {
            return;
        }

        ValidateTemplate(defaultUIAttribute.Template);
        var templateInstance = defaultUIAttribute.Template.MakeGenericType(typeof(TUser));
        model.ModelType = templateInstance.GetTypeInfo();
    }

    private static void ValidateTemplate(Type template)
    {
        if (template.IsAbstract || !template.IsGenericTypeDefinition)
        {
            throw new InvalidOperationException("Implementation type can't be abstract or non generic.");
        }
        var genericArguments = template.GetGenericArguments();
        if (genericArguments.Length != 1)
        {
            throw new InvalidOperationException("Implementation type contains wrong generic arity.");
        }
    }
}
