// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Mvc.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Test
{
    public class ReflectedActionDescriptorProviderTests
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

        [Theory]
        [InlineData(typeof(HttpMethodController), nameof(HttpMethodController.OnlyPost), "POST")]
        [InlineData(typeof(AttributeRoutedHttpMethodController), nameof(AttributeRoutedHttpMethodController.PutOrPatch), "PUT,PATCH")]
        public void GetDescriptors_AddsHttpMethodConstraints(Type controllerType, string actionName, string expectedMethods)
        {
            // Arrange
            var provider = GetProvider(controllerType.GetTypeInfo());

            // Act
            var descriptors = provider.GetDescriptors();
            var descriptor = Assert.Single(descriptors);

            // Assert
            Assert.Equal(actionName, descriptor.Name);

            Assert.Single(descriptor.MethodConstraints);
            Assert.Equal(expectedMethods.Split(','), descriptor.MethodConstraints[0].HttpMethods);
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
            Assert.False(id.IsOptional);
            Assert.Null(id.BodyParameterInfo);

            Assert.NotNull(id.ParameterBindingInfo);
            Assert.Equal("id", id.ParameterBindingInfo.Prefix);
            Assert.Equal(typeof(int), id.ParameterBindingInfo.ParameterType);
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
            Assert.False(id.IsOptional);
            Assert.Null(id.BodyParameterInfo);

            Assert.NotNull(id.ParameterBindingInfo);
            Assert.Equal("id", id.ParameterBindingInfo.Prefix);
            Assert.Equal(typeof(int), id.ParameterBindingInfo.ParameterType);

            var entity = Assert.Single(main.Parameters, p => p.Name == "entity");

            Assert.Equal("entity", entity.Name);
            Assert.False(entity.IsOptional);
            Assert.Null(entity.ParameterBindingInfo);

            Assert.NotNull(entity.BodyParameterInfo);
            Assert.Equal(typeof(TestActionParameter), entity.BodyParameterInfo.ParameterType);
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
            Assert.False(id.IsOptional);
            Assert.Null(id.BodyParameterInfo);

            Assert.NotNull(id.ParameterBindingInfo);
            Assert.Equal("id", id.ParameterBindingInfo.Prefix);
            Assert.Equal(typeof(int), id.ParameterBindingInfo.ParameterType);

            var upperCaseId = Assert.Single(main.Parameters, p => p.Name == "ID");

            Assert.Equal("ID", upperCaseId.Name);
            Assert.False(upperCaseId.IsOptional);
            Assert.Null(upperCaseId.BodyParameterInfo);

            Assert.NotNull(upperCaseId.ParameterBindingInfo);
            Assert.Equal("ID", upperCaseId.ParameterBindingInfo.Prefix);
            Assert.Equal(typeof(int), upperCaseId.ParameterBindingInfo.ParameterType);

            var pascalCaseId = Assert.Single(main.Parameters, p => p.Name == "Id");

            Assert.Equal("Id", pascalCaseId.Name);
            Assert.False(pascalCaseId.IsOptional);
            Assert.Null(pascalCaseId.BodyParameterInfo);

            Assert.NotNull(pascalCaseId.ParameterBindingInfo);
            Assert.Equal("Id", pascalCaseId.ParameterBindingInfo.Prefix);
            Assert.Equal(typeof(int), pascalCaseId.ParameterBindingInfo.ParameterType);
        }

        [Theory]
        [InlineData(nameof(ActionParametersController.OptionalInt), typeof(Nullable<int>))]
        [InlineData(nameof(ActionParametersController.OptionalChar), typeof(char))]
        public void GetDescriptors_AddsParametersWithDefaultValues_AsOptionalParameters(
            string actionName,
            Type parameterType)
        {
            // Arrange & Act
            var descriptors = GetDescriptors(
                typeof(ActionParametersController).GetTypeInfo());

            // Assert
            var optional = Assert.Single(descriptors,
                d => d.Name.Equals(actionName));

            Assert.NotNull(optional.Parameters);
            var id = Assert.Single(optional.Parameters);

            Assert.Equal("id", id.Name);
            Assert.True(id.IsOptional);
            Assert.Null(id.BodyParameterInfo);

            Assert.NotNull(id.ParameterBindingInfo);
            Assert.Equal("id", id.ParameterBindingInfo.Prefix);
            Assert.Equal(parameterType, id.ParameterBindingInfo.ParameterType);
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
            Assert.False(entity.IsOptional);

            Assert.NotNull(entity.BodyParameterInfo);
            Assert.Equal(typeof(TestActionParameter), entity.BodyParameterInfo.ParameterType);

            Assert.Null(entity.ParameterBindingInfo);
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
            Assert.False(entity.IsOptional);
            Assert.Null(entity.BodyParameterInfo);

            Assert.NotNull(entity.ParameterBindingInfo);
            Assert.Equal("entity", entity.ParameterBindingInfo.Prefix);
            Assert.Equal(typeof(TestActionParameter), entity.ParameterBindingInfo.ParameterType);
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
                    c.RouteValue == null &&
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
        public void BuildModel_CreatesReflectedControllerModels_ForAllControllers()
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
            Assert.Null(conventional.AttributeRouteModel);
            Assert.Single(conventional.Actions);

            var attributeRouted = Assert.Single(model.Controllers,
                c => c.ControllerName == "AttributeRouted");
            Assert.Single(attributeRouted.Actions);
            Assert.NotNull(attributeRouted.AttributeRouteModel);

            var empty = Assert.Single(model.Controllers,
                c => c.ControllerName == "Empty");
            Assert.Empty(empty.Actions);

            var nonAction = Assert.Single(model.Controllers,
                c => c.ControllerName == "NonActionAttribute");
            Assert.Empty(nonAction.Actions);
        }

        [Fact]
        public void BuildModel_CreatesReflectedActionDescriptors_ForValidActions()
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
            Assert.True(getPerson.IsActionNameMatchRequired);

            var showPeople = Assert.Single(controller.Actions, a => a.ActionName == "ShowPeople");
            Assert.Empty(showPeople.HttpMethods);
            Assert.True(showPeople.IsActionNameMatchRequired);
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
                    "Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+PersonController.GetPerson",
                    "Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+PersonController.ListPeople",
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
            Assert.Equal("api/Token/[key]/[controller]", controller.AttributeRouteModel.Template);

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
                "For action: 'Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+" +
                "MultipleErrorsController.Unknown'" + Environment.NewLine +
                "Error: While processing template 'stub/[action]/[unknown]', a replacement value for the token 'unknown' " +
                "could not be found. Available tokens: 'controller, action'." + Environment.NewLine +
                Environment.NewLine +
                "For action: 'Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+" +
                "MultipleErrorsController.Invalid'" + Environment.NewLine +
                "Error: The route template '[invalid/syntax' has invalid syntax. A replacement token is not closed.";

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => { provider.GetDescriptors(); });

            // Assert
            Assert.Equal(expectedMessage, ex.Message);
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

            Assert.Null(action.MethodConstraints);
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
                "Action: 'Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.Get' - Template: 'Products'"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.Get' - Template: 'Products/{id}'"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.Put' - Template: 'Products/{id}'"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.Post' - Template: 'Products'"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.Delete' - Template: 'Products/{id}'"
                + Environment.NewLine + Environment.NewLine +
                "Error 2:" + Environment.NewLine +
                "Attribute routes with the same name 'Items' must have the same template:"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.GetItems' - Template: 'Items/{id}'"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.PostItems' - Template: 'Items'"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.PutItems' - Template: 'Items/{id}'"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+" +
                "SameNameDifferentTemplatesController.DeleteItems' - Template: 'Items/{id}'"
                + Environment.NewLine +
                "Action: 'Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+" +
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

        private ReflectedActionDescriptorProvider GetProvider(
            TypeInfo controllerTypeInfo,
            IEnumerable<IFilter> filters = null)
        {
            var conventions = new StaticActionDiscoveryConventions(controllerTypeInfo);

            var assemblyProvider = new Mock<IControllerAssemblyProvider>();
            assemblyProvider
                .SetupGet(ap => ap.CandidateAssemblies)
                .Returns(new Assembly[] { controllerTypeInfo.Assembly });

            var provider = new ReflectedActionDescriptorProvider(
                assemblyProvider.Object,
                conventions,
                filters,
                new MockMvcOptionsAccessor(),
                Mock.Of<IInlineConstraintResolver>());

            return provider;
        }

        private ReflectedActionDescriptorProvider GetProvider(
            params TypeInfo[] controllerTypeInfo)
        {
            var conventions = new StaticActionDiscoveryConventions(controllerTypeInfo);

            var assemblyProvider = new Mock<IControllerAssemblyProvider>();
            assemblyProvider
                .SetupGet(ap => ap.CandidateAssemblies)
                .Returns(new Assembly[] { controllerTypeInfo.First().Assembly });

            var provider = new ReflectedActionDescriptorProvider(
                assemblyProvider.Object,
                conventions,
                Enumerable.Empty<IFilter>(),
                new MockMvcOptionsAccessor(),
                Mock.Of<IInlineConstraintResolver>());

            return provider;
        }

        private IEnumerable<ActionDescriptor> GetDescriptors(params TypeInfo[] controllerTypeInfos)
        {
            var conventions = new StaticActionDiscoveryConventions(controllerTypeInfos);

            var assemblyProvider = new Mock<IControllerAssemblyProvider>();
            assemblyProvider
                .SetupGet(ap => ap.CandidateAssemblies)
                .Returns(controllerTypeInfos.Select(cti => cti.Assembly).Distinct());

            var provider = new ReflectedActionDescriptorProvider(
                assemblyProvider.Object,
                conventions,
                null,
                new MockMvcOptionsAccessor(),
                null);

            return provider.GetDescriptors();
        }

        private class HttpMethodController
        {
            [HttpPost]
            public void OnlyPost()
            {
            }
        }

        [Route("Products")]
        private class AttributeRoutedHttpMethodController
        {
            [AcceptVerbs("PUT", "PATCH")]
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

        [Route("Products")]
        public class OnlyRouteController
        {
            [Route("Index")]
            public void Action() { }
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

            public void OptionalInt(int? id = 5) { }

            public void OptionalChar(char id = 'c') { }

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
        private class AttributeRoutedController()
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

        private class TestActionParameter
        {
            public int Id { get; set; }
            public int Name { get; set; }
        }
    }
}
