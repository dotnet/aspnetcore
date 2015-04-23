// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.ApiExplorer;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Framework.Internal;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Test
{
    public class ControllerActionDescriptorProviderTests
    {
        [Fact]
        public void GetDescriptors_GetsDescriptorsOnlyForValidActions()
        {
            // Arrange
            var provider = GetProvider(typeof(PersonController).GetTypeInfo());

            // Act
            var descriptors = provider.GetDescriptors();
            var actionNames = descriptors.Select(ad => ad.Name);

            // Assert
            Assert.Equal(new[] { "GetPerson", "ShowPeople", }, actionNames);
        }

        [Fact]
        public void GetDescriptors_IncludesFilters()
        {
            // Arrange
            var globalFilter = new MyFilterAttribute(1);
            var provider = GetProvider(typeof(FiltersController).GetTypeInfo(), new IFilter[]
            {
                globalFilter,
            });

            // Act
            var descriptors = provider.GetDescriptors();
            var descriptor = Assert.Single(descriptors);

            // Assert
            Assert.Equal(3, descriptor.FilterDescriptors.Count);

            var filter1 = descriptor.FilterDescriptors[0];
            Assert.Same(globalFilter, filter1.Filter);
            Assert.Equal(FilterScope.Global, filter1.Scope);

            var filter2 = descriptor.FilterDescriptors[1];
            Assert.Equal(2, Assert.IsType<MyFilterAttribute>(filter2.Filter).Value);
            Assert.Equal(FilterScope.Controller, filter2.Scope);

            var filter3 = descriptor.FilterDescriptors[2];
            Assert.Equal(3, Assert.IsType<MyFilterAttribute>(filter3.Filter).Value); ;
            Assert.Equal(FilterScope.Action, filter3.Scope);
        }

        [Fact]
        public void GetDescriptors_AddsHttpMethodConstraints_ForConventionallyRoutedActions()
        {
            // Arrange
            var provider = GetProvider(typeof(HttpMethodController).GetTypeInfo());

            // Act
            var descriptors = provider.GetDescriptors();
            var descriptor = Assert.Single(descriptors);

            // Assert
            Assert.Equal("OnlyPost", descriptor.Name);

            var constraint = Assert.IsType<HttpMethodConstraint>(Assert.Single(descriptor.ActionConstraints));
            Assert.Equal(new string[] { "POST" }, constraint.HttpMethods);
        }

        [Fact]
        public void GetDescriptors_HttpMethodConstraint_RouteOnController()
        {
            // Arrange
            var provider = GetProvider(typeof(AttributeRoutedHttpMethodController).GetTypeInfo());

            // Act
            var descriptors = provider.GetDescriptors();
            var descriptor = Assert.Single(descriptors);

            // Assert
            Assert.Equal("Items", descriptor.AttributeRouteInfo.Template);

            var constraint = Assert.IsType<HttpMethodConstraint>(Assert.Single(descriptor.ActionConstraints));
            Assert.Equal(new string[] { "PUT", "PATCH" }, constraint.HttpMethods);
        }

        [Fact]
        public void GetDescriptors_AddsParameters_ToActionDescriptor()
        {
            // Arrange & Act
            var descriptors = GetDescriptors(
                typeof(ActionParametersController).GetTypeInfo());

            // Assert
            var main = Assert.Single(descriptors,
                d => d.Name.Equals(nameof(ActionParametersController.RequiredInt)));

            Assert.NotNull(main.Parameters);
            var id = Assert.Single(main.Parameters);

            Assert.Equal("id", id.Name);
            Assert.Null(id.BindingInfo?.BindingSource);
            Assert.Equal(typeof(int), id.ParameterType);
        }

        [Fact]
        public void GetDescriptors_AddsMultipleParameters_ToActionDescriptor()
        {
            // Arrange & Act
            var descriptors = GetDescriptors(
                typeof(ActionParametersController).GetTypeInfo());

            // Assert
            var main = Assert.Single(descriptors,
                d => d.Name.Equals(nameof(ActionParametersController.MultipleParameters)));

            Assert.NotNull(main.Parameters);
            var id = Assert.Single(main.Parameters, p => p.Name == "id");

            Assert.Equal("id", id.Name);
            Assert.Null(id.BindingInfo?.BindingSource);
            Assert.Equal(typeof(int), id.ParameterType);

            var entity = Assert.Single(main.Parameters, p => p.Name == "entity");

            Assert.Equal("entity", entity.Name);
            Assert.Equal(entity.BindingInfo.BindingSource, BindingSource.Body);
            Assert.Equal(typeof(TestActionParameter), entity.ParameterType);
        }

        [Fact]
        public void GetDescriptors_AddsMultipleParametersWithDifferentCasing_ToActionDescriptor()
        {
            // Arrange & Act
            var descriptors = GetDescriptors(
                typeof(ActionParametersController).GetTypeInfo());

            // Assert
            var main = Assert.Single(descriptors,
                d => d.Name.Equals(nameof(ActionParametersController.DifferentCasing)));

            Assert.NotNull(main.Parameters);
            var id = Assert.Single(main.Parameters, p => p.Name == "id");

            Assert.Equal("id", id.Name);
            Assert.Null(id.BindingInfo?.BindingSource);
            Assert.Equal(typeof(int), id.ParameterType);

            var upperCaseId = Assert.Single(main.Parameters, p => p.Name == "ID");

            Assert.Equal("ID", upperCaseId.Name);
            Assert.Null(upperCaseId.BindingInfo?.BindingSource);
            Assert.Equal(typeof(int), upperCaseId.ParameterType);

            var pascalCaseId = Assert.Single(main.Parameters, p => p.Name == "Id");

            Assert.Equal("Id", pascalCaseId.Name);
            Assert.Null(id.BindingInfo?.BindingSource);
            Assert.Equal(typeof(int), pascalCaseId.ParameterType);
        }

        [Fact]
        public void GetDescriptors_AddsParameters_DetectsFromBodyParameters()
        {
            // Arrange & Act
            var actionName = nameof(ActionParametersController.FromBodyParameter);

            var descriptors = GetDescriptors(
                typeof(ActionParametersController).GetTypeInfo());

            // Assert
            var fromBody = Assert.Single(descriptors,
                d => d.Name.Equals(actionName));

            Assert.NotNull(fromBody.Parameters);
            var entity = Assert.Single(fromBody.Parameters);

            Assert.Equal("entity", entity.Name);
            Assert.Equal(entity.BindingInfo.BindingSource, BindingSource.Body);
            Assert.Equal(typeof(TestActionParameter), entity.ParameterType);
        }

        [Fact]
        public void GetDescriptors_AddsParameters_DoesNotDetectParameterFromBody_IfNoFromBodyAttribute()
        {
            // Arrange & Act
            var actionName = nameof(ActionParametersController.NotFromBodyParameter);

            var descriptors = GetDescriptors(
                typeof(ActionParametersController).GetTypeInfo());

            // Assert
            var notFromBody = Assert.Single(descriptors,
                d => d.Name.Equals(actionName));

            Assert.NotNull(notFromBody.Parameters);
            var entity = Assert.Single(notFromBody.Parameters);

            Assert.Equal("entity", entity.Name);
            Assert.Null(entity.BindingInfo?.BindingSource);
            Assert.Equal(typeof(TestActionParameter), entity.ParameterType);
        }

        [Fact]
        public void GetDescriptors_AddsControllerAndActionConstraints_ToConventionallyRoutedActions()
        {
            // Arrange & Act
            var descriptors = GetDescriptors(
                typeof(ConventionallyRoutedController).GetTypeInfo());

            // Assert
            var action = Assert.Single(descriptors);

            Assert.NotNull(action.RouteConstraints);

            var controller = Assert.Single(action.RouteConstraints,
                rc => rc.RouteKey.Equals("controller"));
            Assert.Equal(RouteKeyHandling.RequireKey, controller.KeyHandling);
            Assert.Equal("ConventionallyRouted", controller.RouteValue);

            var actionConstraint = Assert.Single(action.RouteConstraints,
                rc => rc.RouteKey.Equals("action"));
            Assert.Equal(RouteKeyHandling.RequireKey, actionConstraint.KeyHandling);
            Assert.Equal(nameof(ConventionallyRoutedController.ConventionalAction), actionConstraint.RouteValue);
        }

        [Fact]
        public void GetDescriptors_AddsControllerAndActionDefaults_ToAttributeRoutedActions()
        {
            // Arrange & Act
            var descriptors = GetDescriptors(
                typeof(AttributeRoutedController).GetTypeInfo());

            // Assert
            var action = Assert.Single(descriptors);

            var routeconstraint = Assert.Single(action.RouteConstraints);
            Assert.Equal(RouteKeyHandling.RequireKey, routeconstraint.KeyHandling);
            Assert.Equal(AttributeRouting.RouteGroupKey, routeconstraint.RouteKey);

            var controller = Assert.Single(action.RouteValueDefaults,
                rc => rc.Key.Equals("controller"));
            Assert.Equal("AttributeRouted", controller.Value);

            var actionConstraint = Assert.Single(action.RouteValueDefaults,
                rc => rc.Key.Equals("action"));
            Assert.Equal(nameof(AttributeRoutedController.AttributeRoutedAction), actionConstraint.Value);
        }

        [Fact]
        public void GetDescriptors_WithRouteDataConstraint_WithBlockNonAttributedActions()
        {
            // Arrange & Act
            var descriptors = GetDescriptors(
                typeof(HttpMethodController).GetTypeInfo(),
                typeof(BlockNonAttributedActionsController).GetTypeInfo()).ToArray();

            var descriptorWithoutConstraint = Assert.Single(
                descriptors,
                ad => ad.RouteConstraints.Any(
                    c => c.RouteKey == "key" && c.KeyHandling == RouteKeyHandling.DenyKey));

            var descriptorWithConstraint = Assert.Single(
                descriptors,
                ad => ad.RouteConstraints.Any(
                    c =>
                        c.KeyHandling == RouteKeyHandling.RequireKey &&
                        c.RouteKey == "key" &&
                        c.RouteValue == "value"));

            // Assert
            Assert.Equal(2, descriptors.Length);

            Assert.Equal(3, descriptorWithConstraint.RouteConstraints.Count);
            Assert.Single(
                descriptorWithConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "controller" &&
                    c.RouteValue == "BlockNonAttributedActions");
            Assert.Single(
                descriptorWithConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "action" &&
                    c.RouteValue == "Edit");
            Assert.Single(
                descriptorWithConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "key" &&
                    c.RouteValue == "value" &&
                    c.KeyHandling == RouteKeyHandling.RequireKey);

            Assert.Equal(3, descriptorWithoutConstraint.RouteConstraints.Count);
            Assert.Single(
                descriptorWithoutConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "controller" &&
                    c.RouteValue == "HttpMethod");
            Assert.Single(
                descriptorWithoutConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "action" &&
                    c.RouteValue == "OnlyPost");
            Assert.Single(
                descriptorWithoutConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "key" &&
                    c.RouteValue == string.Empty &&
                    c.KeyHandling == RouteKeyHandling.DenyKey);
        }

        [Fact]
        public void GetDescriptors_WithRouteDataConstraint_WithoutBlockNonAttributedActions()
        {
            // Arrange & Act
            var descriptors = GetDescriptors(
                typeof(HttpMethodController).GetTypeInfo(),
                typeof(DontBlockNonAttributedActionsController).GetTypeInfo()).ToArray();

            var descriptorWithConstraint = Assert.Single(
                descriptors,
                ad => ad.RouteConstraints.Any(
                    c =>
                        c.KeyHandling == RouteKeyHandling.RequireKey &&
                        c.RouteKey == "key" &&
                        c.RouteValue == "value"));

            var descriptorWithoutConstraint = Assert.Single(
                descriptors,
                ad => !ad.RouteConstraints.Any(c => c.RouteKey == "key"));

            // Assert
            Assert.Equal(2, descriptors.Length);

            Assert.Equal(3, descriptorWithConstraint.RouteConstraints.Count);
            Assert.Single(
                descriptorWithConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "controller" &&
                    c.RouteValue == "DontBlockNonAttributedActions");
            Assert.Single(
                descriptorWithConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "action" &&
                    c.RouteValue == "Create");
            Assert.Single(
                descriptorWithConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "key" &&
                    c.RouteValue == "value" &&
                    c.KeyHandling == RouteKeyHandling.RequireKey);

            Assert.Equal(2, descriptorWithoutConstraint.RouteConstraints.Count);
            Assert.Single(
                descriptorWithoutConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "controller" &&
                    c.RouteValue == "HttpMethod");
            Assert.Single(
                descriptorWithoutConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "action" &&
                    c.RouteValue == "OnlyPost");
        }

        [Fact]
        public void BuildModel_IncludesGlobalFilters()
        {
            // Arrange
            var filter = new MyFilterAttribute(1);
            var provider = GetProvider(typeof(PersonController).GetTypeInfo(), new IFilter[]
            {
                filter,
            });

            // Act
            var model = provider.BuildModel();

            // Assert
            var filters = model.Filters;
            Assert.Same(filter, Assert.Single(filters));
        }

        [Fact]
        public void BuildModel_CreatesControllerModels_ForAllControllers()
        {
            // Arrange
            var provider = GetProvider(
                typeof(ConventionallyRoutedController).GetTypeInfo(),
                typeof(AttributeRoutedController).GetTypeInfo(),
                typeof(EmptyController).GetTypeInfo(),
                typeof(NonActionAttributeController).GetTypeInfo());

            // Act
            var model = provider.BuildModel();

            // Assert
            Assert.NotNull(model);
            Assert.Equal(4, model.Controllers.Count);

            var conventional = Assert.Single(model.Controllers,
                c => c.ControllerName == "ConventionallyRouted");
            Assert.Empty(conventional.AttributeRoutes);
            Assert.Single(conventional.Actions);

            var attributeRouted = Assert.Single(model.Controllers,
                c => c.ControllerName == "AttributeRouted");
            Assert.Single(attributeRouted.Actions);
            Assert.Single(attributeRouted.AttributeRoutes);

            var empty = Assert.Single(model.Controllers,
                c => c.ControllerName == "Empty");
            Assert.Empty(empty.Actions);

            var nonAction = Assert.Single(model.Controllers,
                c => c.ControllerName == "NonActionAttribute");
            Assert.Empty(nonAction.Actions);
        }

        [Fact]
        public void BuildModel_CreatesControllerActionDescriptors_ForValidActions()
        {
            // Arrange
            var provider = GetProvider(
                typeof(PersonController).GetTypeInfo());

            // Act
            var model = provider.BuildModel();

            // Assert
            var controller = Assert.Single(model.Controllers);

            Assert.Equal(2, controller.Actions.Count);

            var getPerson = Assert.Single(controller.Actions, a => a.ActionName == "GetPerson");
            Assert.Empty(getPerson.HttpMethods);

            var showPeople = Assert.Single(controller.Actions, a => a.ActionName == "ShowPeople");
            Assert.Empty(showPeople.HttpMethods);
        }

        [Fact]
        public void GetDescriptor_SetsDisplayName()
        {
            // Arrange
            var provider = GetProvider(typeof(PersonController).GetTypeInfo());

            // Act
            var descriptors = provider.GetDescriptors();
            var displayNames = descriptors.Select(ad => ad.DisplayName);

            // Assert
            Assert.Equal(
                new[]
                {
                    "Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+PersonController.GetPerson",
                    "Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+PersonController.ListPeople",
                },
                displayNames);
        }

        public void AttributeRouting_TokenReplacement_IsAfterReflectedModel()
        {
            // Arrange
            var provider = GetProvider(typeof(TokenReplacementController).GetTypeInfo());

            // Act
            var model = provider.BuildModel();

            // Assert
            var controller = Assert.Single(model.Controllers);

            var attributeRouteModel = Assert.Single(controller.AttributeRoutes);
            Assert.Equal("api/Token/[key]/[controller]", attributeRouteModel.Template);

            var action = Assert.Single(controller.Actions);
            Assert.Equal("stub/[action]", action.AttributeRouteModel.Template);
        }

        [Fact]
        public void AttributeRouting_TokenReplacement_InActionDescriptor()
        {
            // Arrange
            var provider = GetProvider(typeof(TokenReplacementController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            var action = Assert.Single(actions);
            Assert.Equal("api/Token/value/TokenReplacement/stub/ThisIsAnAction", action.AttributeRouteInfo.Template);
        }

        [Fact]
        public void AttributeRouting_TokenReplacement_ThrowsWithMultipleMessages()
        {
            // Arrange
            var provider = GetProvider(typeof(MultipleErrorsController).GetTypeInfo());

            var expectedMessage =
                "The following errors occurred with attribute routing information:" + Environment.NewLine +
                Environment.NewLine +
                "Error 1:" + Environment.NewLine +
                "For action: 'Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+" +
                "MultipleErrorsController.Unknown'" + Environment.NewLine +
                "Error: While processing template 'stub/[action]/[unknown]', a replacement value for the token 'unknown' " +
                "could not be found. Available tokens: 'action, controller'." + Environment.NewLine +
                Environment.NewLine +
                "Error 2:" + Environment.NewLine +
                "For action: 'Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+" +
                "MultipleErrorsController.Invalid'" + Environment.NewLine +
                "Error: The route template '[invalid/syntax' has invalid syntax. A replacement token is not closed.";

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => { provider.GetDescriptors(); });

            // Assert
            VerifyMultiLineError(expectedMessage, ex.Message, unorderedStart: 2, unorderedLineCount: 6);
        }

        [Fact]
        public void AttributeRouting_CreatesOneActionDescriptor_PerControllerAndActionRouteCombination()
        {
            // Arrange
            var provider = GetProvider(typeof(MultiRouteAttributesController).GetTypeInfo());

            // Act
            var descriptors = provider.GetDescriptors();

            // Assert
            var actions = descriptors.Where(d => d.Name == "MultipleHttpGet");
            Assert.Equal(4, actions.Count());

            foreach (var action in actions)
            {
                Assert.Equal("MultipleHttpGet", action.Name);
                Assert.Equal("MultiRouteAttributes", action.ControllerName);
            }

            Assert.Single(actions, a => a.AttributeRouteInfo.Template.Equals("v1/List"));
            Assert.Single(actions, a => a.AttributeRouteInfo.Template.Equals("v1/All"));
            Assert.Single(actions, a => a.AttributeRouteInfo.Template.Equals("v2/List"));
            Assert.Single(actions, a => a.AttributeRouteInfo.Template.Equals("v2/All"));
        }

        [Fact]
        public void AttributeRouting_AcceptVerbsOnAction_CreatesActionPerControllerAttributeRouteCombination()
        {
            // Arrange
            var provider = GetProvider(typeof(MultiRouteAttributesController).GetTypeInfo());

            // Act
            var descriptors = provider.GetDescriptors();

            // Assert
            var actions = descriptors.Where(d => d.Name == "AcceptVerbs");
            Assert.Equal(2, actions.Count());

            foreach (var action in actions)
            {
                Assert.Equal("MultiRouteAttributes", action.ControllerName);

                Assert.NotNull(action.ActionConstraints);
                var methodConstraint = Assert.IsType<HttpMethodConstraint>(Assert.Single(action.ActionConstraints));

                Assert.NotNull(methodConstraint.HttpMethods);
                Assert.Equal(new[] { "POST" }, methodConstraint.HttpMethods);
            }

            Assert.Single(actions, a => a.AttributeRouteInfo.Template.Equals("v1/List"));
            Assert.Single(actions, a => a.AttributeRouteInfo.Template.Equals("v2/List"));
        }

        [Fact]
        public void AttributeRouting_AcceptVerbsOnActionWithOverrideTemplate_CreatesSingleAttributeRoutedAction()
        {
            // Arrange
            var provider = GetProvider(typeof(MultiRouteAttributesController).GetTypeInfo());

            // Act
            var descriptors = provider.GetDescriptors();

            // Assert
            var action = Assert.Single(descriptors, d => d.Name == "AcceptVerbsOverride");

            Assert.Equal("MultiRouteAttributes", action.ControllerName);

            Assert.NotNull(action.ActionConstraints);
            var methodConstraint = Assert.IsType<HttpMethodConstraint>(Assert.Single(action.ActionConstraints));

            Assert.NotNull(methodConstraint.HttpMethods);
            Assert.Equal(new[] { "PUT" }, methodConstraint.HttpMethods);

            Assert.NotNull(action.AttributeRouteInfo);
            Assert.Equal("Override", action.AttributeRouteInfo.Template);
        }

        [Fact]
        public void AttributeRouting_AcceptVerbsOnAction_WithoutTemplate_MergesVerb()
        {
            // Arrange
            var provider = GetProvider(typeof(MultiRouteAttributesController).GetTypeInfo());

            // Act
            var descriptors = provider.GetDescriptors();

            // Assert
            var actions = descriptors.Where(d => d.Name == "AcceptVerbsRouteAttributeAndHttpPut");
            Assert.Equal(4, actions.Count());

            foreach (var action in actions)
            {
                Assert.Equal("MultiRouteAttributes", action.ControllerName);

                Assert.NotNull(action.AttributeRouteInfo);
                Assert.NotNull(action.AttributeRouteInfo.Template);
            }

            var constrainedActions = actions.Where(a => a.ActionConstraints != null);
            Assert.Equal(4, constrainedActions.Count());

            // Actions generated by PutAttribute
            var putActions = constrainedActions.Where(
                a => a.ActionConstraints.OfType<HttpMethodConstraint>().Single().HttpMethods.Single() == "PUT");
            Assert.Equal(2, putActions.Count());
            Assert.Single(putActions, a => a.AttributeRouteInfo.Template.Equals("v1/All"));
            Assert.Single(putActions, a => a.AttributeRouteInfo.Template.Equals("v2/All"));

            // Actions generated by RouteAttribute
            var routeActions = actions.Where(
                a => a.ActionConstraints.OfType<HttpMethodConstraint>().Single().HttpMethods.Single() == "POST");
            Assert.Equal(2, routeActions.Count());
            Assert.Single(routeActions, a => a.AttributeRouteInfo.Template.Equals("v1/List"));
            Assert.Single(routeActions, a => a.AttributeRouteInfo.Template.Equals("v2/List"));
        }

        [Fact]
        public void AttributeRouting_AcceptVerbsOnAction_WithTemplate_DoesNotMergeVerb()
        {
            // Arrange
            var provider = GetProvider(typeof(MultiRouteAttributesController).GetTypeInfo());

            // Act
            var descriptors = provider.GetDescriptors();

            // Assert
            var actions = descriptors.Where(d => d.Name == "AcceptVerbsRouteAttributeWithTemplateAndHttpPut");
            Assert.Equal(6, actions.Count());

            foreach (var action in actions)
            {
                Assert.Equal("MultiRouteAttributes", action.ControllerName);

                Assert.NotNull(action.AttributeRouteInfo);
                Assert.NotNull(action.AttributeRouteInfo.Template);
            }

            var constrainedActions = actions.Where(a => a.ActionConstraints != null);
            Assert.Equal(4, constrainedActions.Count());

            // Actions generated by AcceptVerbs
            var postActions = constrainedActions.Where(
                a => a.ActionConstraints.OfType<HttpMethodConstraint>().Single().HttpMethods.Single() == "POST");
            Assert.Equal(2, postActions.Count());
            Assert.Single(postActions, a => a.AttributeRouteInfo.Template.Equals("v1"));
            Assert.Single(postActions, a => a.AttributeRouteInfo.Template.Equals("v2"));

            // Actions generated by PutAttribute
            var putActions = constrainedActions.Where(
                a => a.ActionConstraints.OfType<HttpMethodConstraint>().Single().HttpMethods.Single() == "PUT");
            Assert.Equal(2, putActions.Count());
            Assert.Single(putActions, a => a.AttributeRouteInfo.Template.Equals("v1/All"));
            Assert.Single(putActions, a => a.AttributeRouteInfo.Template.Equals("v2/All"));

            // Actions generated by RouteAttribute
            var unconstrainedActions = actions.Where(a => a.ActionConstraints == null);
            Assert.Equal(2, unconstrainedActions.Count());
            Assert.Single(unconstrainedActions, a => a.AttributeRouteInfo.Template.Equals("v1/List"));
            Assert.Single(unconstrainedActions, a => a.AttributeRouteInfo.Template.Equals("v2/List"));
        }

        [Fact]
        public void AttributeRouting_AllowsDuplicateAttributeRoutedActions_WithTheSameTemplateAndSameHttpMethodsOnDifferentActions()
        {
            // Arrange
            var provider = GetProvider(typeof(NonDuplicatedAttributeRouteController).GetTypeInfo());
            var firstActionName = nameof(NonDuplicatedAttributeRouteController.ControllerAndAction);
            var secondActionName = nameof(NonDuplicatedAttributeRouteController.OverrideOnAction);

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            var controllerAndAction = Assert.Single(actions, a => a.Name.Equals(firstActionName));
            Assert.NotNull(controllerAndAction.AttributeRouteInfo);

            var controllerActionAndOverride = Assert.Single(actions, a => a.Name.Equals(secondActionName));
            Assert.NotNull(controllerActionAndOverride.AttributeRouteInfo);

            Assert.Equal(
                controllerAndAction.AttributeRouteInfo.Template,
                controllerActionAndOverride.AttributeRouteInfo.Template,
                StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void AttributeRouting_AllowsDuplicateAttributeRoutedActions_WithTheSameTemplateAndDifferentHttpMethodsOnTheSameAction()
        {
            // Arrange
            var provider = GetProvider(typeof(NonDuplicatedAttributeRouteController).GetTypeInfo());
            var actionName = nameof(NonDuplicatedAttributeRouteController.DifferentHttpMethods);

            // Act
            var descriptors = provider.GetDescriptors();

            // Assert
            var actions = descriptors.Where(d => d.Name.Equals(actionName));
            Assert.Equal(5, actions.Count());

            foreach (var method in new[] { "GET", "POST", "PUT", "PATCH", "DELETE" })
            {
                var action = Assert.Single(
                    actions,
                    a => a.ActionConstraints
                        .OfType<HttpMethodConstraint>()
                        .SelectMany(c => c.HttpMethods)
                        .Contains(method));

                Assert.NotNull(action.AttributeRouteInfo);
                Assert.Equal("Products/list", action.AttributeRouteInfo.Template);
            }
        }

        [Fact]
        public void AttributeRouting_ThrowsIfAttributeRoutedAndNonAttributedActions_OnTheSameMethod()
        {
            // Arrange
            var expectedMessage =
                "The following errors occurred with attribute routing information:" + Environment.NewLine +
                Environment.NewLine +
                "Error 1:" + Environment.NewLine +
                "A method 'Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+" +
                "AttributeAndNonAttributeRoutedActionsOnSameMethodController.Method'" +
                " must not define attribute routed actions and non attribute routed actions at the same time:" + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+" +
                "AttributeAndNonAttributeRoutedActionsOnSameMethodController.Method' - Route Template: 'AttributeRouted' - " +
                "HTTP Verbs: 'GET'" + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+" +
                "AttributeAndNonAttributeRoutedActionsOnSameMethodController.Method' - Route Template: '(none)' - " +
                "HTTP Verbs: 'DELETE, PATCH, POST, PUT'" + Environment.NewLine +
                Environment.NewLine +
                "Use 'AcceptVerbsAttribute' to create a single route that allows multiple HTTP verbs and defines a " +
                "route, or set a route template in all attributes that constrain HTTP verbs.";

            var provider = GetProvider(
                typeof(AttributeAndNonAttributeRoutedActionsOnSameMethodController).GetTypeInfo());

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => provider.GetDescriptors());

            // Assert
            VerifyMultiLineError(expectedMessage, exception.Message, unorderedStart: 1, unorderedLineCount: 2);
        }

        [Fact]
        public void AttributeRouting_RouteOnControllerAndAction_CreatesActionDescriptorWithoutHttpConstraints()
        {
            // Arrange
            var provider = GetProvider(typeof(OnlyRouteController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            var action = Assert.Single(actions);

            Assert.Equal("Action", action.Name);
            Assert.Equal("OnlyRoute", action.ControllerName);

            Assert.NotNull(action.AttributeRouteInfo);
            Assert.Equal("Products/Index", action.AttributeRouteInfo.Template);

            Assert.Null(action.ActionConstraints);
        }

        [Fact]
        public void AttributeRouting_Name_ThrowsIfMultipleActions_WithDifferentTemplatesHaveTheSameName()
        {
            // Arrange
            var provider = GetProvider(typeof(SameNameDifferentTemplatesController).GetTypeInfo());

            var expectedMessage =
                "The following errors occurred with attribute routing information:"
                + Environment.NewLine + Environment.NewLine +
                "Error 1:" + Environment.NewLine +
                "Attribute routes with the same name 'Products' must have the same template:"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.Get' - Template: 'Products'"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.Get' - Template: 'Products/{id}'"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.Put' - Template: 'Products/{id}'"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.Post' - Template: 'Products'"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.Delete' - Template: 'Products/{id}'"
                + Environment.NewLine + Environment.NewLine +
                "Error 2:" + Environment.NewLine +
                "Attribute routes with the same name 'Items' must have the same template:"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.GetItems' - Template: 'Items/{id}'"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.PostItems' - Template: 'Items'"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.PutItems' - Template: 'Items/{id}'"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.DeleteItems' - Template: 'Items/{id}'"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.PatchItems' - Template: 'Items'";

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => { provider.GetDescriptors(); });

            // Assert
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void AttributeRouting_Name_AllowsMultipleAttributeRoutesInDifferentActions_WithTheSameNameAndTemplate()
        {
            // Arrange
            var provider = GetProvider(typeof(DifferentCasingsAttributeRouteNamesController).GetTypeInfo());

            // Act
            var descriptors = provider.GetDescriptors();

            // Assert
            foreach (var descriptor in descriptors)
            {
                Assert.NotNull(descriptor.AttributeRouteInfo);
                Assert.Equal("{id}", descriptor.AttributeRouteInfo.Template, StringComparer.OrdinalIgnoreCase);
                Assert.Equal("Products", descriptor.AttributeRouteInfo.Name, StringComparer.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void AttributeRouting_RouteNameTokenReplace_AllowsMultipleActions_WithSameRouteNameTemplate()
        {
            // Arrange
            var provider = GetProvider(typeof(ActionRouteNameTemplatesController).GetTypeInfo());
            var editActionName = nameof(ActionRouteNameTemplatesController.Edit);
            var getActionName = nameof(ActionRouteNameTemplatesController.Get);

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            var getActions = actions.Where(a => a.Name.Equals(getActionName));
            Assert.Equal(2, getActions.Count());

            foreach (var getAction in getActions)
            {
                Assert.NotNull(getAction.AttributeRouteInfo);
                Assert.Equal("Products/Get", getAction.AttributeRouteInfo.Template, StringComparer.OrdinalIgnoreCase);
                Assert.Equal("Products_Get", getAction.AttributeRouteInfo.Name, StringComparer.OrdinalIgnoreCase);
            }

            var editAction = Assert.Single(actions, a => a.Name.Equals(editActionName));
            Assert.NotNull(editAction.AttributeRouteInfo);
            Assert.Equal("Products/Edit", editAction.AttributeRouteInfo.Template, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("Products_Edit", editAction.AttributeRouteInfo.Name, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void AttributeRouting_RouteNameTokenReplace_AreaControllerActionTokensInRoute()
        {
            // Arrange
            var provider = GetProvider(typeof(ControllerActionRouteNameTemplatesController).GetTypeInfo());
            var editActionName = nameof(ControllerActionRouteNameTemplatesController.Edit);
            var getActionName = nameof(ControllerActionRouteNameTemplatesController.Get);

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            var getActions = actions.Where(a => a.Name.Equals(getActionName));
            Assert.Equal(2, getActions.Count());

            foreach (var getAction in getActions)
            {
                Assert.NotNull(getAction.AttributeRouteInfo);
                Assert.Equal(
                    "ControllerActionRouteNameTemplates/Get",
                    getAction.AttributeRouteInfo.Template, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(
                    "Products_ControllerActionRouteNameTemplates_Get",
                    getAction.AttributeRouteInfo.Name, StringComparer.OrdinalIgnoreCase);
            }

            var editAction = Assert.Single(actions, a => a.Name.Equals(editActionName));
            Assert.NotNull(editAction.AttributeRouteInfo);
            Assert.Equal(
                "ControllerActionRouteNameTemplates/Edit",
                editAction.AttributeRouteInfo.Template, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(
                "Products_ControllerActionRouteNameTemplates_Edit",
                editAction.AttributeRouteInfo.Name, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void AttributeRouting_RouteNameTokenReplace_InvalidToken()
        {
            // Arrange
            var provider = GetProvider(typeof(RouteNameIncorrectTokenController).GetTypeInfo());

            var expectedMessage =
                "The following errors occurred with attribute routing information:" + Environment.NewLine +
                Environment.NewLine +
                "Error 1:" + Environment.NewLine +
                "For action: 'Microsoft.AspNet.Mvc.Test.ControllerActionDescriptorProviderTests+" +
                "RouteNameIncorrectTokenController.Get'" + Environment.NewLine +
                "Error: While processing template 'Products_[unknown]', a replacement value for the token 'unknown' " +
                "could not be found. Available tokens: 'action, controller'.";

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => { provider.GetDescriptors(); });
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void AttributeRouting_RouteGroupConstraint_IsAddedOnceForNonAttributeRoutes()
        {
            // Arrange
            var provider = GetProvider(
                typeof(ConventionalAndAttributeRoutedActionsWithAreaController).GetTypeInfo(),
                typeof(ConstrainedController).GetTypeInfo());

            // Act
            var actionDescriptors = provider.GetDescriptors();

            // Assert
            Assert.NotNull(actionDescriptors);
            Assert.Equal(4, actionDescriptors.Count());

            foreach (var actionDescriptor in actionDescriptors.Where(ad => ad.AttributeRouteInfo == null))
            {
                Assert.Equal(6, actionDescriptor.RouteConstraints.Count);
                var routeGroupConstraint = Assert.Single(actionDescriptor.RouteConstraints,
                    rc => rc.RouteKey.Equals(AttributeRouting.RouteGroupKey));
                Assert.Equal(RouteKeyHandling.DenyKey, routeGroupConstraint.KeyHandling);
            }
        }

        [Fact]
        public void AttributeRouting_AddsDefaultRouteValues_ForAttributeRoutedActions()
        {
            // Arrange
            var provider = GetProvider(
                typeof(ConventionalAndAttributeRoutedActionsWithAreaController).GetTypeInfo(),
                typeof(ConstrainedController).GetTypeInfo());

            // Act
            var actionDescriptors = provider.GetDescriptors();

            // Assert
            Assert.NotNull(actionDescriptors);
            Assert.Equal(4, actionDescriptors.Count());

            var indexAction = Assert.Single(actionDescriptors, ad => ad.Name.Equals("Index"));

            Assert.Equal(1, indexAction.RouteConstraints.Count);

            var routeGroupConstraint = Assert.Single(indexAction.RouteConstraints, rc => rc.RouteKey.Equals(AttributeRouting.RouteGroupKey));
            Assert.Equal(RouteKeyHandling.RequireKey, routeGroupConstraint.KeyHandling);
            Assert.NotNull(routeGroupConstraint.RouteValue);

            Assert.Equal(5, indexAction.RouteValueDefaults.Count);

            var controllerDefault = Assert.Single(indexAction.RouteValueDefaults, rd => rd.Key.Equals("controller", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("ConventionalAndAttributeRoutedActionsWithArea", controllerDefault.Value);

            var actionDefault = Assert.Single(indexAction.RouteValueDefaults, rd => rd.Key.Equals("action", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("Index", actionDefault.Value);

            var areaDefault = Assert.Single(indexAction.RouteValueDefaults, rd => rd.Key.Equals("area", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("Home", areaDefault.Value);

            var myRouteConstraintDefault = Assert.Single(indexAction.RouteValueDefaults, rd => rd.Key.Equals("key", StringComparison.OrdinalIgnoreCase));
            Assert.Null(myRouteConstraintDefault.Value);

            var anotherRouteConstraint = Assert.Single(indexAction.RouteValueDefaults, rd => rd.Key.Equals("second", StringComparison.OrdinalIgnoreCase));
            Assert.Null(anotherRouteConstraint.Value);
        }

        [Fact]
        public void AttributeRouting_TokenReplacement_CaseInsensitive()
        {
            // Arrange
            var provider = GetProvider(typeof(CaseInsensitiveController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            var action = Assert.Single(actions);
            Assert.Equal("stub/ThisIsAnAction", action.AttributeRouteInfo.Template);
        }

        // Token replacement happens before we 'group' routes. So two route templates
        // that are equivalent after token replacement go to the same 'group'.
        [Fact]
        public void AttributeRouting_TokenReplacement_BeforeGroupId()
        {
            // Arrange
            var provider = GetProvider(typeof(SameGroupIdController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors().ToArray();

            var groupIds = actions.Select(
                a => a.RouteConstraints
                    .Where(rc => rc.RouteKey == AttributeRouting.RouteGroupKey)
                    .Select(rc => rc.RouteValue)
                    .Single())
                .ToArray();

            // Assert
            Assert.Equal(2, groupIds.Length);
            Assert.Equal(groupIds[0], groupIds[1]);
        }

        // Parameters are validated later. This action uses the forbidden {action} and {controller}
        [Fact]
        public void AttributeRouting_DoesNotValidateParameters()
        {
            // Arrange
            var provider = GetProvider(typeof(InvalidParametersController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            var action = Assert.Single(actions);
            Assert.Equal("stub/{controller}/{action}", action.AttributeRouteInfo.Template);
        }

        [Fact]
        public void ApiExplorer_SetsExtensionData_WhenVisible()
        {
            // Arrange
            var provider = GetProvider(typeof(ApiExplorerVisibleController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            var action = Assert.Single(actions);
            Assert.NotNull(action.GetProperty<ApiDescriptionActionData>());
        }

        [Fact]
        public void ApiExplorer_SetsExtensionData_WhenVisible_CanOverrideControllerOnAction()
        {
            // Arrange
            var provider = GetProvider(typeof(ApiExplorerVisibilityOverrideController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            Assert.Equal(2, actions.Count());

            var action = Assert.Single(actions, a => a.Name == "Edit");
            Assert.NotNull(action.GetProperty<ApiDescriptionActionData>());

            action = Assert.Single(actions, a => a.Name == "Create");
            Assert.Null(action.GetProperty<ApiDescriptionActionData>());
        }

        [Theory]
        [InlineData(typeof(ApiExplorerNotVisibleController))]
        [InlineData(typeof(ApiExplorerExplicitlyNotVisibleController))]
        [InlineData(typeof(ApiExplorerExplicitlyNotVisibleOnActionController))]
        public void ApiExplorer_DoesNotSetExtensionData_WhenNotVisible(Type controllerType)
        {
            // Arrange
            var provider = GetProvider(controllerType.GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            var action = Assert.Single(actions);
            Assert.Null(action.GetProperty<ApiDescriptionActionData>());
        }

        [Fact]
        public void ApiExplorer_SetsName_DefaultToNull()
        {
            // Arrange
            var provider = GetProvider(typeof(ApiExplorerNoNameController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors();

            // Assert

            var action = Assert.Single(actions);
            Assert.Null(action.GetProperty<ApiDescriptionActionData>().GroupName);
        }

        [Fact]
        public void ApiExplorer_SetsName_OnController()
        {
            // Arrange
            var provider = GetProvider(typeof(ApiExplorerNameOnControllerController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors();

            // Assert

            var action = Assert.Single(actions);
            Assert.Equal("Store", action.GetProperty<ApiDescriptionActionData>().GroupName);
        }

        [Fact]
        public void ApiExplorer_SetsName_OnAction()
        {
            // Arrange
            var provider = GetProvider(typeof(ApiExplorerNameOnActionController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            var action = Assert.Single(actions);
            Assert.Equal("Blog", action.GetProperty<ApiDescriptionActionData>().GroupName);
        }

        [Fact]
        public void ApiExplorer_SetsName_CanOverrideControllerOnAction()
        {
            // Arrange
            var provider = GetProvider(typeof(ApiExplorerNameOverrideController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            Assert.Equal(2, actions.Count());

            var action = Assert.Single(actions, a => a.Name == "Edit");
            Assert.Equal("Blog", action.GetProperty<ApiDescriptionActionData>().GroupName);

            action = Assert.Single(actions, a => a.Name == "Create");
            Assert.Equal("Store", action.GetProperty<ApiDescriptionActionData>().GroupName);
        }

        [Fact]
        public void ApiExplorer_IsVisibleOnApplication_CanOverrideOnController()
        {
            // Arrange
            var convention = new ApiExplorerIsVisibleConvention(isVisible: true);
            var provider = GetProvider(typeof(ApiExplorerExplicitlyNotVisibleController).GetTypeInfo(), convention);

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            var action = Assert.Single(actions);
            Assert.Null(action.GetProperty<ApiDescriptionActionData>());
        }

        [Fact]
        public void ApiExplorer_IsVisibleOnApplication_CanOverrideOnAction()
        {
            // Arrange
            var convention = new ApiExplorerIsVisibleConvention(isVisible: true);
            var provider = GetProvider(
                typeof(ApiExplorerExplicitlyNotVisibleOnActionController).GetTypeInfo(),
                convention);

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            var action = Assert.Single(actions);
            Assert.Null(action.GetProperty<ApiDescriptionActionData>());
        }

        [Theory]
        [InlineData("A", typeof(ApiExplorerEnabledConventionalRoutedController))]
        [InlineData("A", typeof(ApiExplorerEnabledActionConventionalRoutedController))]
        public void ApiExplorer_ThrowsForContentionalRouting(string actionName, Type type)
        {
            var expected = string.Format(
                "The action '{0}.{1}' has ApiExplorer enabled, but is using conventional routing. " +
                "Only actions which use attribute routing support ApiExplorer.",
                type.FullName, actionName);

            // Arrange
            var provider = GetProvider(type.GetTypeInfo());

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => provider.GetDescriptors());
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public void ApiExplorer_SkipsConventionalRoutedController_WhenConfiguredOnApplication()
        {
            // Arrange
            var convention = new ApiExplorerIsVisibleConvention(isVisible: true);
            var provider = GetProvider(
                typeof(ConventionallyRoutedController).GetTypeInfo(),
                convention);

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            var action = Assert.Single(actions);
            Assert.Null(action.GetProperty<ApiDescriptionActionData>());
        }

        // Verifies the sequence of conventions running
        [Fact]
        public void ApplyConventions_RunsInOrderOfDecreasingScope()
        {
            // Arrange
            var sequence = 0;

            var applicationConvention = new Mock<IApplicationModelConvention>();
            applicationConvention
                .Setup(c => c.Apply(It.IsAny<ApplicationModel>()))
                .Callback(() => { Assert.Equal(0, sequence++); });

            var controllerConvention = new Mock<IControllerModelConvention>();
            controllerConvention
                .Setup(c => c.Apply(It.IsAny<ControllerModel>()))
                .Callback(() => { Assert.Equal(1, sequence++); });

            var actionConvention = new Mock<IActionModelConvention>();
            actionConvention
                .Setup(c => c.Apply(It.IsAny<ActionModel>()))
                .Callback(() => { Assert.Equal(2, sequence++); });

            var parameterConvention = new Mock<IParameterModelConvention>();
            parameterConvention
                .Setup(c => c.Apply(It.IsAny<ParameterModel>()))
                .Callback(() => { Assert.Equal(3, sequence++); });

            var options = new MockMvcOptionsAccessor();
            options.Options.Conventions.Add(applicationConvention.Object);


            var applicationModel = new ApplicationModel();

            var controller = new ControllerModel(typeof(ConventionsController).GetTypeInfo(),
                                                 new List<object>() { controllerConvention.Object });
            controller.Application = applicationModel;
            applicationModel.Controllers.Add(controller);

            var methodInfo = typeof(ConventionsController).GetMethod("Create");
            var actionModel = new ActionModel(methodInfo, new List<object>() { actionConvention.Object });
            actionModel.Controller = controller;
            controller.Actions.Add(actionModel);

            var parameterInfo = actionModel.ActionMethod.GetParameters().Single();
            var parameterModel = new ParameterModel(parameterInfo,
                                           new List<object>() { parameterConvention.Object });
            parameterModel.Action = actionModel;
            actionModel.Parameters.Add(parameterModel);

            // Act
            ApplicationModelConventions.ApplyConventions(applicationModel, options.Options.Conventions);

            // Assert
            Assert.Equal(4, sequence);
        }

        [Fact]
        public void BuildModel_SplitsConstraintsBasedOnRoute()
        {
            // Arrange
            var provider = GetProvider(typeof(MultipleRouteProviderOnActionController).GetTypeInfo());

            // Act
            var model = provider.BuildModel();

            // Assert
            var actions = Assert.Single(model.Controllers).Actions;
            Assert.Equal(2, actions.Count());

            var action = Assert.Single(actions, a => a.AttributeRouteModel.Template == "R1");

            Assert.Equal(2, action.Attributes.Count);
            Assert.Single(action.Attributes, a => a is RouteAndConstraintAttribute);
            Assert.Single(action.Attributes, a => a is ConstraintAttribute);

            Assert.Equal(2, action.ActionConstraints.Count);
            Assert.Single(action.ActionConstraints, a => a is RouteAndConstraintAttribute);
            Assert.Single(action.ActionConstraints, a => a is ConstraintAttribute);

            action = Assert.Single(actions, a => a.AttributeRouteModel.Template == "R2");

            Assert.Equal(2, action.Attributes.Count);
            Assert.Single(action.Attributes, a => a is RouteAndConstraintAttribute);
            Assert.Single(action.Attributes, a => a is ConstraintAttribute);

            Assert.Equal(2, action.ActionConstraints.Count);
            Assert.Single(action.ActionConstraints, a => a is RouteAndConstraintAttribute);
            Assert.Single(action.ActionConstraints, a => a is ConstraintAttribute);
        }

        [Fact]
        public void GetDescriptors_SplitsConstraintsBasedOnRoute()
        {
            // Arrange
            var provider = GetProvider(typeof(MultipleRouteProviderOnActionController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            Assert.Equal(2, actions.Count());

            var action = Assert.Single(actions, a => a.AttributeRouteInfo.Template == "R1");

            Assert.Equal(2, action.ActionConstraints.Count);
            Assert.Single(action.ActionConstraints, a => a is RouteAndConstraintAttribute);
            Assert.Single(action.ActionConstraints, a => a is ConstraintAttribute);

            action = Assert.Single(actions, a => a.AttributeRouteInfo.Template == "R2");

            Assert.Equal(2, action.ActionConstraints.Count);
            Assert.Single(action.ActionConstraints, a => a is RouteAndConstraintAttribute);
            Assert.Single(action.ActionConstraints, a => a is ConstraintAttribute);
        }

        [Fact]
        public void GetDescriptors_SplitsConstraintsBasedOnControllerRoute()
        {
            // Arrange
            var actionName = nameof(MultipleRouteProviderOnActionAndControllerController.Edit);
            var provider = GetProvider(typeof(MultipleRouteProviderOnActionAndControllerController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors().Where(a => a.Name == actionName);

            // Assert
            Assert.Equal(2, actions.Count());

            var action = Assert.Single(actions, a => a.AttributeRouteInfo.Template == "C1/A1");
            Assert.Equal(3, action.ActionConstraints.Count);
            Assert.Single(action.ActionConstraints, a => (a as RouteAndConstraintAttribute)?.Template == "C1");
            Assert.Single(action.ActionConstraints, a => (a as RouteAndConstraintAttribute)?.Template == "A1");
            Assert.Single(action.ActionConstraints, a => a is ConstraintAttribute);

            action = Assert.Single(actions, a => a.AttributeRouteInfo.Template == "C2/A1");
            Assert.Equal(3, action.ActionConstraints.Count);
            Assert.Single(action.ActionConstraints, a => (a as RouteAndConstraintAttribute)?.Template == "C2");
            Assert.Single(action.ActionConstraints, a => (a as RouteAndConstraintAttribute)?.Template == "A1");
            Assert.Single(action.ActionConstraints, a => a is ConstraintAttribute);
        }

        [Fact]
        public void GetDescriptors_SplitsConstraintsBasedOnControllerRoute_MultipleRoutesOnAction()
        {
            // Arrange
            var actionName = nameof(MultipleRouteProviderOnActionAndControllerController.Delete);
            var provider = GetProvider(typeof(MultipleRouteProviderOnActionAndControllerController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors().Where(a => a.Name == actionName);

            // Assert
            Assert.Equal(4, actions.Count());

            var action = Assert.Single(actions, a => a.AttributeRouteInfo.Template == "C1/A3");
            Assert.Equal(3, action.ActionConstraints.Count);
            Assert.Single(action.ActionConstraints, a => (a as RouteAndConstraintAttribute)?.Template == "C1");
            Assert.Single(action.ActionConstraints, a => (a as RouteAndConstraintAttribute)?.Template == "A3");
            Assert.Single(action.ActionConstraints, a => a is ConstraintAttribute);

            action = Assert.Single(actions, a => a.AttributeRouteInfo.Template == "C2/A3");
            Assert.Equal(3, action.ActionConstraints.Count);
            Assert.Single(action.ActionConstraints, a => (a as RouteAndConstraintAttribute)?.Template == "C2");
            Assert.Single(action.ActionConstraints, a => (a as RouteAndConstraintAttribute)?.Template == "A3");
            Assert.Single(action.ActionConstraints, a => a is ConstraintAttribute);

            action = Assert.Single(actions, a => a.AttributeRouteInfo.Template == "C1/A4");
            Assert.Equal(3, action.ActionConstraints.Count);
            Assert.Single(action.ActionConstraints, a => (a as RouteAndConstraintAttribute)?.Template == "C1");
            Assert.Single(action.ActionConstraints, a => (a as RouteAndConstraintAttribute)?.Template == "A4");
            Assert.Single(action.ActionConstraints, a => a is ConstraintAttribute);

            action = Assert.Single(actions, a => a.AttributeRouteInfo.Template == "C2/A4");
            Assert.Equal(3, action.ActionConstraints.Count);
            Assert.Single(action.ActionConstraints, a => (a as RouteAndConstraintAttribute)?.Template == "C2");
            Assert.Single(action.ActionConstraints, a => (a as RouteAndConstraintAttribute)?.Template == "A4");
            Assert.Single(action.ActionConstraints, a => a is ConstraintAttribute);
        }

        // This method overrides the route from the controller, and so doesn't inherit its metadata.
        [Fact]
        public void GetDescriptors_SplitsConstraintsBasedOnControllerRoute_Override()
        {
            // Arrange
            var actionName = nameof(MultipleRouteProviderOnActionAndControllerController.Create);
            var provider = GetProvider(typeof(MultipleRouteProviderOnActionAndControllerController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors().Where(a => a.Name == actionName);

            // Assert
            Assert.Equal(1, actions.Count());

            var action = Assert.Single(actions, a => a.AttributeRouteInfo.Template == "A2");
            Assert.Equal(2, action.ActionConstraints.Count);
            Assert.Single(action.ActionConstraints, a => (a as RouteAndConstraintAttribute)?.Template == "~/A2");
            Assert.Single(action.ActionConstraints, a => a is ConstraintAttribute);
        }

        private ControllerActionDescriptorProvider GetProvider(
            TypeInfo controllerTypeInfo,
            IEnumerable<IFilter> filters = null)
        {
            var controllerTypeProvider = new FixedSetControllerTypeProvider(new[] { controllerTypeInfo });
            var controllerModelBuilder = new DefaultControllerModelBuilder(new DefaultActionModelBuilder(null), null);

            var provider = new ControllerActionDescriptorProvider(
                controllerTypeProvider,
                controllerModelBuilder,
                new TestGlobalFilterProvider(filters),
                new MockMvcOptionsAccessor());

            return provider;
        }

        private ControllerActionDescriptorProvider GetProvider(
            params TypeInfo[] controllerTypeInfo)
        {
            var controllerTypeProvider = new FixedSetControllerTypeProvider(controllerTypeInfo);
            var controllerModelBuilder = new DefaultControllerModelBuilder(new DefaultActionModelBuilder(null), null);

            var provider = new ControllerActionDescriptorProvider(
                controllerTypeProvider,
                controllerModelBuilder,
                new TestGlobalFilterProvider(),
                new MockMvcOptionsAccessor());

            return provider;
        }

        private ControllerActionDescriptorProvider GetProvider(
            TypeInfo type,
            IApplicationModelConvention convention)
        {
            var controllerTypeProvider = new FixedSetControllerTypeProvider(new[] { type });
            var modelBuilder = new DefaultControllerModelBuilder(new DefaultActionModelBuilder(null), null);

            var options = new MockMvcOptionsAccessor();
            options.Options.Conventions.Add(convention);

            return new ControllerActionDescriptorProvider(
                controllerTypeProvider,
                modelBuilder,
                new TestGlobalFilterProvider(),
                options);
        }

        private IEnumerable<ActionDescriptor> GetDescriptors(params TypeInfo[] controllerTypeInfos)
        {
            var controllerTypeProvider = new FixedSetControllerTypeProvider(controllerTypeInfos);
            var modelBuilder = new DefaultControllerModelBuilder(new DefaultActionModelBuilder(null), null);

            var provider = new ControllerActionDescriptorProvider(
                controllerTypeProvider,
                modelBuilder,
                new TestGlobalFilterProvider(),
                new MockMvcOptionsAccessor());

            return provider.GetDescriptors();
        }

        private static void VerifyMultiLineError(
            string expectedMessage,
            string actualMessage,
            int unorderedStart,
            int unorderedLineCount)
        {
            var expectedLines = expectedMessage
                .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                .ToArray();

            var actualLines = actualMessage
                .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                .ToArray();

            for (var i = 0; i < unorderedStart; i++)
            {
                Assert.Equal(expectedLines[i], actualLines[i]);
            }

            var orderedExpectedLines = expectedLines
                .Skip(unorderedStart)
                .Take(unorderedLineCount)
                .OrderBy(l => l, StringComparer.Ordinal)
                .ToArray();

            var orderedActualLines = actualLines
                .Skip(unorderedStart)
                .Take(unorderedLineCount)
                .OrderBy(l => l, StringComparer.Ordinal)
                .ToArray();

            for (var i = 0; i < unorderedLineCount; i++)
            {
                Assert.Equal(orderedExpectedLines[i], orderedActualLines[i]);
            }

            for (var i = unorderedStart + unorderedLineCount; i < expectedLines.Length; i++)
            {
                Assert.Equal(expectedLines[i], actualLines[i]);
            }

            Assert.Equal(expectedLines.Length, actualLines.Length);
        }

        private class HttpMethodController
        {
            [HttpPost]
            public void OnlyPost()
            {
            }
        }

        [Route("Items")]
        private class AttributeRoutedHttpMethodController
        {
            [CustomHttpMethodConstraint("PUT", "PATCH")]
            public void PutOrPatch() { }
        }

        private class PersonController
        {
            public void GetPerson()
            { }

            [ActionName("ShowPeople")]
            public void ListPeople()
            { }

            [NonAction]
            public void NotAnAction()
            { }
        }

        private class MyRouteConstraintAttribute : RouteConstraintAttribute
        {
            public MyRouteConstraintAttribute(bool blockNonAttributedActions)
                : base("key", "value", blockNonAttributedActions)
            {
            }
        }

        private class MySecondRouteConstraintAttribute : RouteConstraintAttribute
        {
            public MySecondRouteConstraintAttribute(bool blockNonAttributedActions)
                : base("second", "value", blockNonAttributedActions)
            {
            }
        }

        [MyRouteConstraint(blockNonAttributedActions: true)]
        private class BlockNonAttributedActionsController
        {
            public void Edit()
            {
            }
        }

        [MyRouteConstraint(blockNonAttributedActions: false)]
        private class DontBlockNonAttributedActionsController
        {
            public void Create()
            {
            }
        }

        private class MyFilterAttribute : Attribute, IFilter
        {
            public MyFilterAttribute(int value)
            {
                Value = value;
            }

            public int Value { get; private set; }
        }

        [MyFilter(2)]
        private class FiltersController
        {
            [MyFilter(3)]
            public void FilterAction()
            {
            }
        }

        [Route("api/Token/[key]/[controller]")]
        [MyRouteConstraint(false)]
        private class TokenReplacementController
        {
            [HttpGet("stub/[action]")]
            public void ThisIsAnAction() { }
        }

        private class CaseInsensitiveController
        {
            [HttpGet("stub/[ActIon]")]
            public void ThisIsAnAction() { }
        }

        private class MultipleErrorsController
        {
            [HttpGet("stub/[action]/[unknown]")]
            public void Unknown() { }

            [HttpGet("[invalid/syntax")]
            public void Invalid() { }
        }

        private class InvalidParametersController
        {
            [HttpGet("stub/{controller}/{action}")]
            public void Action1() { }
        }

        private class SameGroupIdController
        {
            [HttpGet("stub/[action]")]
            public void Action1() { }

            [HttpGet("stub/Action1")]
            public void Action2() { }
        }

        [Area("Home")]
        private class ConventionalAndAttributeRoutedActionsWithAreaController
        {
            [HttpGet("Index")]
            public void Index() { }

            [HttpGet("Edit")]
            public void Edit() { }

            public void AnotherNonAttributedAction() { }
        }

        [Route("Products", Name = "Products")]
        private class SameNameDifferentTemplatesController
        {
            [HttpGet]
            public void Get() { }

            [HttpGet("{id}", Name = "Products")]
            public void Get(int id) { }

            [HttpPut("{id}", Name = "Products")]
            public void Put(int id) { }

            [HttpPost]
            public void Post() { }

            [HttpDelete("{id}", Name = "Products")]
            public void Delete(int id) { }

            [HttpGet("/Items/{id}", Name = "Items")]
            public void GetItems(int id) { }

            [HttpPost("/Items", Name = "Items")]
            public void PostItems() { }

            [HttpPut("/Items/{id}", Name = "Items")]
            public void PutItems(int id) { }

            [HttpDelete("/Items/{id}", Name = "Items")]
            public void DeleteItems(int id) { }

            [HttpPatch("/Items", Name = "Items")]
            public void PatchItems() { }
        }

        [Route("Products/[action]", Name = "Products_[action]")]
        private class ActionRouteNameTemplatesController
        {
            [HttpGet]
            public void Get() { }

            [HttpPost]
            public void Get(int id) { }

            public void Edit() { }
        }

        [Area("Products")]
        [Route("[controller]/[action]", Name = "[area]_[controller]_[action]")]
        private class ControllerActionRouteNameTemplatesController
        {
            [HttpGet]
            public void Get() { }

            [HttpPost]
            public void Get(int id) { }

            public void Edit() { }
        }

        [Route("Products/[action]", Name = "Products_[unknown]")]
        private class RouteNameIncorrectTokenController
        {
            public void Get() { }
        }

        private class DifferentCasingsAttributeRouteNamesController
        {
            [HttpGet("{id}", Name = "Products")]
            public void Get() { }

            [HttpGet("{ID}", Name = "Products")]
            public void Get(int id) { }

            [HttpPut("{id}", Name = "PRODUCTS")]
            public void Put(int id) { }

            [HttpDelete("{ID}", Order = 1, Name = "PRODUCTS")]
            public void Delete(int id) { }
        }

        [Route("v1")]
        [Route("v2")]
        public class MultiRouteAttributesController
        {
            [HttpGet("List")]
            [HttpGet("All")]
            public void MultipleHttpGet() { }

            [AcceptVerbs("POST", Route = "List")]
            public void AcceptVerbs() { }

            [AcceptVerbs("PUT", Route = "/Override")]
            public void AcceptVerbsOverride() { }

            [AcceptVerbs("POST")]
            [Route("List")]
            [HttpPut("All")]
            public void AcceptVerbsRouteAttributeAndHttpPut() { }

            [AcceptVerbs("POST", Route = "")]
            [Route("List")]
            [HttpPut("All")]
            public void AcceptVerbsRouteAttributeWithTemplateAndHttpPut() { }
        }

        [Route("Products")]
        public class OnlyRouteController
        {
            [Route("Index")]
            public void Action() { }
        }

        public class AttributeAndNonAttributeRoutedActionsOnSameMethodController
        {
            [HttpGet("AttributeRouted")]
            [HttpPost]
            [AcceptVerbs("PUT", "PATCH")]
            [CustomHttpMethodConstraint("DELETE")]
            public void Method() { }
        }

        [Route("Product")]
        [Route("/Product")]
        [Route("/product")]
        public class DuplicatedAttributeRouteController : Controller
        {
            [HttpGet("/List")]
            [HttpGet("/List")]
            public void Action() { }

            public void Controller() { }

            [HttpPut("list")]
            [PutOrPatch("list")]
            public void CommonHttpMethod() { }
        }

        [Route("Products")]
        public class NonDuplicatedAttributeRouteController : Controller
        {
            [HttpGet("list")]
            public void ControllerAndAction() { }

            [HttpGet("/PRODUCTS/LIST")]
            public void OverrideOnAction() { }

            [HttpGet("list")]
            [HttpPost("list")]
            [HttpPut("list")]
            [HttpPatch("list")]
            [HttpDelete("list")]
            public void DifferentHttpMethods() { }
        }

        [MyRouteConstraint(blockNonAttributedActions: true)]
        [MySecondRouteConstraint(blockNonAttributedActions: true)]
        private class ConstrainedController
        {
            public void ConstrainedNonAttributedAction() { }
        }

        private class ActionParametersController
        {
            public void RequiredInt(int id) { }

            public void FromBodyParameter([FromBody] TestActionParameter entity) { }

            public void NotFromBodyParameter(TestActionParameter entity) { }

            public void MultipleParameters(int id, [FromBody] TestActionParameter entity) { }

            public void DifferentCasing(int id, int ID, int Id) { }
        }

        private class ConventionallyRoutedController
        {
            public void ConventionalAction() { }
        }

        [Route("api")]
        private class AttributeRoutedController
        {
            [HttpGet("AttributeRoute")]
            public void AttributeRoutedAction() { }
        }

        private class EmptyController
        {
        }

        private class NonActionAttributeController
        {
            [NonAction]
            public void Action() { }
        }

        private class CustomHttpMethodConstraintAttribute : Attribute, IActionHttpMethodProvider
        {
            private readonly string[] _methods;

            public CustomHttpMethodConstraintAttribute(params string[] methods)
            {
                _methods = methods;
            }

            public IEnumerable<string> HttpMethods
            {
                get
                {
                    return _methods;
                }
            }
        }

        private class PutOrPatchAttribute : HttpMethodAttribute
        {
            private static readonly string[] _httpMethods = new string[] { "PUT", "PATCH" };

            public PutOrPatchAttribute(string template)
                : base(_httpMethods, template)
            {
            }
        }

        private class TestActionParameter
        {
            public int Id { get; set; }
            public int Name { get; set; }
        }

        [Route("AttributeRouting/IsRequired/ForApiExplorer")]
        private class ApiExplorerNotVisibleController
        {
            public void Edit() { }
        }

        [Route("AttributeRouting/IsRequired/ForApiExplorer")]
        [ApiExplorerSettings()]
        private class ApiExplorerVisibleController
        {
            public void Edit() { }
        }

        [Route("AttributeRouting/IsRequired/ForApiExplorer")]
        [ApiExplorerSettings(IgnoreApi = true)]
        private class ApiExplorerExplicitlyNotVisibleController
        {
            public void Edit() { }
        }

        [Route("AttributeRouting/IsRequired/ForApiExplorer")]
        private class ApiExplorerExplicitlyNotVisibleOnActionController
        {
            [ApiExplorerSettings(IgnoreApi = true)]
            public void Edit() { }
        }

        [Route("AttributeRouting/IsRequired/ForApiExplorer")]
        [ApiExplorerSettings(IgnoreApi = true)]
        private class ApiExplorerVisibilityOverrideController
        {
            [ApiExplorerSettings(IgnoreApi = false)]
            public void Edit() { }

            public void Create() { }
        }

        [Route("AttributeRouting/IsRequired/ForApiExplorer")]
        [ApiExplorerSettings(GroupName = "Store")]
        private class ApiExplorerNameOnControllerController
        {
            public void Edit() { }
        }

        [Route("AttributeRouting/IsRequired/ForApiExplorer")]
        private class ApiExplorerNameOnActionController
        {
            [ApiExplorerSettings(GroupName = "Blog")]
            public void Edit() { }
        }

        [Route("AttributeRouting/IsRequired/ForApiExplorer")]
        [ApiExplorerSettings()]
        private class ApiExplorerNoNameController
        {
            public void Edit() { }
        }

        [Route("AttributeRouting/IsRequired/ForApiExplorer")]
        [ApiExplorerSettings(GroupName = "Store")]
        private class ApiExplorerNameOverrideController
        {
            [ApiExplorerSettings(GroupName = "Blog")]
            public void Edit() { }

            public void Create() { }
        }

        private class ConventionsController
        {
            public void Create(int productId) { }
        }

        private class MultipleRouteProviderOnActionController
        {
            [Constraint]
            [RouteAndConstraint("R1")]
            [RouteAndConstraint("R2")]
            public void Edit() { }
        }

        [Constraint]
        [RouteAndConstraint("C1")]
        [RouteAndConstraint("C2")]
        private class MultipleRouteProviderOnActionAndControllerController
        {
            [RouteAndConstraint("A1")]
            public void Edit() { }

            [RouteAndConstraint("~/A2")]
            public void Create() { }

            [RouteAndConstraint("A3")]
            [RouteAndConstraint("A4")]
            public void Delete() { }
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
        private class RouteAndConstraintAttribute : Attribute, IActionConstraintMetadata, IRouteTemplateProvider
        {
            public RouteAndConstraintAttribute(string template)
            {
                Template = template;
            }

            public string Name { get; set; }

            public int? Order { get; set; }

            public string Template { get; private set; }
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
        private class ConstraintAttribute : Attribute, IActionConstraintMetadata
        {
        }

        [ApiExplorerSettings(GroupName = "Default")]
        private class ApiExplorerEnabledConventionalRoutedController : Controller
        {
            public void A()
            {
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        private class ApiExplorerEnabledActionConventionalRoutedController : Controller
        {
            [ApiExplorerSettings(GroupName = "Default")]
            public void A()
            {
            }
        }

        private class ApiExplorerIsVisibleConvention : IApplicationModelConvention
        {
            private bool _isVisible;

            public ApiExplorerIsVisibleConvention(bool isVisible)
            {
                _isVisible = isVisible;
            }

            public void Apply([NotNull] ApplicationModel application)
            {
                application.ApiExplorer.IsVisible = _isVisible;
            }
        }
    }
}
