// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ApiExplorer;

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
            throw new NotImplementedException();
        }

        private ApiExplorerData CreateSerializableData(ApiDescription description)
        {
            var data = new ApiExplorerData()
            {
                GroupName = description.GroupName,
                HttpMethod = description.HttpMethod,
                RelativePath = description.RelativePath,
                ResponseType = description.ResponseType?.FullName,
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

            foreach (var response in description.SupportedResponseFormats)
            {
                var responseData = new ApiExplorerResponseData()
                {
                    FormatterType = response.Formatter.GetType().FullName,
                    MediaType = response.MediaType.ToString(),
                };

                data.SupportedResponseFormats.Add(responseData);
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

            public string ResponseType { get; set; }

            public List<ApiExplorerResponseData> SupportedResponseFormats { get; } = new List<ApiExplorerResponseData>();
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
        private class ApiExplorerResponseData
        {
            public string MediaType { get; set; }

            public string FormatterType { get; set; }
        }
    }
}