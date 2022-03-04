// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    public class ClientErrorResultFilterConventionTest
    {
        [Fact]
        public void Apply_AddsFilter()
        {
            // Arrange
            var action = GetActionModel();
            var convention = GetConvention();

            // Act
            convention.Apply(action);

            // Assert
            Assert.Single(action.Filters.OfType<ClientErrorResultFilterFactory>());
        }

        private ClientErrorResultFilterConvention GetConvention()
        {
            return new ClientErrorResultFilterConvention();
        }

        private static ActionModel GetActionModel()
        {
            var action = new ActionModel(typeof(object).GetMethods()[0], new object[0]);

            return action;
        }
    }
}
