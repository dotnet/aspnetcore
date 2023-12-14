// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// A facade service for creating application models.
/// </summary>
internal sealed class ApplicationModelFactory
{
    private readonly IApplicationModelProvider[] _applicationModelProviders;
    private readonly IList<IApplicationModelConvention> _conventions;

    public ApplicationModelFactory(
        IEnumerable<IApplicationModelProvider> applicationModelProviders,
        IOptions<MvcOptions> options)
    {
        ArgumentNullException.ThrowIfNull(applicationModelProviders);
        ArgumentNullException.ThrowIfNull(options);

        _applicationModelProviders = applicationModelProviders.OrderBy(p => p.Order).ToArray();
        _conventions = options.Value.Conventions;
    }

    public ApplicationModel CreateApplicationModel(IEnumerable<TypeInfo> controllerTypes)
    {
        ArgumentNullException.ThrowIfNull(controllerTypes);

        var context = new ApplicationModelProviderContext(controllerTypes);

        for (var i = 0; i < _applicationModelProviders.Length; i++)
        {
            _applicationModelProviders[i].OnProvidersExecuting(context);
        }

        for (var i = _applicationModelProviders.Length - 1; i >= 0; i--)
        {
            _applicationModelProviders[i].OnProvidersExecuted(context);
        }

        ApplicationModelConventions.ApplyConventions(context.Result, _conventions);

        return context.Result;
    }

    public static List<TResult> Flatten<TResult>(
        ApplicationModel application,
        Func<ApplicationModel, ControllerModel, ActionModel, SelectorModel, TResult> flattener)
    {
        var results = new List<TResult>();

        var actionsByMethod = new Dictionary<MethodInfo, List<(ActionModel, SelectorModel)>>();
        var actionsByRouteName = new Dictionary<string, List<(ActionModel, SelectorModel)>>(StringComparer.OrdinalIgnoreCase);

        var routeTemplateErrors = new List<string>();

        foreach (var controller in application.Controllers)
        {
            foreach (var action in controller.Actions)
            {
                foreach (var selector in ActionAttributeRouteModel.FlattenSelectors(action))
                {
                    // PostProcess attribute routes so we can observe any errors.
                    ReplaceAttributeRouteTokens(controller, action, selector, routeTemplateErrors);

                    // Add to the data structures we use to find errors.
                    AddActionToMethodInfoMap(actionsByMethod, action, selector);
                    AddActionToRouteNameMap(actionsByRouteName, action, selector);

                    var result = flattener(application, controller, action, selector);
                    Debug.Assert(result != null);

                    results.Add(result);
                }
            }
        }

        var attributeRoutingConfigurationErrors = new Dictionary<MethodInfo, string>();
        foreach (var (method, actions) in actionsByMethod)
        {
            ValidateActionGroupConfiguration(
                method,
                actions,
                attributeRoutingConfigurationErrors);
        }

        if (attributeRoutingConfigurationErrors.Count > 0)
        {
            var message = CreateAttributeRoutingAggregateErrorMessage(attributeRoutingConfigurationErrors.Values);

            throw new InvalidOperationException(message);
        }

        var namedRoutedErrors = ValidateNamedAttributeRoutedActions(actionsByRouteName);
        if (namedRoutedErrors.Count > 0)
        {
            var message = CreateAttributeRoutingAggregateErrorMessage(namedRoutedErrors);
            throw new InvalidOperationException(message);
        }

        if (routeTemplateErrors.Count > 0)
        {
            var message = CreateAttributeRoutingAggregateErrorMessage(routeTemplateErrors);
            throw new InvalidOperationException(message);
        }

        return results;
    }

