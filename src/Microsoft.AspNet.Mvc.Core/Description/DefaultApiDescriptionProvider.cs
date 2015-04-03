// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.Description
{
    /// <summary>
    /// Implements a provider of <see cref="ApiDescription"/> for actions represented
    /// by <see cref="ControllerActionDescriptor"/>.
    /// </summary>
    public class DefaultApiDescriptionProvider : IApiDescriptionProvider
    {
        private readonly IList<IOutputFormatter> _outputFormatters;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IInlineConstraintResolver _constraintResolver;

        /// <summary>
        /// Creates a new instance of <see cref="DefaultApiDescriptionProvider"/>.
        /// </summary>
        /// <param name="optionsAccessor">The accessor for <see cref="MvcOptions"/>.</param>
        /// <param name="constraintResolver">The <see cref="IInlineConstraintResolver"/> used for resolving inline
        /// constraints.</param>
        /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        public DefaultApiDescriptionProvider(
            IOptions<MvcOptions> optionsAccessor,
            IInlineConstraintResolver constraintResolver,
            IModelMetadataProvider modelMetadataProvider)
        {
            _outputFormatters = optionsAccessor.Options.OutputFormatters;
            _constraintResolver = constraintResolver;
            _modelMetadataProvider = modelMetadataProvider;
        }

        /// <inheritdoc />
        public int Order
        {
            get { return DefaultOrder.DefaultFrameworkSortOrder; }
        }

        /// <inheritdoc />
        public void OnProvidersExecuting([NotNull] ApiDescriptionProviderContext context)
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
        }

        public void OnProvidersExecuted([NotNull] ApiDescriptionProviderContext context)
        {
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

            var parameterContext = new ApiParameterContext(_modelMetadataProvider, action, templateParameters);

            foreach (var parameter in GetParameters(parameterContext))
            {
                apiDescription.ParameterDescriptions.Add(parameter);
            }

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

                apiDescription.ResponseModelMetadata = _modelMetadataProvider.GetMetadataForType(runtimeReturnType);

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
        private IList<ApiParameterDescription> GetParameters(ApiParameterContext context)
        {
            // First, get parameters from the model-binding/parameter-binding side of the world.
            if (context.ActionDescriptor.Parameters != null)
            {
                foreach (var actionParameter in context.ActionDescriptor.Parameters)
                {
                    var visitor = new PseudoModelBindingVisitor(context, actionParameter);
                    var metadata = _modelMetadataProvider.GetMetadataForType(actionParameter.ParameterType);

                    var bindingContext = ApiParameterDescriptionContext.GetContext(
                        metadata,
                        actionParameter.BindingInfo,
                        propertyName: actionParameter.Name);
                    visitor.WalkParameter(bindingContext);
                }
            }

            if (context.ActionDescriptor.BoundProperties != null)
            {
                foreach (var actionParameter in context.ActionDescriptor.BoundProperties)
                {
                    var visitor = new PseudoModelBindingVisitor(context, actionParameter);
                    var modelMetadata = context.MetadataProvider.GetMetadataForProperty(
                        containerType: context.ActionDescriptor.ControllerTypeInfo.AsType(),
                        propertyName: actionParameter.Name);

                    var bindingContext = ApiParameterDescriptionContext.GetContext(
                        modelMetadata,
                        actionParameter.BindingInfo,
                        propertyName: actionParameter.Name);

                    visitor.WalkParameter(bindingContext);
                }
            }

            for (var i = context.Results.Count - 1; i >= 0; i--)
            {
                // Remove any 'hidden' parameters. These are things that can't come from user input,
                // so they aren't worth showing.
                if (!context.Results[i].Source.IsFromRequest)
                {
                    context.Results.RemoveAt(i);
                }
            }

            // Next, we want to join up any route parameters with those discovered from the action's parameters.
            var routeParameters = new Dictionary<string, ApiParameterRouteInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var routeParameter in context.RouteParameters)
            {
                routeParameters.Add(routeParameter.Name, CreateRouteInfo(routeParameter));
            }

            foreach (var parameter in context.Results)
            {
                if (parameter.Source == BindingSource.Path ||
                    parameter.Source == BindingSource.ModelBinding ||
                    parameter.Source == BindingSource.Custom)
                {
                    ApiParameterRouteInfo routeInfo;
                    if (routeParameters.TryGetValue(parameter.Name, out routeInfo))
                    {
                        parameter.RouteInfo = routeInfo;
                        routeParameters.Remove(parameter.Name);

                        if (parameter.Source == BindingSource.ModelBinding &&
                            !parameter.RouteInfo.IsOptional)
                        {
                            // If we didn't see any information about the parameter, but we have
                            // a route parameter that matches, let's switch it to path.
                            parameter.Source = BindingSource.Path;
                        }
                    }
                }
            }

            // Lastly, create a parameter representation for each route parameter that did not find
            // a partner.
            foreach (var routeParameter in routeParameters)
            {
                context.Results.Add(new ApiParameterDescription()
                {
                    Name = routeParameter.Key,
                    RouteInfo = routeParameter.Value,
                    Source = BindingSource.Path,
                });
            }

            return context.Results;
        }

        private ApiParameterRouteInfo CreateRouteInfo(TemplatePart routeParameter)
        {
            var constraints = new List<IRouteConstraint>();
            if (routeParameter.InlineConstraints != null)
            {
                foreach (var constraint in routeParameter.InlineConstraints)
                {
                    constraints.Add(_constraintResolver.ResolveConstraint(constraint.Constraint));
                }
            }

            return new ApiParameterRouteInfo()
            {
                Constraints = constraints,
                DefaultValue = routeParameter.DefaultValue,
                IsOptional = routeParameter.IsOptional || routeParameter.DefaultValue != null,
            };
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
                return TemplateParser.Parse(action.AttributeRouteInfo.Template);
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

            foreach (var contentType in contentTypes)
            {
                foreach (var formatter in _outputFormatters)
                {
                    var responseFormatMetadataProvider = formatter as IApiResponseFormatMetadataProvider;
                    if (responseFormatMetadataProvider != null)
                    {
                        var supportedTypes = responseFormatMetadataProvider.GetSupportedContentTypes(
                            declaredType, 
                            runtimeType, 
                            contentType);

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
            // while searching for a filter that implements IApiResponseMetadataProvider.
            //
            // The workaround for that is to implement the metadata interface on the IFilterFactory.
            return action.FilterDescriptors
                .Select(fd => fd.Filter)
                .OfType<IApiResponseMetadataProvider>()
                .ToArray();
        }

        private class ApiParameterContext
        {
            public ApiParameterContext(
                IModelMetadataProvider metadataProvider,
                ControllerActionDescriptor actionDescriptor,
                IReadOnlyList<TemplatePart> routeParameters)
            {
                MetadataProvider = metadataProvider;
                ActionDescriptor = actionDescriptor;
                RouteParameters = routeParameters;

                Results = new List<ApiParameterDescription>();
            }

            public ControllerActionDescriptor ActionDescriptor { get; }

            public IModelMetadataProvider MetadataProvider { get; }

            public IList<ApiParameterDescription> Results { get; }

            public IReadOnlyList<TemplatePart> RouteParameters { get; }
        }

        private class ApiParameterDescriptionContext
        {
            public ModelMetadata ModelMetadata { get; set; }

            public string BinderModelName { get; set; }

            public BindingSource BindingSource { get; set; }

            public string PropertyName { get; set; }

            public static ApiParameterDescriptionContext GetContext(
                ModelMetadata metadata,
                BindingInfo bindingInfo,
                string propertyName)
            {
                // BindingMetadata can be null if the metadata represents properties.
                return new ApiParameterDescriptionContext
                {
                    ModelMetadata = metadata,
                    BinderModelName = bindingInfo?.BinderModelName ?? metadata.BinderModelName,
                    BindingSource = bindingInfo?.BindingSource ?? metadata.BindingSource,
                    PropertyName = propertyName ?? metadata.PropertyName
                };
            }
        }

        private class PseudoModelBindingVisitor
        {
            public PseudoModelBindingVisitor(ApiParameterContext context, ParameterDescriptor parameter)
            {
                Context = context;
                Parameter = parameter;

                Visited = new HashSet<PropertyKey>();
            }

            public ApiParameterContext Context { get; }

            public ParameterDescriptor Parameter { get; }

            // Avoid infinite recursion by tracking properties. 
            private HashSet<PropertyKey> Visited { get; }

            public void WalkParameter(ApiParameterDescriptionContext context)
            {
                // Attempt to find a binding source for the parameter
                //
                // The default is ModelBinding (aka all default value providers)
                var source = BindingSource.ModelBinding;
                if (!Visit(context, source, containerName: string.Empty))
                {
                    // If we get here, then it means we didn't find a match for any of the model. This means that it's
                    // likely 'model-bound' in the traditional MVC sense (formdata + query string + route data) and
                    // doesn't use any IBinderMetadata.
                    // 
                    // Add a single 'default' parameter description for the model.
                    Context.Results.Add(CreateResult(context, source, containerName: string.Empty));
                }
            }

            /// <summary>
            /// Visits a node in a model, and attempts to create <see cref="ApiParameterDescription"/> for any
            /// model properties where we can definitely compute an answer. 
            /// </summary>
            /// <param name="modelMetadata">The metadata for the model.</param>
            /// <param name="ambientSource">The <see cref="BindingSource"/> from the ambient context.</param>
            /// <param name="containerName">The current name prefix (to prepend to property names).</param>
            /// <returns>
            /// <c>true</c> if the set of <see cref="ApiParameterDescription"/> objects were created for the model.
            /// <c>false</c> if no <see cref="ApiParameterDescription"/> objects were created for the model.
            /// </returns>
            /// <remarks>
            /// Its the reponsibility of this method to create a parameter description for ALL of the current model
            /// or NONE of it. If a parameter description is created for ANY sub-properties of the model, then a parameter
            /// description will be created for ALL of them.
            /// </remarks>
            private bool Visit(
                ApiParameterDescriptionContext bindingContext,
                BindingSource ambientSource,
                string containerName)
            {
                var source = bindingContext.BindingSource;
                if (source != null && source.IsGreedy)
                {
                    // We have a definite answer for this model. This is a greedy source like
                    // [FromBody] so there's no need to consider properties.
                    Context.Results.Add(CreateResult(bindingContext, source, containerName));

                    return true;
                }

                var modelMetadata = bindingContext.ModelMetadata;

                // For any property which is a leaf node, we don't want to keep traversing:
                //
                //  1)  Collections - while it's possible to have binder attributes on the inside of a collection,
                //      it hardly seems useful, and would result in some very wierd binding.
                //
                //  2)  Simple Types - These are generally part of the .net framework - primitives, or types which have a
                //      type converter from string.
                //
                //  3)  Types with no properties. Obviously nothing to explore there.
                //
                if (modelMetadata.IsCollectionType ||
                    !modelMetadata.IsComplexType ||
                    !modelMetadata.Properties.Any())
                {
                    if (source == null || source == ambientSource)
                    {
                        // If it's a leaf node, and we have no new source then we don't know how to bind this.
                        // Return without creating any parameters, so that this can be included in the parent model.
                        return false;
                    }
                    else
                    {
                        // We found a new source, and this model has no properties. This is probabaly
                        // a simple type with an attribute like [FromQuery].
                        Context.Results.Add(CreateResult(bindingContext, source, containerName));
                        return true;
                    }
                }

                // This will come from composite model binding - so investigate what's going on with each property.
                // 
                // Basically once we find something that we know how to bind, we want to treat all properties at that
                // level (and higher levels) as separate parameters. 
                //
                // Ex:
                //
                //      public IActionResult PlaceOrder(OrderDTO order) {...}
                //
                //      public class OrderDTO
                //      {
                //          public int AccountId { get; set; }
                //          
                //          [FromBody]
                //          public Order { get; set; }
                //      }
                //
                // This should result in two parameters:
                //
                //  AccountId - source: Any
                //  Order - source: Body
                //

                var unboundProperties = new HashSet<ApiParameterDescriptionContext>();

                // We don't want to append the **parameter** name when building a model name.
                var newContainerName = containerName;
                if (modelMetadata.ContainerType != null)
                {
                    newContainerName = GetName(containerName, bindingContext);
                }

                foreach (var propertyMetadata in modelMetadata.Properties)
                {
                    var key = new PropertyKey(propertyMetadata, source);

                    var propertyContext = ApiParameterDescriptionContext.GetContext(
                        propertyMetadata,
                        bindingInfo: null,
                        propertyName: null);
                    if (Visited.Add(key))
                    {
                        if (!Visit(propertyContext, source ?? ambientSource, newContainerName))
                        {
                            unboundProperties.Add(propertyContext);
                        }
                    }
                    else
                    {
                        unboundProperties.Add(propertyContext);
                    }
                }

                if (unboundProperties.Count == modelMetadata.Properties.Count)
                {
                    if (source == null || source == ambientSource)
                    {
                        // No properties were bound and we didn't find a new source, let the caller handle it.
                        return false;
                    }
                    else
                    {
                        // We found a new source, and didn't create a result for any of the properties yet,
                        // so create a result for the current object.
                        Context.Results.Add(CreateResult(bindingContext, source, containerName));
                        return true;
                    }
                }
                else
                {
                    // This model was only partially bound, so create a result for all the other properties
                    foreach (var property in unboundProperties)
                    {
                        // Create a 'default' description for each property
                        Context.Results.Add(CreateResult(property, source ?? ambientSource, newContainerName));
                    }

                    return true;
                }
            }

            private ApiParameterDescription CreateResult(
                ApiParameterDescriptionContext bindingContext,
                BindingSource source,
                string containerName)
            {
                return new ApiParameterDescription()
                {
                    ModelMetadata = bindingContext.ModelMetadata,
                    Name = GetName(containerName, bindingContext),
                    Source = source,
                    Type = bindingContext.ModelMetadata.ModelType,
                };
            }

            private static string GetName(string containerName, ApiParameterDescriptionContext metadata)
            {
                if (!string.IsNullOrEmpty(metadata.BinderModelName))
                {
                    // Name was explicitly provided
                    return metadata.BinderModelName;
                }
                else
                {
                    return ModelBindingHelper.CreatePropertyModelName(containerName, metadata.PropertyName);
                }
            }

            private struct PropertyKey
            {
                public readonly Type ContainerType;

                public readonly string PropertyName;

                public readonly BindingSource Source;

                public PropertyKey(ModelMetadata metadata, BindingSource source)
                {
                    ContainerType = metadata.ContainerType;
                    PropertyName = metadata.PropertyName;
                    Source = source;
                }
            }

            private class PropertyKeyEqualityComparer : IEqualityComparer<PropertyKey>
            {
                public bool Equals(PropertyKey x, PropertyKey y)
                {
                    return
                        x.ContainerType == y.ContainerType &&
                        x.PropertyName == y.PropertyName &&
                        x.Source == y.Source;
                }

                public int GetHashCode(PropertyKey obj)
                {
                    return obj.ContainerType.GetHashCode() ^ obj.PropertyName.GetHashCode() ^ obj.Source.GetHashCode();
                }
            }
        }
    }
}