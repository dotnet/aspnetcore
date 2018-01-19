// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace BasicWebSite
{
    public class VndErrorDescriptionProvider : IErrorDescriptorProvider
    {
        public int Order => 0;

        public void OnProvidersExecuting(ErrorDescriptionContext context)
        {
            if (context.ActionDescriptor.FilterDescriptors.Any(f => f.Filter is VndErrorAttribute) &&
                context.Result is ModelStateDictionary dictionary)
            {
                var vndErrors = new List<VndError>();
                foreach (var item in dictionary)
                {
                    foreach (var modelError in item.Value.Errors)
                    {
                        vndErrors.Add(new VndError
                        {
                            LogRef = modelError.ErrorMessage,
                            Path = item.Key,
                            Message = modelError.ErrorMessage,
                        });
                    }
                }

                context.Result = vndErrors;
            }
        }
    }
}
