// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Reflection;

namespace RazorPagesWebSite.Conventions
{
    internal class CustomModelTypeConvention : IPageApplicationModelConvention
    {
        public void Apply(PageApplicationModel model)
        {
            if (model.ModelType == typeof(CustomModelTypeModel))
            {
                model.ModelType = typeof(CustomModelTypeModel<User>).GetTypeInfo();
            }
        }
    }
}
