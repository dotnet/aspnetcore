// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http;

internal sealed class RequestDelegateFactoryContext
{
    // Options
    public required IServiceProvider ServiceProvider { get; init; }
    public required IServiceProviderIsService? ServiceProviderIsService { get; init; }
    public required IEnumerable<string>? RouteParameters { get; init; }
    public required bool ThrowOnBadRequest { get; init; }
    public required bool DisableInferredFromBody { get; init; }
    public required EndpointBuilder EndpointBuilder { get; init; }

    // Handler could be null if the MethodInfo overload of RDF.Create is used, but that doesn't matter because this is
    // only referenced to optimize certain cases where a RequestDelegate is the handler and filters don't modify it.
    public Delegate? Handler { get; set; }

    // Temporary State

    // This indicates whether we're currently in RDF.Create() with a non-null RequestDelegateResult.
    // This is settable, because if this context is cached we need to set it to true after it's created.
    // But it's still possible this should be initialized to true initially, so we're making it required.
    // In theory, someone could construct their own RequestDelegateResult without a cached context.
    public required bool MetadataAlreadyInferred { get; set; }

    public ParameterInfo? JsonRequestBodyParameter { get; set; }
    public bool AllowEmptyRequestBody { get; set; }

    public bool UsingTempSourceString { get; set; }
    public List<ParameterExpression> ExtraLocals { get; } = new();
    public List<Expression> ParamCheckExpressions { get; } = new();
    public List<Func<HttpContext, ValueTask<object?>>> ParameterBinders { get; } = new();

    public Dictionary<string, string> TrackedParameters { get; } = new();
    public bool HasMultipleBodyParameters { get; set; }
    public bool HasInferredBody { get; set; }

    public NullabilityInfoContext NullabilityContext { get; } = new();

    // Used to invoke TryResolveFormAsync once per handler so that we can
    // avoid the blocking code-path that occurs when `httpContext.Request.Form`
    // is called.
    public bool ReadForm { get; set; }
    public bool ReadFormFile { get; set; }
    public ParameterInfo? FirstFormRequestBodyParameter { get; set; }
    // Properties for constructing and managing filters
    public Expression? MethodCall { get; set; }
    public Type[] ArgumentTypes { get; set; } = Array.Empty<Type>();
    public Expression[]? ArgumentExpressions { get; set; }
    public Expression[] BoxedArgs { get; set; } = Array.Empty<Expression>();
    public bool FilterFactoriesHaveRunWithoutModifyingPerRequestBehavior { get; set; }

    public List<ParameterInfo> Parameters { get; set; } = new();

    // Grab these options upfront to avoid the per request DI scope that would be made otherwise to get the options when writing Json
    public required JsonSerializerOptions JsonSerializerOptions { get; set; }

    public required FormDataMapperOptions FormDataMapperOptions { get; set; }
}
