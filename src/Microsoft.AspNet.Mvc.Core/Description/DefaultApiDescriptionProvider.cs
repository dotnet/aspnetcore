// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Description
{
    /// <summary>
    /// Implements a provider of <see cref="ApiDescription"/> for actions represented
    /// by <see cref="ControllerActionDescriptor"/>.
    /// </summary>
    public class DefaultApiDescriptionProvider : INestedProvider<ApiDescriptionProviderContext>
    {
        private readonly IOutputFormattersProvider _formattersProvider;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IInlineConstraintResolver _constraintResolver;

        /// <summary>
        /// Creates a new instance of <see cref="DefaultApiDescriptionProvider"/>.
        /// </summary>
        /// <param name="formattersProvider">The <see cref="IOutputFormattersProvider"/>.</param>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        public DefaultApiDescriptionProvider(
            IOutputFormattersProvider formattersProvider,
            IInlineConstraintResolver constraintResolver,
            IModelMetadataProvider modelMetadataProvider)
        {
            _formattersProvider = formattersProvider;
            _modelMetadataProvider = modelMetadataProvider;
            _constraintResolver = constraintResolver;
        }

        /// <inheritdoc />
        public int Order 
        {
            get { return DefaultOrder.DefaultFrameworkSortOrder; }
        }


        /// <inheritdoc />
        public void Invoke(ApiDescriptionProviderContext context, Action callNext)
        {
            foreach (var action in context.Actions.OfType<ControllerActionDescriptor>())
            {
                var extensionData = action.GetProperty<ApiDescriptionActionData>();
                if (extensionData != null)
                {
                    var httpMethods = GetHttpMethods(action);
                    foreach (var httpMethod in httpMethods)
                    {
                        context.Results.Add(CreateApiDescription(action, httpMethod, extensionData.GroupName));
                    }
                }
            }

            callNext();
        }

        private ApiDescription CreateApiDescription(
            ControllerActionDescriptor action,
            string httpMethod,
            string groupName)
        {
            var parsedTemplate = ParseTemplate(action);

            var apiDescription = new ApiDescription()
            {
                ActionDescriptor = action,
                GroupName = groupName,
                HttpMethod = httpMethod,
                RelativePath = GetRelativePath(parsedTemplate),
            };

            var templateParameters = parsedTemplate?.Parameters?.ToList() ?? new List<TemplatePart>();

            GetParameters(apiDescription, action.Parameters, templateParameters);

            var responseMetadataAttributes = GetResponseMetadataAttributes(action);

            // We only provide response info if we can figure out a type that is a user-data type.
            // Void /Task object/IActionResult will result in no data.
            var declaredReturnType = GetDeclaredReturnType(action);

            // Now 'simulate' an action execution. This attempts to figure out to the best of our knowledge
            // what the logical data type is using filters.
            var runtimeReturnType = GetRuntimeReturnType(declaredReturnType, responseMetadataAttributes);

            // We might not be able to figure out a good runtime return type. If that's the case we don't
            // provide any information about outputs. The workaround is to attribute the action.
            if (runtimeReturnType == typeof(void))
            {
                // As a special case, if the return type is void - we want to surface that information
                // specifically, but nothing else. This can be overridden with a filter/attribute.
                apiDescription.ResponseType = runtimeReturnType;
            }
            else if (runtimeReturnType != null)
            {
                apiDescription.ResponseType = runtimeReturnType;

                apiDescription.ResponseModelMetadata = _modelMetadataProvider.GetMetadataForType(
                    modelAccessor: null,
                    modelType: runtimeReturnType);

                var formats = GetResponseFormats(
                    action,
                    responseMetadataAttributes,
                    declaredReturnType,
                    runtimeReturnType);

                foreach (var format in formats)
                {
                    apiDescription.SupportedResponseFormats.Add(format);
                }
            }

            return apiDescription;
        }

        private void GetParameters(
            ApiDescription apiDescription,
            IList<ParameterDescriptor> parameterDescriptors,
            IList<TemplatePart> templateParameters)
        {
            if (parameterDescriptors != null)
            {
                foreach (var parameter in parameterDescriptors)
                {
                    // Process together parameters that appear on the path template and on the
                    // action descriptor and do not come from the body.
                    TemplatePart templateParameter = null;
                    if (parameter.BinderMetadata as IFormatterBinderMetadata == null)
                    {
                        templateParameter = templateParameters
                            .FirstOrDefault(p => p.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase));

                        if (templateParameter != null)
                        {
                            templateParameters.Remove(templateParameter);
                        }
                    }

                    apiDescription.ParameterDescriptions.Add(GetParameter(parameter, templateParameter));
                }
            }

            if (templateParameters.Count > 0)
            {
                // Process parameters that only appear on the path template if any.
                foreach (var templateParameter in templateParameters)
                {
                    var parameterDescription = GetParameter(parameterDescriptor: null, templateParameter: templateParameter);
                    apiDescription.ParameterDescriptions.Add(parameterDescription);
                }
            }
        }

        private IEnumerable<string> GetHttpMethods(ControllerActionDescriptor action)
        {
            if (action.ActionConstraints != null && action.ActionConstraints.Count > 0)
            {
                return action.ActionConstraints.OfType<HttpMethodConstraint>().SelectMany(c => c.HttpMethods);
            }
            else
            {
                return new string[] { null };
            }
        }

        private RouteTemplate ParseTemplate(ControllerActionDescriptor action)
        {
            if (action.AttributeRouteInfo != null &&
                action.AttributeRouteInfo.Template != null)
            {
                return TemplateParser.Parse(action.AttributeRouteInfo.Template, _constraintResolver);
            }

            return null;
        }

        private string GetRelativePath(RouteTemplate parsedTemplate)
        {
            if (parsedTemplate == null)
            {
                return null;
            }

            var segments = new List<string>();

            foreach (var segment in parsedTemplate.Segments)
            {
                var currentSegment = "";
                foreach (var part in segment.Parts)
                {
                    if (part.IsLiteral)
                    {
                        currentSegment += part.Text;
                    }
                    else if (part.IsParameter)
                    {
                        currentSegment += "{" + part.Name + "}";
                    }
                }

                segments.Add(currentSegment);
            }

            return string.Join("/", segments);
        }

        private ApiParameterDescription GetParameter(
            ParameterDescriptor parameterDescriptor,
            TemplatePart templateParameter)
        {
            // This is a placeholder based on currently available functionality for parameters. See #886.
            ApiParameterDescription parameterDescription = null;

            if (templateParameter != null && parameterDescriptor == null)
            {
                // The parameter is part of the route template but not part of the ActionDescriptor.

                // For now if a parameter is part of the template we will asume its value comes from the path.
                // We will be more accurate when we implement #886.
                parameterDescription = CreateParameterFromTemplate(templateParameter);
            }
            else if (templateParameter != null && parameterDescriptor != null)
            {
                // The parameter is part of the route template and part of the ActionDescriptor.
                parameterDescription = CreateParameterFromTemplateAndParameterDescriptor(
                    templateParameter,
                    parameterDescriptor);
            }
            else if(templateParameter == null && parameterDescriptor != null)
            {
                // The parameter is part of the ActionDescriptor but is not part of the route template.
                parameterDescription = CreateParameterFromParameterDescriptor(parameterDescriptor);
            }
            else
            {
                // We will never call this method with templateParameter == null && parameterDescriptor == null
                Contract.Assert(parameterDescriptor != null);
            }

            if (parameterDescription.Type != null)
            {
                parameterDescription.ModelMetadata = _modelMetadataProvider.GetMetadataForType(
                    modelAccessor: null,
                    modelType: parameterDescription.Type);
            }

            return parameterDescription;
        }

        private static ApiParameterDescription CreateParameterFromParameterDescriptor(ParameterDescriptor parameter)
        {
            var resourceParameter = new ApiParameterDescription
            {
                IsOptional = parameter.IsOptional,
                Name = parameter.Name,
                ParameterDescriptor = parameter,
                Type = parameter.ParameterType,
            };

            if (parameter.BinderMetadata as IFormatterBinderMetadata != null)
            {
                resourceParameter.Source = ApiParameterSource.Body;
            }
            else
            {
                resourceParameter.Source = ApiParameterSource.Query;
            }

            return resourceParameter;
        }

        private static ApiParameterDescription CreateParameterFromTemplateAndParameterDescriptor(
            TemplatePart templateParameter,
            ParameterDescriptor parameter)
        {
            var resourceParameter = new ApiParameterDescription
            {
                Source = ApiParameterSource.Path,
                IsOptional = parameter.IsOptional && IsOptionalParameter(templateParameter),
                Name = parameter.Name,
                ParameterDescriptor = parameter,
                Constraint = templateParameter.InlineConstraint,
                DefaultValue = templateParameter.DefaultValue,
                Type = parameter.ParameterType,
            };

            return resourceParameter;
        }

        private static bool IsOptionalParameter(TemplatePart templateParameter)
        {
            return templateParameter.IsOptional || templateParameter.DefaultValue != null;
        }

        private static ApiParameterDescription CreateParameterFromTemplate(TemplatePart templateParameter)
        {
            return new ApiParameterDescription
            {
                Source = ApiParameterSource.Path,
                IsOptional = IsOptionalParameter(templateParameter),
                Name = templateParameter.Name,
                ParameterDescriptor = null,
                Constraint = templateParameter.InlineConstraint,
                DefaultValue = templateParameter.DefaultValue,
            };
        }

        private IReadOnlyList<ApiResponseFormat> GetResponseFormats(
            ControllerActionDescriptor action,
            IApiResponseMetadataProvider[] responseMetadataAttributes,
            Type declaredType,
            Type runtimeType)
        {
            var results = new List<ApiResponseFormat>();

            // Walk through all 'filter' attributes in order, and allow each one to see or override
            // the results of the previous ones. This is similar to the execution path for content-negotiation.
            var contentTypes = new List<MediaTypeHeaderValue>();
            if (responseMetadataAttributes != null)
            {
                foreach (var metadataAttribute in responseMetadataAttributes)
                {
                    metadataAttribute.SetContentTypes(contentTypes);
                }
            }

            if (contentTypes.Count == 0)
            {
                contentTypes.Add(null);
            }

            var formatters = _formattersProvider.OutputFormatters;
            foreach (var contentType in contentTypes)
            {
                foreach (var formatter in formatters)
                {
                    var supportedTypes = formatter.GetSupportedContentTypes(declaredType, runtimeType, contentType);
                    if (supportedTypes != null)
                    {
                        foreach (var supportedType in supportedTypes)
                        {
                            results.Add(new ApiResponseFormat()
                            {
                                Formatter = formatter,
                                MediaType = supportedType,
                            });
                        }
                    }
                }
            }

            return results;
        }

        private Type GetDeclaredReturnType(ControllerActionDescriptor action)
        {
            var declaredReturnType = action.MethodInfo.ReturnType;
            if (declaredReturnType == typeof(void) ||
                declaredReturnType == typeof(Task))
            {
                return typeof(void);
            }

            // Unwrap the type if it's a Task<T>. The Task (non-generic) case was already handled.
            var unwrappedType = TypeHelper.GetTaskInnerTypeOrNull(declaredReturnType) ?? declaredReturnType;

            // If the method is declared to return IActionResult or a derived class, that information
            // isn't valuable to the formatter.
            if (typeof(IActionResult).IsAssignableFrom(unwrappedType))
            {
                return null;
            }
            else
            {
                return unwrappedType;
            }
        }

        private Type GetRuntimeReturnType(Type declaredReturnType, IApiResponseMetadataProvider[] metadataAttributes)
        {
            // Walk through all of the filter attributes and allow them to set the type. This will execute them
            // in filter-order allowing the desired behavior for overriding.
            if (metadataAttributes != null)
            {
                Type typeSetByAttribute = null;
                foreach (var metadataAttribute in metadataAttributes)
                {
                    if (metadataAttribute.Type != null)
                    {
                        typeSetByAttribute = metadataAttribute.Type;
                    }
                }

                // If one of the filters set a type, then trust it.
                if (typeSetByAttribute != null)
                {
                    return typeSetByAttribute;
                }
            }

            // If we get here, then a filter didn't give us an answer, so we need to figure out if we
            // want to use the declared return type.
            //
            // We've already excluded Task, void, and IActionResult at this point.
            //
            // If the action might return any object, then assume we don't know anything about it.
            if (declaredReturnType == typeof(object))
            {
                return null;
            }

            return declaredReturnType;
        }

        private IApiResponseMetadataProvider[] GetResponseMetadataAttributes(ControllerActionDescriptor action)
        {
            if (action.FilterDescriptors == null)
            {
                return null;
            }

            // This technique for enumerating filters will intentionally ignore any filter that is an IFilterFactory
            // for a filter that implements IApiResponseMetadataProvider.
            //
            // The workaround for that is to implement the metadata interface on the IFilterFactory.
            return action.FilterDescriptors
                .Select(fd => fd.Filter)
                .OfType<IApiResponseMetadataProvider>()
                .ToArray();
        }
    }
}