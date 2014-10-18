// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Web.Http;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Routing;
using Xunit;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public class OverloadActionConstraintTest
    {
        [Fact]
        public void Accept_RejectsActionMatchWithMissingParameter()
        {
            // Arrange
            var action = new ActionDescriptor();
            action.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "id",
                    ParameterType = typeof(int),
                },
            };

            var constraint = new OverloadActionConstraint();

            var context = new ActionConstraintContext();
            context.Candidates = new List<ActionSelectorCandidate>()
            {
                new ActionSelectorCandidate(action, new [] { constraint }),
            };

            context.CurrentCandidate = context.Candidates[0];
            context.RouteContext = CreateRouteContext();

            // Act & Assert
            Assert.False(constraint.Accept(context));
        }

        [Fact]
        public void Accept_AcceptsActionWithSatisfiedParameters()
        {
            // Arrange
            var action = new ActionDescriptor();
            action.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "id",
                    ParameterType = typeof(int),
                },
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "quantity",
                    ParameterType = typeof(int),
                },
            };

            var constraint = new OverloadActionConstraint();

            var context = new ActionConstraintContext();
            context.Candidates = new List<ActionSelectorCandidate>()
            {
                new ActionSelectorCandidate(action, new [] { constraint }),
            };

            context.CurrentCandidate = context.Candidates[0];
            context.RouteContext = CreateRouteContext("?quantity=5", new { id = 17 });

            // Act & Assert
            Assert.True(constraint.Accept(context));
        }

        [Fact]
        public void Accept_AcceptsActionWithSatisfiedParameters_QueryStringOnly()
        {
            // Arrange
            var action = new ActionDescriptor();
            action.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "id",
                    ParameterType = typeof(int),
                },
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "quantity",
                    ParameterType = typeof(int),
                },
            };

            var constraint = new OverloadActionConstraint();

            var context = new ActionConstraintContext();
            context.Candidates = new List<ActionSelectorCandidate>()
            {
                new ActionSelectorCandidate(action, new [] { constraint }),
            };

            context.CurrentCandidate = context.Candidates[0];
            context.RouteContext = CreateRouteContext("?quantity=5&id=7", new { });

            // Act & Assert
            Assert.True(constraint.Accept(context));
        }

        [Fact]
        public void Accept_AcceptsActionWithSatisfiedParameters_RouteDataOnly()
        {
            // Arrange
            var action = new ActionDescriptor();
            action.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "id",
                    ParameterType = typeof(int),
                },
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "quantity",
                    ParameterType = typeof(int),
                },
            };

            var constraint = new OverloadActionConstraint();

            var context = new ActionConstraintContext();
            context.Candidates = new List<ActionSelectorCandidate>()
            {
                new ActionSelectorCandidate(action, new [] { constraint }),
            };

            context.CurrentCandidate = context.Candidates[0];
            context.RouteContext = CreateRouteContext("?", new { quantity = 9, id = 17 });

            // Act & Assert
            Assert.True(constraint.Accept(context));
        }

        [Fact]
        public void Accept_AcceptsActionWithUnsatisfiedOptionalParameter()
        {
            // Arrange
            var action = new ActionDescriptor();
            action.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "id",
                    ParameterType = typeof(int),
                },
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "quantity",
                    IsOptional = true,
                    ParameterType = typeof(int),
                },
            };

            var constraint = new OverloadActionConstraint();

            var context = new ActionConstraintContext();
            context.Candidates = new List<ActionSelectorCandidate>()
            {
                new ActionSelectorCandidate(action, new [] { constraint }),
            };

            context.CurrentCandidate = context.Candidates[0];
            context.RouteContext = CreateRouteContext("?store=5", new { id = 17 });

            // Act & Assert
            Assert.True(constraint.Accept(context));
        }

        [Fact]
        public void Accept_AcceptsOneAndRejectsAnother()
        {
            // Arrange
            var action1 = new ActionDescriptor();
            action1.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "id",
                    ParameterType = typeof(int),
                },
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "quantity",
                    ParameterType = typeof(int),
                },
            };

            var action2 = new ActionDescriptor();
            action2.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "id",
                    ParameterType = typeof(int),
                },
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "quantity_ordered",
                    ParameterType = typeof(int),
                },
            };

            var constraint = new OverloadActionConstraint();

            var context = new ActionConstraintContext();
            context.Candidates = new List<ActionSelectorCandidate>()
            {
                new ActionSelectorCandidate(action1, new [] { constraint }),
                new ActionSelectorCandidate(action2, new [] { constraint }),
            };

            context.CurrentCandidate = context.Candidates[0];
            context.RouteContext = CreateRouteContext("?quantity=5", new { id = 17 });

            // Act & Assert
            Assert.True(constraint.Accept(context));

            context.CurrentCandidate = context.Candidates[1];
            Assert.False(constraint.Accept(context));
        }

        [Fact]
        public void Accept_RejectsWorseMatch()
        {
            // Arrange
            var action1 = new ActionDescriptor();
            action1.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "id",
                    ParameterType = typeof(int),
                },
            };

            var action2 = new ActionDescriptor();
            action2.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "id",
                    ParameterType = typeof(int),
                },
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "quantity",
                    ParameterType = typeof(int),
                },
            };

            var constraint = new OverloadActionConstraint();

            var context = new ActionConstraintContext();
            context.Candidates = new List<ActionSelectorCandidate>()
            {
                new ActionSelectorCandidate(action1, new [] { constraint }),
                new ActionSelectorCandidate(action2, new [] { constraint }),
            };

            context.CurrentCandidate = context.Candidates[0];
            context.RouteContext = CreateRouteContext("?quantity=5", new { id = 17 });

            // Act & Assert
            Assert.False(constraint.Accept(context));
        }

        [Fact]
        public void Accept_RejectsWorseMatch_OptionalParameter()
        {
            // Arrange
            var action1 = new ActionDescriptor();
            action1.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "id",
                    ParameterType = typeof(int),
                },
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "quantity",
                    IsOptional = true,
                    ParameterType = typeof(int),
                },
            };

            var action2 = new ActionDescriptor();
            action2.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "id",
                    ParameterType = typeof(int),
                },
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "quantity",
                    ParameterType = typeof(int),
                },
            };

            var constraint = new OverloadActionConstraint();

            var context = new ActionConstraintContext();
            context.Candidates = new List<ActionSelectorCandidate>()
            {
                new ActionSelectorCandidate(action1, new [] { constraint }),
                new ActionSelectorCandidate(action2, new [] { constraint }),
            };

            context.CurrentCandidate = context.Candidates[0];
            context.RouteContext = CreateRouteContext("?quantity=5", new { id = 17 });

            // Act & Assert
            Assert.False(constraint.Accept(context));
        }

        [Fact]
        public void Accept_AcceptsActionsOnSameTier()
        {
            // Arrange
            var action1 = new ActionDescriptor();
            action1.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "id",
                    ParameterType = typeof(int),
                },
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "quantity",
                    ParameterType = typeof(int),
                },
            };

            var action2 = new ActionDescriptor();
            action2.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "id",
                    ParameterType = typeof(int),
                },
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "price",
                    ParameterType = typeof(decimal),
                },
            };

            var constraint = new OverloadActionConstraint();

            var context = new ActionConstraintContext();
            context.Candidates = new List<ActionSelectorCandidate>()
            {
                new ActionSelectorCandidate(action1, new [] { constraint }),
                new ActionSelectorCandidate(action2, new [] { constraint }),
            };

            context.CurrentCandidate = context.Candidates[0];
            context.RouteContext = CreateRouteContext("?quantity=5&price=5.99", new { id = 17 });

            // Act & Assert
            Assert.True(constraint.Accept(context));

            context.CurrentCandidate = context.Candidates[1];
            Assert.True(constraint.Accept(context));
        }

        [Fact]
        public void Accept_AcceptsAction_WithFewerParameters_WhenOtherIsNotOverloaded()
        {
            // Arrange
            var action1 = new ActionDescriptor();
            action1.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                     BinderMetadata = new FromUriAttribute(),
                    Name = "id",
                    ParameterType = typeof(int),
                },
            };

            var action2 = new ActionDescriptor();
            action2.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "id",
                    ParameterType = typeof(int),
                },
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    IsOptional = true,
                    Name = "quantity",
                    ParameterType = typeof(int),
                },
            };

            var constraint = new OverloadActionConstraint();

            var context = new ActionConstraintContext();
            context.Candidates = new List<ActionSelectorCandidate>()
            {
                new ActionSelectorCandidate(action1, new [] { constraint }),
                new ActionSelectorCandidate(action2, new IActionConstraint[] { }),
            };

            context.CurrentCandidate = context.Candidates[0];
            context.RouteContext = CreateRouteContext("?quantity=5", new { id = 17 });

            // Act & Assert
            Assert.True(constraint.Accept(context));
        }

        [Fact]
        public void Accept_AcceptsAction_WithFewerParameters_WhenOtherIsNotOverloaded_FromBodyAttribute()
        {
            // Arrange
            var action1 = new ActionDescriptor();
            action1.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                     BinderMetadata = new FromUriAttribute(),
                    Name = "id",
                    ParameterType = typeof(int),
                },
            };

            var action2 = new ActionDescriptor();
            action2.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromUriAttribute(),
                    Name = "id",
                    ParameterType = typeof(int),
                },
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromBodyAttribute(),
                    Name = "quantity",
                    ParameterType = typeof(int),
                },
            };

            var constraint = new OverloadActionConstraint();

            var context = new ActionConstraintContext();
            context.Candidates = new List<ActionSelectorCandidate>()
            {
                new ActionSelectorCandidate(action1, new [] { constraint }),
                new ActionSelectorCandidate(action2, new IActionConstraint[] { }),
            };

            context.CurrentCandidate = context.Candidates[0];
            context.RouteContext = CreateRouteContext("?quantity=5", new { id = 17 });

            // Act & Assert
            Assert.True(constraint.Accept(context));
        }

        private static RouteContext CreateRouteContext(string queryString = null, object routeValues = null)
        {
            var httpContext = new DefaultHttpContext();
            if (queryString != null)
            {
                httpContext.Request.QueryString = new QueryString(queryString);
            }

            var routeContext = new RouteContext(httpContext);
            routeContext.RouteData = new RouteData()
            {
                Values = new RouteValueDictionary(routeValues),
            };

            return routeContext;
        }
    }
}