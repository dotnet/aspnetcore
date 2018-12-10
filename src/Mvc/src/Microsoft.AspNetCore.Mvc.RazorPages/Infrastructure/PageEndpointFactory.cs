// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    internal class PageEndpointFactory
    {
        private readonly PageRouteModelFactory _factory;
        private readonly RoutePatternTransformer _patternTransformer;

        public PageEndpointFactory(PageRouteModelFactory factory, RoutePatternTransformer patternTransformer)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (patternTransformer == null)
            {
                throw new ArgumentNullException(nameof(patternTransformer));
            }

            _factory = factory;
            _patternTransformer = patternTransformer;
        }

        public List<Endpoint> CreateEndpoints(
            IEnumerable<RazorCompiledItem> items,
            IReadOnlyList<Action<EndpointModel>> conventions)
        {
            var endpoints = new List<Endpoint>();
            
            var models = _factory.CreateModel(items);
            
            for (var i = 0; i < models.Count; i++)
            {
                var model = models[i];
                foreach (var model in CreateModels(tuples[i], routes))
                {
                    for (var k = 0; k < conventions.Count; k++)
                    {
                        conventions[k](model);
                    }

                    endpoints.Add(model.Build());
                }
            }

            return endpoints;
        }

        private static (ApplicationModel, ControllerModel, ActionModel, SelectorModel) Map(ApplicationModel application, ControllerModel controller, ActionModel action, SelectorModel selector)
        {
            return (application, controller, action, selector);
        }

        private void AddActionDescriptors(IList<ActionDescriptor> actions, PageRouteModel model)
        {
            foreach (var selector in model.Selectors)
            {
                var descriptor = new PageActionDescriptor
                {
                    ActionConstraints = selector.ActionConstraints.ToList(),
                    AreaName = model.AreaName,
                    AttributeRouteInfo = new AttributeRouteInfo
                    {
                        Name = selector.AttributeRouteModel.Name,
                        Order = selector.AttributeRouteModel.Order ?? 0,
                        Template = TransformPageRoute(model, selector),
                        SuppressLinkGeneration = selector.AttributeRouteModel.SuppressLinkGeneration,
                        SuppressPathMatching = selector.AttributeRouteModel.SuppressPathMatching,
                    },
                    DisplayName = $"Page: {model.ViewEnginePath}",
                    EndpointMetadata = selector.EndpointMetadata.ToList(),
                    FilterDescriptors = Array.Empty<FilterDescriptor>(),
                    Properties = new Dictionary<object, object>(model.Properties),
                    RelativePath = model.RelativePath,
                    ViewEnginePath = model.ViewEnginePath,
                };

                foreach (var kvp in model.RouteValues)
                {
                    if (!descriptor.RouteValues.ContainsKey(kvp.Key))
                    {
                        descriptor.RouteValues.Add(kvp.Key, kvp.Value);
                    }
                }

                if (!descriptor.RouteValues.ContainsKey("page"))
                {
                    descriptor.RouteValues.Add("page", model.ViewEnginePath);
                }

                actions.Add(descriptor);
            }
        }

        private static string TransformPageRoute(PageRouteModel model, SelectorModel selectorModel)
        {
            // Transformer not set on page route
            if (model.RouteParameterTransformer == null)
            {
                return selectorModel.AttributeRouteModel.Template;
            }

            var pageRouteMetadata = selectorModel.EndpointMetadata.OfType<PageRouteMetadata>().SingleOrDefault();
            if (pageRouteMetadata == null)
            {
                // Selector does not have expected metadata
                // This selector was likely configured by AddPageRouteModelConvention
                // Use the existing explicitly configured template
                return selectorModel.AttributeRouteModel.Template;
            }

            var segments = pageRouteMetadata.PageRoute.Split('/');
            for (var i = 0; i < segments.Length; i++)
            {
                segments[i] = model.RouteParameterTransformer.TransformOutbound(segments[i]);
            }

            var transformedPageRoute = string.Join("/", segments);

            // Combine transformed page route with template
            return AttributeRouteModel.CombineTemplates(transformedPageRoute, pageRouteMetadata.RouteTemplate);
        }

        private IEnumerable<ControllerActionEndpointModel> CreateModels((ApplicationModel, ControllerModel, ActionModel, SelectorModel) tuple)
        {
            var (application, controller, action, selector) = tuple;

            if (action.ActionName == "CreateProduct" && controller.ControllerName == "ConsumesAttribute_OverridesController")
            {
                Console.WriteLine();
            }

            var values = new RouteValueDictionary(controller.RouteValues);
            foreach (var kvp in action.RouteValues)
            {
                values[kvp.Key] = kvp.Value;
            }

            values.TryAdd("action", action.ActionName);
            values.TryAdd("controller", controller.ControllerName);

            // We need to transform data in the selector and conventional route mappings
            // into a route patterns. After this this block we will ignore the attribute route info
            // because we've already processed it.
            var patterns = new List<(RoutePattern pattern, AttributeRouteModel info, ConventionalRouteEntry route)>();
            if (selector.AttributeRouteModel == null)
            {
                // This is conventionally routed. There should be one model per *matching* conventional route.
                for (var i = 0; i < routes.Count; i++)
                {
                    var route = routes[i];
                    var pattern = _patternTransformer.SubstituteRequiredValues(route.Pattern, values);

                    // It's OK for pattern to be null - that means that the route cannot match this action.
                    if (pattern != null)
                    {
                        patterns.Add((pattern, null, route));
                    }
                }
            }
            else
            {
                // This is attribute routed. There should only be one model created.

                // Currently there is no supported way for user-code to specify additonal defaults and
                // constraints for an attribute routed action outside of the template.
                var pattern = RoutePatternFactory.Parse(selector.AttributeRouteModel.Template, values, null, values);
                patterns.Add((pattern, selector.AttributeRouteModel, null));
            }

            for (var i = 0; i < patterns.Count; i++)
            {
                var pattern = patterns[i];

                var model = new ControllerActionEndpointModel(controller.ControllerType, controller.ControllerName, action.ActionMethod, action.ActionName)
                {
                    DisplayName = ControllerActionDescriptor.GetDefaultDisplayName(controller.ControllerType, action.ActionMethod),
                    Order = pattern.info?.Order ?? pattern.route?.Order ?? 0,
                    RequestDelegate = null, // TODO
                    RoutePattern = pattern.pattern,
                };

                model.RequestDelegate = (context) =>
                {
                    var routeData = context.GetRouteData();
                    var endpoint = context.Features.Get<IEndpointFeature>().Endpoint;

                    var actionContext = new ActionContext(context, routeData, endpoint.Metadata.GetMetadata<ActionDescriptor>());

                    var invokerFactory = context.RequestServices.GetRequiredService<MvcEndpointInvokerFactory>();
                    var invoker = invokerFactory.CreateInvoker(actionContext);
                    return invoker.InvokeAsync();
                };

                for (var j = 0; j < controller.ControllerProperties.Count; j++)
                {
                    var property = controller.ControllerProperties[j];
                    if (property.BindingInfo != null)
                    {
                        model.Parameters.Add(new ControllerActionParameterModel(property.PropertyInfo)
                        {
                            BindingInfo = property.BindingInfo,
                            Name = property.PropertyName,
                        });
                    }
                }

                for (var j = 0; j < action.Parameters.Count; j++)
                {
                    var parameter = action.Parameters[j];
                    model.Parameters.Add(new ControllerActionParameterModel(parameter.ParameterInfo)
                    {
                        BindingInfo = parameter.BindingInfo,
                        Name = parameter.ParameterName,
                    });
                }

                var filters =
                    action.Filters.Select(f => new FilterDescriptor(f, FilterScope.Action))
                    .Concat(controller.Filters.Select(f => new FilterDescriptor(f, FilterScope.Controller)))
                    .Concat(application.Filters.Select(f => new FilterDescriptor(f, FilterScope.Global)))
                    .OrderBy(d => d, FilterDescriptorOrderComparer.Comparer);
                foreach (var filter in filters)
                {
                    model.Filters.Add(filter);
                }

                var apiDescriptionData = ApiDescriptionActionData.Create(application, controller, action, selector);
                if (apiDescriptionData != null)
                {
                    model.Properties[typeof(ApiDescriptionActionData)] = apiDescriptionData;
                }

                if (pattern.info != null)
                {
                    // Currently used by API Explorer
                    var info = new AttributeRouteInfo()
                    {
                        Name = pattern.info.Name,
                        Order = pattern.info.Order ?? 0,
                        SuppressLinkGeneration = pattern.info.SuppressLinkGeneration,
                        SuppressPathMatching = pattern.info.SuppressPathMatching,
                        Template = pattern.info.Template,
                    };

                    model.Properties[typeof(AttributeRouteInfo)] = info;
                }

                if (pattern.info?.Name != null)
                {
                    model.Metadata.Add(new RouteNameMetadata(pattern.info.Name));
                }

                model.Properties[typeof(IList<IActionConstraintMetadata>)] = selector.ActionConstraints;

                foreach (var metadata in selector.EndpointMetadata)
                {
                    model.Metadata.Add(metadata);
                }

                if (pattern.info != null && pattern.info.SuppressLinkGeneration)
                {
                    model.Metadata.Add(new SuppressLinkGenerationMetadata());
                }

                if (pattern.info != null && pattern.info.SuppressPathMatching)
                {
                    model.Metadata.Add(new SuppressMatchingMetadata());
                }

                foreach (var item in application.Properties)
                {
                    model.Properties[item.Key] = item.Value;
                }

                foreach (var item in controller.Properties)
                {
                    model.Properties[item.Key] = item.Value;
                }

                foreach (var item in action.Properties)
                {
                    model.Properties[item.Key] = item.Value;
                }

                yield return model;
            }
        }

    }
}
