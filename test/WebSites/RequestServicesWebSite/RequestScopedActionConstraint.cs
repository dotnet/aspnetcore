// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;

namespace RequestServicesWebSite
{
    // Only matches when the requestId is the same as the one passed in the constructor.
    public class RequestScopedActionConstraintAttribute : Attribute, IActionConstraintFactory
    {
        private readonly string _requestId;

        public RequestScopedActionConstraintAttribute(string requestId)
        {
            _requestId = requestId;
        }

        public IActionConstraint CreateInstance(IServiceProvider services)
        {
            var activator = services.GetService<ITypeActivator>();
            return activator.CreateInstance<Constraint>(services, _requestId);
        }

        private class Constraint : IActionConstraint
        {
            private readonly RequestIdService _requestIdService;
            private readonly string _requestId;

            public Constraint(RequestIdService requestIdService, string requestId)
            {
                _requestIdService = requestIdService;
                _requestId = requestId;
            }

            public int Order { get; private set; }

            public bool Accept(ActionConstraintContext context)
            {
                return _requestId == _requestIdService.RequestId;
            }
        }
    }
}