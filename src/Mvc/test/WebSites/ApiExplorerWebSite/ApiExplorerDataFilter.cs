// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ApiExplorerWebSite
{
    /// <summary>
    /// A resource filter that looks up and serializes Api Explorer data for the action.
    ///
    /// This replaces the 'actual' output of the action.
    /// </summary>
    public class ApiExplorerDataFilter : IResourceFilter
    {
        private readonly IApiDescriptionGroupCollectionProvider _descriptionProvider;

        public ApiExplorerDataFilter(IApiDescriptionGroupCollectionProvider descriptionProvider)
        {
            _descriptionProvider = descriptionProvider;
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor != null && controllerActionDescriptor.MethodInfo.IsDefined(typeof(PassThruAttribute)))
            {
                return;
            }

            var descriptions = new List<ApiExplorerData>();
            foreach (var group in _descriptionProvider.ApiDescriptionGroups.Items)
            {
                foreach (var description in group.Items)
                {
                    if (context.ActionDescriptor == description.ActionDescriptor)
                    {
                        descriptions.Add(CreateSerializableData(description));
                    }
                }
            }

            context.Result = new JsonResult(descriptions);
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }

        private ApiExplorerData CreateSerializableData(ApiDescription description)
        {
            var data = new ApiExplorerData()
            {
                GroupName = description.GroupName,
                HttpMethod = description.HttpMethod,
                RelativePath = description.RelativePath
            };

            foreach (var parameter in description.ParameterDescriptions)
            {
                var parameterData = new ApiExplorerParameterData()
                {
                    Name = parameter.Name,
                    Source = parameter.Source.Id,
                    Type = parameter.Type?.FullName,
                };

                if (parameter.RouteInfo != null)
                {
                    parameterData.RouteInfo = new ApiExplorerParameterRouteInfo()
                    {
                        ConstraintTypes = parameter.RouteInfo.Constraints?.Select(c => c.GetType().Name).ToArray(),
                        DefaultValue = parameter.RouteInfo.DefaultValue,
                        IsOptional = parameter.RouteInfo.IsOptional,
                    };
                }

                data.ParameterDescriptions.Add(parameterData);
            }

            foreach (var request in description.SupportedRequestFormats)
            {
                data.SupportedRequestFormats.Add(new ApiExplorerRequestFormat
                {
                    FormatterType = request.Formatter?.GetType().FullName,
                    MediaType = request.MediaType,
                });
            }

            foreach (var response in description.SupportedResponseTypes)
            {
                var responseType = new ApiExplorerResponseType()
                {
                    StatusCode = response.StatusCode,
                    ResponseType = response.Type?.FullName,
                    IsDefaultResponse = response.IsDefaultResponse,
                };

                foreach(var responseFormat in response.ApiResponseFormats)
                {
                    responseType.ResponseFormats.Add(new ApiExplorerResponseFormat()
                    {
                        FormatterType = responseFormat.Formatter?.GetType().FullName,
                        MediaType = responseFormat.MediaType
                    });
                }

                data.SupportedResponseTypes.Add(responseType);
            }

            return data;
        }

        // Used to serialize data between client and server
        private class ApiExplorerData
        {
            public string GroupName { get; set; }

            public string HttpMethod { get; set; }

            public List<ApiExplorerParameterData> ParameterDescriptions { get; } = new List<ApiExplorerParameterData>();

            public string RelativePath { get; set; }

            public List<ApiExplorerResponseType> SupportedResponseTypes { get; } = new List<ApiExplorerResponseType>();

            public List<ApiExplorerRequestFormat> SupportedRequestFormats { get; } = new List<ApiExplorerRequestFormat>();
        }

        // Used to serialize data between client and server
        private class ApiExplorerParameterData
        {
            public string Name { get; set; }

            public ApiExplorerParameterRouteInfo RouteInfo { get; set; }

            public string Source { get; set; }

            public string Type { get; set; }
        }

        // Used to serialize data between client and server
        private class ApiExplorerParameterRouteInfo
        {
            public string[] ConstraintTypes { get; set; }

            public object DefaultValue { get; set; }

            public bool IsOptional { get; set; }
        }

        // Used to serialize data between client and server
        private class ApiExplorerResponseType
        {
            public IList<ApiExplorerResponseFormat> ResponseFormats { get; }
                = new List<ApiExplorerResponseFormat>();

            public string ResponseType { get; set; }

            public int StatusCode { get; set; }

            public bool IsDefaultResponse { get; set; }
        }

        private class ApiExplorerResponseFormat
        {
            public string MediaType { get; set; }

            public string FormatterType { get; set; }
        }

        private class ApiExplorerRequestFormat
        {
            public string MediaType { get; set; }

            public string FormatterType { get; set; }
        }
    }
}