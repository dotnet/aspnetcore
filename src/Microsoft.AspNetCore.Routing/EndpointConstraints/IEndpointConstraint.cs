// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.EndpointConstraints
{
    public class EndpointConstraintContext
    {
        public IReadOnlyList<EndpointSelectorCandidate> Candidates { get; set; }

        public EndpointSelectorCandidate CurrentCandidate { get; set; }

        public HttpContext HttpContext { get; set; }
    }

    public interface IEndpointConstraint : IEndpointConstraintMetadata
    {
        int Order { get; }

        bool Accept(EndpointConstraintContext context);
    }

    public interface IEndpointConstraintMetadata
    {
    }

    public readonly struct EndpointSelectorCandidate
    {
        public EndpointSelectorCandidate(
            Endpoint endpoint,
            int score,
            RouteValueDictionary values,
            IReadOnlyList<IEndpointConstraint> constraints)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            Endpoint = endpoint;
            Score = score;
            Values = values;
            Constraints = constraints;
        }

        // Temporarily added to not break MVC build
        public EndpointSelectorCandidate(
            Endpoint endpoint,
            IReadOnlyList<IEndpointConstraint> constraints)
        {
            throw new NotSupportedException();
        }

        public Endpoint Endpoint { get; }

        public int Score { get; }

        public RouteValueDictionary Values { get; }

        public IReadOnlyList<IEndpointConstraint> Constraints { get; }
    }

    public class EndpointConstraintItem
    {
        public EndpointConstraintItem(IEndpointConstraintMetadata metadata)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            Metadata = metadata;
        }

        public IEndpointConstraint Constraint { get; set; }

        public IEndpointConstraintMetadata Metadata { get; }

        public bool IsReusable { get; set; }
    }

    public interface IEndpointConstraintProvider
    {
        int Order { get; }

        void OnProvidersExecuting(EndpointConstraintProviderContext context);

        void OnProvidersExecuted(EndpointConstraintProviderContext context);
    }

    public class EndpointConstraintProviderContext
    {
        public EndpointConstraintProviderContext(
            HttpContext context,
            Endpoint endpoint,
            IList<EndpointConstraintItem> items)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            HttpContext = context;
            Endpoint = endpoint;
            Results = items;
        }

        public HttpContext HttpContext { get; }

        public Endpoint Endpoint { get; }

        public IList<EndpointConstraintItem> Results { get; }
    }

    public class DefaultEndpointConstraintProvider : IEndpointConstraintProvider
    {
        /// <inheritdoc />
        public int Order => -1000;

        /// <inheritdoc />
        public void OnProvidersExecuting(EndpointConstraintProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            for (var i = 0; i < context.Results.Count; i++)
            {
                ProvideConstraint(context.Results[i], context.HttpContext.RequestServices);
            }
        }

        /// <inheritdoc />
        public void OnProvidersExecuted(EndpointConstraintProviderContext context)
        {
        }

        private void ProvideConstraint(EndpointConstraintItem item, IServiceProvider services)
        {
            // Don't overwrite anything that was done by a previous provider.
            if (item.Constraint != null)
            {
                return;
            }

            if (item.Metadata is IEndpointConstraint constraint)
            {
                item.Constraint = constraint;
                item.IsReusable = true;
                return;
            }

            if (item.Metadata is IEndpointConstraintFactory factory)
            {
                item.Constraint = factory.CreateInstance(services);
                item.IsReusable = factory.IsReusable;
                return;
            }
        }
    }

    public interface IEndpointConstraintFactory : IEndpointConstraintMetadata
    {
        bool IsReusable { get; }

        IEndpointConstraint CreateInstance(IServiceProvider services);
    }
}