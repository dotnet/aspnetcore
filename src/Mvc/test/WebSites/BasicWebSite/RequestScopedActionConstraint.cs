// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace BasicWebSite;

// Only matches when the requestId is the same as the one passed in the constructor.
public class RequestScopedConstraintAttribute : Attribute, IActionConstraintFactory
{
    private readonly string _requestId;

    public bool IsReusable => false;

    public RequestScopedConstraintAttribute(string requestId)
    {
        _requestId = requestId;
    }

    IActionConstraint IActionConstraintFactory.CreateInstance(IServiceProvider services)
    {
        return CreateInstanceCore(services);
    }

    private Constraint CreateInstanceCore(IServiceProvider services)
    {
        var constraintType = typeof(Constraint);
        return (Constraint)ActivatorUtilities.CreateInstance(services, typeof(Constraint), new[] { _requestId });
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

        bool IActionConstraint.Accept(ActionConstraintContext context)
        {
            return AcceptCore();
        }

        private bool AcceptCore()
        {
            return _requestId == _requestIdService.RequestId;
        }
    }
}
