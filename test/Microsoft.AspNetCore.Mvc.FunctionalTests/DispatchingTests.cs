// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class DispatchingTests : RoutingTestsBase<RoutingWebSite.StartupWithDispatching>
    {
        public DispatchingTests(MvcTestFixture<RoutingWebSite.StartupWithDispatching> fixture)
            : base(fixture)
        {
        }

        [Fact(Skip = "Conventional routing WIP")]
        public override Task ConventionalRoutedController_ActionIsReachable()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "Conventional routing WIP")]
        public override Task ConventionalRoutedController_ActionIsReachable_WithDefaults()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "Conventional routing WIP")]
        public override Task ConventionalRoutedController_NonActionIsNotReachable()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "Conventional routing WIP")]
        public override Task ConventionalRoutedController_InArea_ActionIsReachable()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "Conventional routing WIP")]
        public override Task ConventionalRoutedController_InArea_ActionBlockedByHttpMethod()
        {
            return Task.CompletedTask;
        }

        [Theory(Skip = "Conventional routing WIP")]
        [InlineData("", "/Home/OptionalPath/default")]
        [InlineData("CustomPath", "/Home/OptionalPath/CustomPath")]
        public override Task ConventionalRoutedController_WithOptionalSegment(string optionalSegment, string expected)
        {
            return Task.CompletedTask;
        }

        [Theory(Skip = "URL generation WIP")]
        [InlineData("http://localhost/api/v1/Maps")]
        [InlineData("http://localhost/api/v2/Maps")]
        public override Task AttributeRoutedAction_MultipleRouteAttributes_WorksWithNameAndOrder(string url)
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "URL generation WIP")]
        public override Task AttributeRoutedAction_MultipleRouteAttributes_WorksWithOverrideRoutes()
        {
            return Task.CompletedTask;
        }

        [Theory(Skip = "URL generation WIP")]
        [InlineData("http://localhost/api/v1/Maps/5", "PUT")]
        [InlineData("http://localhost/api/v2/Maps/5", "PUT")]
        [InlineData("http://localhost/api/v1/Maps/PartialUpdate/5", "PATCH")]
        [InlineData("http://localhost/api/v2/Maps/PartialUpdate/5", "PATCH")]
        public override Task AttributeRoutedAction_MultipleRouteAttributes_CombinesWithMultipleHttpAttributes(
            string url,
            string method)
        {
            return Task.CompletedTask;
        }

        [Theory(Skip = "URL generation WIP")]
        [InlineData("http://localhost/Banks/Get/5")]
        [InlineData("http://localhost/Bank/Get/5")]
        public override Task AttributeRoutedAction_MultipleHttpAttributesAndTokenReplacement(string url)
        {
            return Task.CompletedTask;
        }

        [Theory(Skip = "URL generation WIP")]
        [InlineData("PUT", "Bank")]
        [InlineData("PATCH", "Bank")]
        [InlineData("PUT", "Bank/Update")]
        [InlineData("PATCH", "Bank/Update")]
        public override Task AttributeRoutedAction_AcceptVerbsAndRouteTemplate_IsReachable(string verb, string path)
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "URL generation WIP")]
        public override Task AttributeRoutedAction_LinkGeneration_OverrideActionOverridesOrderOnController()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "URL generation WIP")]
        public override Task AttributeRoutedAction_LinkGeneration_OrderOnActionOverridesOrderOnController()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "URL generation WIP")]
        public override Task AttributeRoutedAction_LinkToSelf()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "URL generation WIP")]
        public override Task AttributeRoutedAction_LinkWithAmbientController()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "URL generation WIP")]
        public override Task AttributeRoutedAction_LinkToAttributeRoutedController()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "URL generation WIP")]
        public override Task AttributeRoutedAction_LinkToConventionalController()
        {
            return Task.CompletedTask;
        }

        [Theory(Skip = "URL generation WIP")]
        [InlineData("GET", "Get")]
        [InlineData("PUT", "Put")]
        public override Task AttributeRoutedAction_LinkWithName_WithNameInheritedFromControllerRoute(
            string method,
            string actionName)
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "URL generation WIP")]
        public override Task AttributeRoutedAction_LinkWithName_WithNameOverrridenFromController()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "URL generation WIP")]
        public override Task AttributeRoutedAction_Link_WithNonEmptyActionRouteTemplateAndNoActionRouteName()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "URL generation WIP")]
        public override Task AttributeRoutedAction_LinkWithName_WithNonEmptyActionRouteTemplateAndActionRouteName()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "Conventional routing WIP")]
        public override Task ConventionalRoutedAction_LinkToArea()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "Conventional routing WIP")]
        public override Task ConventionalRoutedAction_InArea_ImplicitLinkToArea()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "Conventional routing WIP")]
        public override Task ConventionalRoutedAction_InArea_ExplicitLeaveArea()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "Conventional routing WIP")]
        public override Task ConventionalRoutedAction_InArea_StaysInArea()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "URL generation WIP")]
        public override Task AttributeRoutedAction_LinkToArea()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "URL generation WIP")]
        public override Task AttributeRoutedAction_InArea_ImplicitLinkToArea()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "URL generation WIP")]
        public override Task AttributeRoutedAction_InArea_ExplicitLeaveArea()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "URL generation WIP")]
        public override Task AttributeRoutedAction_InArea_StaysInArea_ActionDoesntExist()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "URL generation WIP")]
        public override Task AttributeRoutedAction_InArea_LinkToConventionalRoutedActionInArea()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "Conventional routing WIP")]
        public override Task ConventionalRoutedAction_InArea_LinkToAttributeRoutedActionInArea()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "Conventional routing WIP")]
        public override Task ConventionalRoutedAction_InArea_LinkToAnotherArea()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "URL generation WIP")]
        public override Task AttributeRoutedAction_InArea_LinkToAnotherArea()
        {
            return Task.CompletedTask;
        }
    }
}
