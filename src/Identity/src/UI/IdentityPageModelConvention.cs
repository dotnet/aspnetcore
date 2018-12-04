// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Microsoft.AspNetCore.Identity.UI
{
    internal class IdentityPageModelConvention<TUser> : IPageApplicationModelConvention where TUser : class
    {
        public void Apply(PageApplicationModel model)
        {
            var defaultUIAttribute = model.ModelType.GetCustomAttribute<IdentityDefaultUIAttribute>();
            if (defaultUIAttribute == null)
            {
                return;
            }

            ValidateTemplate(defaultUIAttribute.Template);
            var templateInstance = defaultUIAttribute.Template.MakeGenericType(typeof(TUser));
            model.ModelType = templateInstance.GetTypeInfo();
        }

        private void ValidateTemplate(Type template)
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
}
