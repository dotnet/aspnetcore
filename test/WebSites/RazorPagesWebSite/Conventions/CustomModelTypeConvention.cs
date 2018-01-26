// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