    private static void ReplaceAttributeRouteTokens(
        ControllerModel controller,
        ActionModel action,
        SelectorModel selector,
        List<string> errors)
    {
        if (selector.AttributeRouteModel == null)
        {
            return;
        }

        try
        {
            var routeValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    { "action", action.ActionName },
                    { "controller", controller.ControllerName },
                };

            foreach (var kvp in action.RouteValues)
            {
                routeValues.TryAdd(kvp.Key, kvp.Value);
            }

            foreach (var kvp in controller.RouteValues)
            {
                routeValues.TryAdd(kvp.Key, kvp.Value);
            }

            selector.AttributeRouteModel.Template = AttributeRouteModel.ReplaceTokens(
                selector.AttributeRouteModel.Template!,
                routeValues,
                action.RouteParameterTransformer);

            if (selector.AttributeRouteModel.Name != null)
            {
                selector.AttributeRouteModel.Name = AttributeRouteModel.ReplaceTokens(
                    selector.AttributeRouteModel.Name,
                    routeValues,
                    action.RouteParameterTransformer);
            }
        }
        catch (InvalidOperationException ex)
        {
            // Routing will throw an InvalidOperationException here if we can't parse/replace tokens
            // in the template.
            var message = Resources.FormatAttributeRoute_IndividualErrorMessage(
                action.DisplayName,
                Environment.NewLine,
                ex.Message);

            errors.Add(message);
        }
    }

    private static void AddActionToMethodInfoMap(
        Dictionary<MethodInfo, List<(ActionModel, SelectorModel)>> actionsByMethod,
        ActionModel action,
        SelectorModel selector)
    {
        if (!actionsByMethod.TryGetValue(action.ActionMethod, out var actions))
        {
            actions = new List<(ActionModel, SelectorModel)>();
            actionsByMethod.Add(action.ActionMethod, actions);
        }

        actions.Add((action, selector));
    }

    private static void AddActionToRouteNameMap(
        Dictionary<string, List<(ActionModel action, SelectorModel selector)>> actionsByRouteName,
        ActionModel action,
        SelectorModel selector)
    {
        var routeName = selector.AttributeRouteModel?.Name;
        if (routeName == null)
        {
            return;
        }

        if (!actionsByRouteName.TryGetValue(routeName, out var actions))
        {
            actions = new List<(ActionModel, SelectorModel)>();
            actionsByRouteName.Add(routeName, actions);
        }

        actions.Add((action, selector));
    }

    private static List<string> AddErrorNumbers(IEnumerable<string> namedRoutedErrors)
    {
        return namedRoutedErrors
            .Select((error, i) =>
                Resources.FormatAttributeRoute_AggregateErrorMessage_ErrorNumber(
                    i + 1,
                    Environment.NewLine,
                    error))
            .ToList();
    }

    private static List<string> ValidateNamedAttributeRoutedActions(
        Dictionary<string, List<(ActionModel action, SelectorModel selector)>> actionsByRouteName)
    {
        var namedRouteErrors = new List<string>();

        foreach (var (routeName, actions) in actionsByRouteName)
        {
            // We are looking for attribute routed actions that have the same name but
            // different route templates. We pick the first template of the group and
            // we compare it against the rest of the templates that have that same name
            // associated.
            // The moment we find one that is different we report the whole group to the
            // user in the error message so that they can see the different actions and the
            // different templates for a given named attribute route.
            var template = actions[0].selector.AttributeRouteModel!.Template!;

            for (var i = 1; i < actions.Count; i++)
            {
                var other = actions[i].selector.AttributeRouteModel!.Template;

                if (!template.Equals(other, StringComparison.OrdinalIgnoreCase))
                {
                    var descriptions = actions.Select(a =>
                    {
                        return Resources.FormatAttributeRoute_DuplicateNames_Item(a.action.DisplayName, a.selector.AttributeRouteModel!.Template);
                    });

                    var message = Resources.FormatAttributeRoute_DuplicateNames(routeName, Environment.NewLine, string.Join(Environment.NewLine, descriptions));
                    namedRouteErrors.Add(message);
                    break;
                }
            }
        }

        return namedRouteErrors;
    }

    private static void ValidateActionGroupConfiguration(
        MethodInfo method,
        List<(ActionModel action, SelectorModel selector)> actions,
        IDictionary<MethodInfo, string> routingConfigurationErrors)
    {
        var hasAttributeRoutedActions = false;
        var hasConventionallyRoutedActions = false;

        for (var i = 0; i < actions.Count; i++)
        {
            if (actions[i].selector.AttributeRouteModel == null)
            {
                hasConventionallyRoutedActions = true;
            }
            else
            {
                hasAttributeRoutedActions = true;
            }
        }

        // Validate that no method result in attribute and non attribute actions at the same time.
        // By design, mixing attribute and conventionally actions in the same method is not allowed.
        //
        // Assuming the controller doesn't specify a route template, this example would not be allowed:
        //
        // [HttpGet]
        // [HttpPost("Foo")]
        // public void Foo() { }
        if (hasAttributeRoutedActions && hasConventionallyRoutedActions)
        {
            var message = CreateMixedRoutedActionDescriptorsErrorMessage(method, actions);
            routingConfigurationErrors.Add(method, message);
        }
    }

    private static string CreateMixedRoutedActionDescriptorsErrorMessage(
        MethodInfo method,
        List<(ActionModel action, SelectorModel selector)> actions)
    {
        // Text to show as the attribute route template for conventionally routed actions.
        var nullTemplate = Resources.AttributeRoute_NullTemplateRepresentation;

        var actionDescriptions = new List<string>(actions.Count);
        for (var i = 0; i < actions.Count; i++)
        {
            var (action, selector) = actions[i];
            var routeTemplate = selector.AttributeRouteModel?.Template ?? nullTemplate;

            var verbs = selector.ActionConstraints?.OfType<HttpMethodActionConstraint>().FirstOrDefault()?.HttpMethods;

            var formattedVerbs = string.Empty;
            if (verbs != null)
            {
                formattedVerbs = string.Join(", ", verbs.OrderBy(v => v, StringComparer.OrdinalIgnoreCase));
            }

            var description = Resources.FormatAttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod_Item(
                action.DisplayName,
                routeTemplate,
                formattedVerbs);

            actionDescriptions.Add(description);
        }

        // Sample error message:
        //
        // A method 'MyApplication.CustomerController.Index' must not define attributed actions and
        // non attributed actions at the same time:
        // Action: 'MyApplication.CustomerController.Index' - Route Template: 'Products' - HTTP Verbs: 'PUT'
        // Action: 'MyApplication.CustomerController.Index' - Route Template: '(none)' - HTTP Verbs: 'POST'
        //
        // Use 'AcceptVerbsAttribute' to create a single route that allows multiple HTTP verbs and defines a route,
        // or set a route template in all attributes that constrain HTTP verbs.

        var type = method.ReflectedType!;
        var formattedMethodInfo = $"{TypeNameHelper.GetTypeDisplayName(type)}.{method.Name} ({type.Assembly.GetName().Name})";
        return Resources.FormatAttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod(
                formattedMethodInfo,
                Environment.NewLine,
                string.Join(Environment.NewLine, actionDescriptions));
    }

    private static string CreateAttributeRoutingAggregateErrorMessage(IEnumerable<string> individualErrors)
    {
        var errorMessages = AddErrorNumbers(individualErrors);

        var message = Resources.FormatAttributeRoute_AggregateErrorMessage(
            Environment.NewLine,
            string.Join(Environment.NewLine + Environment.NewLine, errorMessages));
        return message;
    }
}
