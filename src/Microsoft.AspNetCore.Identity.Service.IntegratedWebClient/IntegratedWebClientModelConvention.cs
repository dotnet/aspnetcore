// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.Service.IntegratedWebClient
{
    public class IntegratedWebClientModelConvention : IApplicationModelConvention
    {
        private readonly IOptions<IntegratedWebClientOptions> _webClientOptions;

        public IntegratedWebClientModelConvention(IOptions<IntegratedWebClientOptions> webClientOptions)
        {
            _webClientOptions = webClientOptions;
        }

        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                foreach (var action in controller.Actions)
                {
                    Apply(action);
                }
            }
        }

        private void Apply(ActionModel action)
        {
            var parameters = action.Parameters.Where(p => p.Attributes.OfType<EnableIntegratedWebClientAttribute>().Any());
            if (parameters.Any())
            {
                action.Filters.Add(new IntegratedWebClientRedirectFilter(
                    _webClientOptions,
                    parameters.Select(p => p.ParameterName)));
            }
        }
    }
}
