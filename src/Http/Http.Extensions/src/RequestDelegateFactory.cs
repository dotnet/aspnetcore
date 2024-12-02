// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Pipelines;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Creates <see cref="RequestDelegate"/> implementations from <see cref="Delegate"/> request handlers.
/// </summary>
[RequiresUnreferencedCode("RequestDelegateFactory performs object creation, serialization and deserialization on the delegates and its parameters. This cannot be statically analyzed.")]
[RequiresDynamicCode("RequestDelegateFactory performs object creation, serialization and deserialization on the delegates and its parameters. This cannot be statically analyzed.")]
public static partial class RequestDelegateFactory
{
    private static readonly MethodInfo ExecuteTaskWithEmptyResultMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTaskWithEmptyResult), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteValueTaskWithEmptyResultMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskWithEmptyResult), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteTaskOfTMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTaskOfT), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteTaskOfTFastMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTaskOfTFast), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteTaskOfObjectMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTaskOfObject), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteValueTaskOfObjectMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskOfObject), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteTaskOfStringMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTaskOfString), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteValueTaskOfTMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskOfT), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteValueTaskOfTFastMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskOfTFast), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteValueTaskMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTask), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteValueTaskOfStringMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskOfString), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteTaskResultOfTMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTaskResult), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteValueResultTaskOfTMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskResult), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteAwaitedReturnMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteAwaitedReturn), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo GetRequiredServiceMethod = typeof(ServiceProviderServiceExtensions).GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(IServiceProvider) })!;
    private static readonly MethodInfo GetServiceMethod = typeof(ServiceProviderServiceExtensions).GetMethod(nameof(ServiceProviderServiceExtensions.GetService), BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(IServiceProvider) })!;
    private static readonly MethodInfo GetRequiredKeyedServiceMethod = typeof(ServiceProviderKeyedServiceExtensions).GetMethod(nameof(ServiceProviderKeyedServiceExtensions.GetRequiredKeyedService), BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(IServiceProvider), typeof(object) })!;
    private static readonly MethodInfo GetKeyedServiceMethod = typeof(ServiceProviderKeyedServiceExtensions).GetMethod(nameof(ServiceProviderKeyedServiceExtensions.GetKeyedService), BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(IServiceProvider), typeof(object) })!;
    private static readonly MethodInfo ResultWriteResponseAsyncMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteResultWriteResponse), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo StringResultWriteResponseAsyncMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteWriteStringResponseAsync), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo StringIsNullOrEmptyMethod = typeof(string).GetMethod(nameof(string.IsNullOrEmpty), BindingFlags.Static | BindingFlags.Public)!;
    private static readonly MethodInfo WrapObjectAsValueTaskMethod = typeof(RequestDelegateFactory).GetMethod(nameof(WrapObjectAsValueTask), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo TaskOfTToValueTaskOfObjectMethod = typeof(RequestDelegateFactory).GetMethod(nameof(TaskOfTToValueTaskOfObject), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ValueTaskOfTToValueTaskOfObjectMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ValueTaskOfTToValueTaskOfObject), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ArrayEmptyOfObjectMethod = typeof(Array).GetMethod(nameof(Array.Empty), BindingFlags.Public | BindingFlags.Static)!.MakeGenericMethod(new Type[] { typeof(object) });

    private static readonly PropertyInfo QueryIndexerProperty = typeof(IQueryCollection).GetProperty("Item")!;
    private static readonly PropertyInfo RouteValuesIndexerProperty = typeof(RouteValueDictionary).GetProperty("Item")!;
    private static readonly PropertyInfo HeaderIndexerProperty = typeof(IHeaderDictionary).GetProperty("Item")!;
    private static readonly PropertyInfo FormFilesIndexerProperty = typeof(IFormFileCollection).GetProperty("Item")!;
    private static readonly PropertyInfo FormIndexerProperty = typeof(IFormCollection).GetProperty("Item")!;

    private static readonly MethodInfo JsonResultWriteResponseOfTFastAsyncMethod = typeof(RequestDelegateFactory).GetMethod(nameof(WriteJsonResponseFast), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo JsonResultWriteResponseOfTAsyncMethod = typeof(RequestDelegateFactory).GetMethod(nameof(WriteJsonResponse), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo LogParameterBindingFailedMethod = GetMethodInfo<Action<HttpContext, string, string, string, bool>>((httpContext, parameterType, parameterName, sourceValue, shouldThrow) =>
        Log.ParameterBindingFailed(httpContext, parameterType, parameterName, sourceValue, shouldThrow));
    private static readonly MethodInfo LogRequiredParameterNotProvidedMethod = GetMethodInfo<Action<HttpContext, string, string, string, bool>>((httpContext, parameterType, parameterName, source, shouldThrow) =>
        Log.RequiredParameterNotProvided(httpContext, parameterType, parameterName, source, shouldThrow));
    private static readonly MethodInfo LogImplicitBodyNotProvidedMethod = GetMethodInfo<Action<HttpContext, string, bool>>((httpContext, parameterName, shouldThrow) =>
        Log.ImplicitBodyNotProvided(httpContext, parameterName, shouldThrow));
    private static readonly MethodInfo LogFormMappingFailedMethod = GetMethodInfo<Action<HttpContext, string, string, FormDataMappingException, bool>>((httpContext, parameterName, parameterType, exception, shouldThrow) =>
        Log.FormDataMappingFailed(httpContext, parameterName, parameterType, exception, shouldThrow));

    private static readonly ParameterExpression TargetExpr = Expression.Parameter(typeof(object), "target");
    private static readonly ParameterExpression BodyValueExpr = Expression.Parameter(typeof(object), "bodyValue");
    private static readonly ParameterExpression WasParamCheckFailureExpr = Expression.Variable(typeof(bool), "wasParamCheckFailure");
    private static readonly ParameterExpression BoundValuesArrayExpr = Expression.Parameter(typeof(object[]), "boundValues");

    private static readonly ParameterExpression HttpContextExpr = ParameterBindingMethodCache.SharedExpressions.HttpContextExpr;
    private static readonly MemberExpression RequestServicesExpr = Expression.Property(HttpContextExpr, typeof(HttpContext).GetProperty(nameof(HttpContext.RequestServices))!);
    private static readonly MemberExpression HttpRequestExpr = Expression.Property(HttpContextExpr, typeof(HttpContext).GetProperty(nameof(HttpContext.Request))!);
    private static readonly MemberExpression HttpResponseExpr = Expression.Property(HttpContextExpr, typeof(HttpContext).GetProperty(nameof(HttpContext.Response))!);
    private static readonly MemberExpression RequestAbortedExpr = Expression.Property(HttpContextExpr, typeof(HttpContext).GetProperty(nameof(HttpContext.RequestAborted))!);
    private static readonly MemberExpression UserExpr = Expression.Property(HttpContextExpr, typeof(HttpContext).GetProperty(nameof(HttpContext.User))!);
    private static readonly MemberExpression RouteValuesExpr = Expression.Property(HttpRequestExpr, typeof(HttpRequest).GetProperty(nameof(HttpRequest.RouteValues))!);
    private static readonly MemberExpression QueryExpr = Expression.Property(HttpRequestExpr, typeof(HttpRequest).GetProperty(nameof(HttpRequest.Query))!);
    private static readonly MemberExpression HeadersExpr = Expression.Property(HttpRequestExpr, typeof(HttpRequest).GetProperty(nameof(HttpRequest.Headers))!);
    private static readonly MemberExpression FormExpr = Expression.Property(HttpRequestExpr, typeof(HttpRequest).GetProperty(nameof(HttpRequest.Form))!);
    private static readonly MemberExpression RequestStreamExpr = Expression.Property(HttpRequestExpr, typeof(HttpRequest).GetProperty(nameof(HttpRequest.Body))!);
    private static readonly MemberExpression RequestPipeReaderExpr = Expression.Property(HttpRequestExpr, typeof(HttpRequest).GetProperty(nameof(HttpRequest.BodyReader))!);
    private static readonly MemberExpression FormFilesExpr = Expression.Property(FormExpr, typeof(IFormCollection).GetProperty(nameof(IFormCollection.Files))!);
    private static readonly MemberExpression StatusCodeExpr = Expression.Property(HttpResponseExpr, typeof(HttpResponse).GetProperty(nameof(HttpResponse.StatusCode))!);
    private static readonly MemberExpression CompletedTaskExpr = Expression.Property(null, (PropertyInfo)GetMemberInfo<Func<Task>>(() => Task.CompletedTask));
    private static readonly NewExpression EmptyHttpResultValueTaskExpr = Expression.New(typeof(ValueTask<object>).GetConstructor(new[] { typeof(EmptyHttpResult) })!, Expression.Property(null, typeof(EmptyHttpResult), nameof(EmptyHttpResult.Instance)));
    private static readonly ParameterExpression TempSourceStringExpr = ParameterBindingMethodCache.SharedExpressions.TempSourceStringExpr;
    private static readonly BinaryExpression TempSourceStringNotNullExpr = Expression.NotEqual(TempSourceStringExpr, Expression.Constant(null));
    private static readonly BinaryExpression TempSourceStringNullExpr = Expression.Equal(TempSourceStringExpr, Expression.Constant(null));
    private static readonly UnaryExpression TempSourceStringIsNotNullOrEmptyExpr = Expression.Not(Expression.Call(StringIsNullOrEmptyMethod, TempSourceStringExpr));

    private static readonly ConstructorInfo DefaultEndpointFilterInvocationContextConstructor = typeof(DefaultEndpointFilterInvocationContext).GetConstructor(new[] { typeof(HttpContext), typeof(object[]) })!;
    private static readonly MethodInfo EndpointFilterInvocationContextGetArgument = typeof(EndpointFilterInvocationContext).GetMethod(nameof(EndpointFilterInvocationContext.GetArgument))!;
    private static readonly PropertyInfo ListIndexer = typeof(IList<object>).GetProperty("Item")!;
    private static readonly ParameterExpression FilterContextExpr = Expression.Parameter(typeof(EndpointFilterInvocationContext), "context");
    private static readonly MemberExpression FilterContextHttpContextExpr = Expression.Property(FilterContextExpr, typeof(EndpointFilterInvocationContext).GetProperty(nameof(EndpointFilterInvocationContext.HttpContext))!);
    private static readonly MemberExpression FilterContextArgumentsExpr = Expression.Property(FilterContextExpr, typeof(EndpointFilterInvocationContext).GetProperty(nameof(EndpointFilterInvocationContext.Arguments))!);
    private static readonly MemberExpression FilterContextHttpContextResponseExpr = Expression.Property(FilterContextHttpContextExpr, typeof(HttpContext).GetProperty(nameof(HttpContext.Response))!);
    private static readonly MemberExpression FilterContextHttpContextStatusCodeExpr = Expression.Property(FilterContextHttpContextResponseExpr, typeof(HttpResponse).GetProperty(nameof(HttpResponse.StatusCode))!);
    private static readonly ParameterExpression InvokedFilterContextExpr = Expression.Parameter(typeof(EndpointFilterInvocationContext), "filterContext");

    private static readonly ConstructorInfo FormDataReaderConstructor = typeof(FormDataReader).GetConstructor(new[] { typeof(IReadOnlyDictionary<FormKey, StringValues>), typeof(CultureInfo), typeof(Memory<char>), typeof(IFormFileCollection) })!;
    private static readonly MethodInfo ProcessFormMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ProcessForm), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo FormDataMapperMapMethod = typeof(FormDataMapper).GetMethod(nameof(FormDataMapper.Map))!;
    private static readonly MethodInfo AsMemoryMethod = new Func<char[]?, int, int, Memory<char>>(MemoryExtensions.AsMemory).Method;
    private static readonly MethodInfo ArrayPoolSharedReturnMethod = typeof(ArrayPool<char>).GetMethod(nameof(ArrayPool<char>.Shared.Return))!;

    private static readonly string[] DefaultAcceptsAndProducesContentType = new[] { ContentTypeConstants.JsonContentType };
    private static readonly string[] FormFileContentType = new[] { "multipart/form-data" };
    private static readonly string[] FormContentType = new[] { "multipart/form-data", "application/x-www-form-urlencoded" };
    private static readonly string[] PlaintextContentType = new[] { "text/plain" };

    /// <summary>
    /// Returns metadata inferred automatically for the <see cref="RequestDelegate"/> created by <see cref="Create(Delegate, RequestDelegateFactoryOptions?, RequestDelegateMetadataResult?)"/>.
    /// This includes metadata inferred by <see cref="IEndpointMetadataProvider"/> and <see cref="IEndpointParameterMetadataProvider"/> implemented by parameter and return types to the <paramref name="methodInfo"/>.
    /// </summary>
    /// <param name="methodInfo">The <see cref="MethodInfo"/> for the route handler to be passed to <see cref="Create(Delegate, RequestDelegateFactoryOptions?, RequestDelegateMetadataResult?)"/>.</param>
    /// <param name="options">The options that will be used when calling <see cref="Create(Delegate, RequestDelegateFactoryOptions?, RequestDelegateMetadataResult?)"/>.</param>
    /// <returns>The <see cref="RequestDelegateMetadataResult"/> to be passed to <see cref="Create(Delegate, RequestDelegateFactoryOptions?, RequestDelegateMetadataResult?)"/>.</returns>
    public static RequestDelegateMetadataResult InferMetadata(MethodInfo methodInfo, RequestDelegateFactoryOptions? options = null)
    {
        var factoryContext = CreateFactoryContext(options);
        factoryContext.ArgumentExpressions = CreateArgumentsAndInferMetadata(methodInfo, factoryContext);

        return new RequestDelegateMetadataResult
        {
            EndpointMetadata = AsReadOnlyList(factoryContext.EndpointBuilder.Metadata),
            CachedFactoryContext = factoryContext,
        };
    }

    /// <summary>
    /// Creates a <see cref="RequestDelegate"/> implementation for <paramref name="handler"/>.
    /// </summary>
    /// <param name="handler">A request handler with any number of custom parameters that often produces a response with its return value.</param>
    /// <param name="options">The <see cref="RequestDelegateFactoryOptions"/> used to configure the behavior of the handler.</param>
    /// <returns>The <see cref="RequestDelegateResult"/>.</returns>
    public static RequestDelegateResult Create(Delegate handler, RequestDelegateFactoryOptions? options)
    {
        return Create(handler, options, metadataResult: null);
    }

    /// <summary>
    /// Creates a <see cref="RequestDelegate"/> implementation for <paramref name="handler"/>.
    /// </summary>
    /// <param name="handler">A request handler with any number of custom parameters that often produces a response with its return value.</param>
    /// <param name="options">The <see cref="RequestDelegateFactoryOptions"/> used to configure the behavior of the handler.</param>
    /// <param name="metadataResult">
    /// The result returned from <see cref="InferMetadata(MethodInfo, RequestDelegateFactoryOptions?)"/> if that was used to inferring metadata before creating the final RequestDelegate.
    /// If <see langword="null"/>, this call to <see cref="Create(Delegate, RequestDelegateFactoryOptions?, RequestDelegateMetadataResult?)"/> method will infer the metadata that
    /// <see cref="InferMetadata(MethodInfo, RequestDelegateFactoryOptions?)"/> would have inferred for the same <see cref="Delegate.Method"/> and populate <see cref="RequestDelegateFactoryOptions.EndpointBuilder"/>
    /// with that metadata. Otherwise, this metadata inference will be skipped as this step has already been done.
    /// </param>
    /// <returns>The <see cref="RequestDelegateResult"/>.</returns>
    [SuppressMessage("ApiDesign", "RS0027:Public API with optional parameter(s) should have the most parameters amongst its public overloads.", Justification = "Required to maintain compatibility")]
    public static RequestDelegateResult Create(Delegate handler, RequestDelegateFactoryOptions? options = null, RequestDelegateMetadataResult? metadataResult = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var targetExpression = handler.Target switch
        {
            object => Expression.Convert(TargetExpr, handler.Target.GetType()),
            null => null,
        };

        var factoryContext = CreateFactoryContext(options, metadataResult, handler);

        Expression<Func<HttpContext, object?>> targetFactory = (httpContext) => handler.Target;
        var targetableRequestDelegate = CreateTargetableRequestDelegate(handler.Method, targetExpression, factoryContext, targetFactory);

        RequestDelegate finalRequestDelegate = targetableRequestDelegate switch
        {
            // handler is a RequestDelegate that has not been modified by a filter. Short-circuit and return the original RequestDelegate back.
            // It's possible a filter factory has still modified the endpoint metadata though.
            null => (RequestDelegate)handler,
            _ => httpContext => targetableRequestDelegate(handler.Target, httpContext),
        };

        return CreateRequestDelegateResult(finalRequestDelegate, factoryContext.EndpointBuilder);
    }

    /// <summary>
    /// Creates a <see cref="RequestDelegate"/> implementation for <paramref name="methodInfo"/>.
    /// </summary>
    /// <param name="methodInfo">A request handler with any number of custom parameters that often produces a response with its return value.</param>
    /// <param name="targetFactory">Creates the <see langword="this"/> for the non-static method.</param>
    /// <param name="options">The <see cref="RequestDelegateFactoryOptions"/> used to configure the behavior of the handler.</param>
    /// <returns>The <see cref="RequestDelegate"/>.</returns>
    public static RequestDelegateResult Create(MethodInfo methodInfo, Func<HttpContext, object>? targetFactory, RequestDelegateFactoryOptions? options)
    {
        return Create(methodInfo, targetFactory, options, metadataResult: null);
    }

    /// <summary>
    /// Creates a <see cref="RequestDelegate"/> implementation for <paramref name="methodInfo"/>.
    /// </summary>
    /// <param name="methodInfo">A request handler with any number of custom parameters that often produces a response with its return value.</param>
    /// <param name="targetFactory">Creates the <see langword="this"/> for the non-static method.</param>
    /// <param name="options">The <see cref="RequestDelegateFactoryOptions"/> used to configure the behavior of the handler.</param>
    /// <param name="metadataResult">
    /// The result returned from <see cref="InferMetadata(MethodInfo, RequestDelegateFactoryOptions?)"/> if that was used to inferring metadata before creating the final RequestDelegate.
    /// If <see langword="null"/>, this call to <see cref="Create(Delegate, RequestDelegateFactoryOptions?, RequestDelegateMetadataResult?)"/> method will infer the metadata that
    /// <see cref="InferMetadata(MethodInfo, RequestDelegateFactoryOptions?)"/> would have inferred for the same <see cref="Delegate.Method"/> and populate <see cref="RequestDelegateFactoryOptions.EndpointBuilder"/>
    /// with that metadata. Otherwise, this metadata inference will be skipped as this step has already been done.
    /// </param>
    /// <returns>The <see cref="RequestDelegate"/>.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static RequestDelegateResult Create(MethodInfo methodInfo, Func<HttpContext, object>? targetFactory = null, RequestDelegateFactoryOptions? options = null, RequestDelegateMetadataResult? metadataResult = null)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);

        if (methodInfo.DeclaringType is null)
        {
            throw new ArgumentException($"{nameof(methodInfo)} does not have a declaring type.");
        }

        var factoryContext = CreateFactoryContext(options, metadataResult);
        RequestDelegate finalRequestDelegate;

        if (methodInfo.IsStatic)
        {
            var untargetableRequestDelegate = CreateTargetableRequestDelegate(methodInfo, targetExpression: null, factoryContext);

            // CreateTargetableRequestDelegate can only return null given a RequestDelegate passed into the other RDF.Create() overload.
            Debug.Assert(untargetableRequestDelegate is not null);

            finalRequestDelegate = httpContext => untargetableRequestDelegate(null, httpContext);
        }
        else
        {
            targetFactory ??= context => Activator.CreateInstance(methodInfo.DeclaringType)!;

            var targetExpression = Expression.Convert(TargetExpr, methodInfo.DeclaringType);
            var targetableRequestDelegate = CreateTargetableRequestDelegate(methodInfo, targetExpression, factoryContext, context => targetFactory(context));

            // CreateTargetableRequestDelegate can only return null given a RequestDelegate passed into the other RDF.Create() overload.
            Debug.Assert(targetableRequestDelegate is not null);

            finalRequestDelegate = httpContext => targetableRequestDelegate(targetFactory(httpContext), httpContext);
        }

        return CreateRequestDelegateResult(finalRequestDelegate, factoryContext.EndpointBuilder);
    }

    private static RequestDelegateFactoryContext CreateFactoryContext(RequestDelegateFactoryOptions? options, RequestDelegateMetadataResult? metadataResult = null, Delegate? handler = null)
    {
        if (metadataResult?.CachedFactoryContext is RequestDelegateFactoryContext cachedFactoryContext)
        {
            cachedFactoryContext.MetadataAlreadyInferred = true;
            // The handler was not passed in to the InferMetadata call that originally created this context.
            cachedFactoryContext.Handler = handler;
            return cachedFactoryContext;
        }

        var serviceProvider = options?.ServiceProvider ?? options?.EndpointBuilder?.ApplicationServices ?? EmptyServiceProvider.Instance;
        var endpointBuilder = options?.EndpointBuilder ?? new RdfEndpointBuilder(serviceProvider);
        var jsonSerializerOptions = serviceProvider.GetService<IOptions<JsonOptions>>()?.Value.SerializerOptions ?? JsonOptions.DefaultSerializerOptions;
        var formDataMapperOptions = new FormDataMapperOptions();

        var factoryContext = new RequestDelegateFactoryContext
        {
            Handler = handler,
            ServiceProvider = serviceProvider,
            ServiceProviderIsService = serviceProvider.GetService<IServiceProviderIsService>(),
            RouteParameters = options?.RouteParameterNames,
            ThrowOnBadRequest = options?.ThrowOnBadRequest ?? false,
            DisableInferredFromBody = options?.DisableInferBodyFromParameters ?? false,
            EndpointBuilder = endpointBuilder,
            MetadataAlreadyInferred = metadataResult is not null,
            JsonSerializerOptions = jsonSerializerOptions,
            FormDataMapperOptions = formDataMapperOptions
        };

        return factoryContext;
    }

    private static RequestDelegateResult CreateRequestDelegateResult(RequestDelegate finalRequestDelegate, EndpointBuilder endpointBuilder)
    {
        endpointBuilder.RequestDelegate = finalRequestDelegate;
        return new RequestDelegateResult(finalRequestDelegate, AsReadOnlyList(endpointBuilder.Metadata));
    }

    private static IReadOnlyList<object> AsReadOnlyList(IList<object> metadata)
    {
        if (metadata is IReadOnlyList<object> readOnlyList)
        {
            return readOnlyList;
        }

        return new List<object>(metadata);
    }

    private static Func<object?, HttpContext, Task>? CreateTargetableRequestDelegate(
        MethodInfo methodInfo,
        Expression? targetExpression,
        RequestDelegateFactoryContext factoryContext,
        Expression<Func<HttpContext, object?>>? targetFactory = null)
    {
        // Non void return type

        // Task Invoke(HttpContext httpContext)
        // {
        //     // Action parameters are bound from the request, services, etc... based on attribute and type information.
        //     return ExecuteTask(handler(...), httpContext);
        // }

        // void return type

        // Task Invoke(HttpContext httpContext)
        // {
        //     handler(...);
        //     return default;
        // }

        // If ArgumentExpressions is not null here, it's guaranteed we have already inferred metadata and we can reuse a lot of work.
        // The converse is not true. Metadata may have already been inferred even if ArgumentExpressions is null, but metadata
        // inference is skipped internally if necessary.
        factoryContext.ArgumentExpressions ??= CreateArgumentsAndInferMetadata(methodInfo, factoryContext);

        // Although we can re-use the cached argument expressions for most cases, parameters that are bound
        // using the new form mapping logic are a special exception because we need to account for the `FormOptionsMetadata`
        // added to the builder _during_ the construction of the parameter binding.
        UpdateFormBindingArgumentExpressions(factoryContext);

        factoryContext.MethodCall = CreateMethodCall(methodInfo, targetExpression, factoryContext.ArgumentExpressions);
        EndpointFilterDelegate? filterPipeline = null;
        var returnType = methodInfo.ReturnType;

        // If there are filters registered on the route handler, then we update the method call and
        // return type associated with the request to allow for the filter invocation pipeline.
        if (factoryContext.EndpointBuilder.FilterFactories.Count > 0)
        {
            filterPipeline = CreateFilterPipeline(methodInfo, targetExpression, factoryContext, targetFactory);

            if (filterPipeline is not null)
            {
                Expression<Func<EndpointFilterInvocationContext, ValueTask<object?>>> invokePipeline = (context) => filterPipeline(context);
                returnType = typeof(ValueTask<object?>);
                // var filterContext = new EndpointFilterInvocationContext<string, int>(httpContext, name_local, int_local);
                // invokePipeline.Invoke(filterContext);
                factoryContext.MethodCall = Expression.Block(
                    new[] { InvokedFilterContextExpr },
                    Expression.Assign(
                        InvokedFilterContextExpr,
                        CreateEndpointFilterInvocationContextBase(factoryContext, factoryContext.ArgumentExpressions)),
                        Expression.Invoke(invokePipeline, InvokedFilterContextExpr)
                    );
            }
        }

        // return null for plain RequestDelegates that have not been modified by filters so we can just pass back the original RequestDelegate.
        if (filterPipeline is null && factoryContext.Handler is RequestDelegate)
        {
            return null;
        }

        var responseWritingMethodCall = factoryContext.ParamCheckExpressions.Count > 0 ?
            CreateParamCheckingResponseWritingMethodCall(returnType, factoryContext) :
            AddResponseWritingToMethodCall(factoryContext.MethodCall, returnType, factoryContext);

        if (factoryContext.UsingTempSourceString)
        {
            responseWritingMethodCall = Expression.Block(new[] { TempSourceStringExpr }, responseWritingMethodCall);
        }

        return HandleRequestBodyAndCompileRequestDelegate(responseWritingMethodCall, factoryContext);
    }

    private static Expression[] CreateArgumentsAndInferMetadata(MethodInfo methodInfo, RequestDelegateFactoryContext factoryContext)
    {
        // Add any default accepts metadata. This does a lot of reflection and expression tree building, so the results are cached in RequestDelegateFactoryOptions.FactoryContext
        // For later reuse in Create().
        var args = CreateArguments(methodInfo.GetParameters(), factoryContext);

        if (!factoryContext.MetadataAlreadyInferred)
        {
            if (factoryContext.ReadForm)
            {
                // Add the Accepts metadata when reading from FORM.
                InferFormAcceptsMetadata(factoryContext);
                InferAntiforgeryMetadata(factoryContext);
            }

            PopulateBuiltInResponseTypeMetadata(methodInfo.ReturnType, factoryContext.EndpointBuilder);

            // Add metadata provided by the delegate return type and parameter types next, this will be more specific than inferred metadata from above
            EndpointMetadataPopulator.PopulateMetadata(methodInfo, factoryContext.EndpointBuilder, factoryContext.Parameters);
        }

        return args;
    }

    private static EndpointFilterDelegate? CreateFilterPipeline(MethodInfo methodInfo, Expression? targetExpression, RequestDelegateFactoryContext factoryContext, Expression<Func<HttpContext, object?>>? targetFactory)
    {
        Debug.Assert(factoryContext.EndpointBuilder.FilterFactories.Count > 0);
        // httpContext.Response.StatusCode >= 400
        // ? Task.CompletedTask
        // : {
        //   handlerInvocation
        // }
        // To generate the handler invocation, we first create the
        // target of the handler provided to the route.
        //      target = targetFactory(httpContext);
        // This target is then used to generate the handler invocation like so;
        //      ((Type)target).MethodName(parameters);
        //  When `handler` returns an object, we generate the following wrapper
        //  to convert it to `ValueTask<object?>` as expected in the filter
        //  pipeline.
        //      ValueTask<object?>.FromResult(handler(EndpointFilterInvocationContext.GetArgument<string>(0), EndpointFilterInvocationContext.GetArgument<int>(1)));
        //  When the `handler` is a generic Task or ValueTask we await the task and
        //  create a `ValueTask<object?> from the resulting value.
        //      new ValueTask<object?>(await handler(EndpointFilterInvocationContext.GetArgument<string>(0), EndpointFilterInvocationContext.GetArgument<int>(1)));
        //  When the `handler` returns a void or a void-returning Task, then we return an EmptyHttpResult
        //  to as a ValueTask<object?>
        // }

        var argTypes = factoryContext.ArgumentTypes;
        var contextArgAccess = new Expression[argTypes.Length];

        for (var i = 0; i < argTypes.Length; i++)
        {
            // MakeGenericMethod + value type requires IsDynamicCodeSupported to be true.
            if (RuntimeFeature.IsDynamicCodeSupported)
            {
                // Register expressions containing the boxed and unboxed variants
                // of the route handler's arguments for use in EndpointFilterInvocationContext
                // construction and route handler invocation.
                // context.GetArgument<string>(0)
                // (string, name_local), (int, int_local)
                contextArgAccess[i] = Expression.Call(FilterContextExpr, EndpointFilterInvocationContextGetArgument.MakeGenericMethod(argTypes[i]), Expression.Constant(i));
            }
            else
            {
                // We box if dynamic code isn't supported
                contextArgAccess[i] = Expression.Convert(
                    Expression.Property(FilterContextArgumentsExpr, ListIndexer, Expression.Constant(i)),
                argTypes[i]);
            }
        }

        var handlerReturnMapping = MapHandlerReturnTypeToValueTask(
                        targetExpression is null
                            ? Expression.Call(methodInfo, contextArgAccess)
                            : Expression.Call(targetExpression, methodInfo, contextArgAccess),
                        methodInfo.ReturnType);
        var handlerInvocation = Expression.Block(
                    new[] { TargetExpr },
                    targetFactory == null
                        ? Expression.Empty()
                        : Expression.Assign(TargetExpr, Expression.Invoke(targetFactory, FilterContextHttpContextExpr)),
                    handlerReturnMapping
                );
        var filteredInvocation = Expression.Lambda<EndpointFilterDelegate>(
            Expression.Condition(
                Expression.GreaterThanOrEqual(FilterContextHttpContextStatusCodeExpr, Expression.Constant(400)),
                EmptyHttpResultValueTaskExpr,
                handlerInvocation),
            FilterContextExpr).Compile();
        var routeHandlerContext = new EndpointFilterFactoryContext
        {
            MethodInfo = methodInfo,
            ApplicationServices = factoryContext.EndpointBuilder.ApplicationServices,
        };

        var initialFilteredInvocation = filteredInvocation;

        for (var i = factoryContext.EndpointBuilder.FilterFactories.Count - 1; i >= 0; i--)
        {
            var currentFilterFactory = factoryContext.EndpointBuilder.FilterFactories[i];
            filteredInvocation = currentFilterFactory(routeHandlerContext, filteredInvocation);
        }

        // The filter factories have run without modifying per-request behavior, we can skip running the pipeline.
        // If a plain old RequestDelegate was passed in (with no generic parameter), we can just return it back directly now.
        if (ReferenceEquals(initialFilteredInvocation, filteredInvocation))
        {
            return null;
        }

        return filteredInvocation;
    }

    private static Expression MapHandlerReturnTypeToValueTask(Expression methodCall, Type returnType)
    {
        if (returnType == typeof(void))
        {
            return Expression.Block(methodCall, EmptyHttpResultValueTaskExpr);
        }
        else if (CoercedAwaitableInfo.IsTypeAwaitable(returnType, out var coercedAwaitableInfo))
        {
            if (coercedAwaitableInfo.CoercerResultType is { } coercedType)
            {
                returnType = coercedType;
            }

            if (coercedAwaitableInfo.CoercerExpression is { } coercerExpression)
            {
                methodCall = Expression.Invoke(coercerExpression, methodCall);
            }

            if (returnType == typeof(Task))
            {
                return Expression.Call(ExecuteTaskWithEmptyResultMethod, methodCall);
            }
            else if (returnType == typeof(ValueTask))
            {
                return Expression.Call(ExecuteValueTaskWithEmptyResultMethod, methodCall);
            }
            else if (returnType == typeof(ValueTask<object?>))
            {
                return methodCall;
            }
            else if (returnType.IsGenericType &&
                         returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                var typeArg = coercedAwaitableInfo.AwaitableInfo.ResultType;
                return Expression.Call(ValueTaskOfTToValueTaskOfObjectMethod.MakeGenericMethod(typeArg), methodCall);
            }
            else if (returnType.IsGenericType &&
                        returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var typeArg = coercedAwaitableInfo.AwaitableInfo.ResultType;
                return Expression.Call(TaskOfTToValueTaskOfObjectMethod.MakeGenericMethod(typeArg), methodCall);
            }
        }

        if (returnType.IsValueType)
        {
            return Expression.Call(WrapObjectAsValueTaskMethod, Expression.Convert(methodCall, typeof(object)));
        }

        return Expression.Call(WrapObjectAsValueTaskMethod, methodCall);
    }

    private static ValueTask<object?> ValueTaskOfTToValueTaskOfObject<T>(ValueTask<T> valueTask)
    {
        static async ValueTask<object?> ExecuteAwaited(ValueTask<T> valueTask)
        {
            return await valueTask;
        }

        if (valueTask.IsCompletedSuccessfully)
        {
            return new ValueTask<object?>(valueTask.Result);
        }

        return ExecuteAwaited(valueTask);
    }

    private static ValueTask<object?> TaskOfTToValueTaskOfObject<T>(Task<T> task)
    {
        static async ValueTask<object?> ExecuteAwaited(Task<T> task)
        {
            return await task;
        }

        if (task.IsCompletedSuccessfully)
        {
            return new ValueTask<object?>(task.Result);
        }

        return ExecuteAwaited(task);
    }

    private static Expression CreateEndpointFilterInvocationContextBase(RequestDelegateFactoryContext factoryContext, Expression[] arguments)
    {
        // In the event that a constructor matching the arity of the
        // provided parameters is not found, we fall back to using the
        // non-generic implementation of EndpointFilterInvocationContext.
        Expression paramArray = factoryContext.BoxedArgs.Length > 0
            ? Expression.NewArrayInit(typeof(object), factoryContext.BoxedArgs)
            : Expression.Call(ArrayEmptyOfObjectMethod);
        var fallbackConstruction = Expression.New(
            DefaultEndpointFilterInvocationContextConstructor,
            new Expression[] { HttpContextExpr, paramArray });

        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            // For AOT platforms it's not possible to support the closed generic arguments that are based on the
            // parameter arguments dynamically (for value types). In that case, fallback to boxing the argument list.
            return fallbackConstruction;
        }

        var expandedArguments = new Expression[arguments.Length + 1];
        expandedArguments[0] = HttpContextExpr;
        arguments.CopyTo(expandedArguments, 1);

        var constructorType = factoryContext.ArgumentTypes?.Length switch
        {
            1 => typeof(EndpointFilterInvocationContext<>),
            2 => typeof(EndpointFilterInvocationContext<,>),
            3 => typeof(EndpointFilterInvocationContext<,,>),
            4 => typeof(EndpointFilterInvocationContext<,,,>),
            5 => typeof(EndpointFilterInvocationContext<,,,,>),
            6 => typeof(EndpointFilterInvocationContext<,,,,,>),
            7 => typeof(EndpointFilterInvocationContext<,,,,,,>),
            8 => typeof(EndpointFilterInvocationContext<,,,,,,,>),
            9 => typeof(EndpointFilterInvocationContext<,,,,,,,,>),
            10 => typeof(EndpointFilterInvocationContext<,,,,,,,,,>),
            _ => typeof(DefaultEndpointFilterInvocationContext)
        };

        if (constructorType.IsGenericType)
        {
            var constructor = constructorType.MakeGenericType(factoryContext.ArgumentTypes!).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).SingleOrDefault();
            if (constructor == null)
            {
                // new EndpointFilterInvocationContext(httpContext, (object)name_local, (object)int_local);
                return fallbackConstruction;
            }

            // new EndpointFilterInvocationContext<string, int>(httpContext, name_local, int_local);
            return Expression.New(constructor, expandedArguments);
        }

        // new EndpointFilterInvocationContext(httpContext, (object)name_local, (object)int_local);
        return fallbackConstruction;
    }

    private static Expression[] CreateArguments(ParameterInfo[]? parameters, RequestDelegateFactoryContext factoryContext)
    {
        if (parameters is null || parameters.Length == 0)
        {
            return Array.Empty<Expression>();
        }

        var args = new Expression[parameters.Length];

        factoryContext.ArgumentTypes = new Type[parameters.Length];
        factoryContext.BoxedArgs = new Expression[parameters.Length];
        factoryContext.Parameters = new List<ParameterInfo>(parameters);

        for (var i = 0; i < parameters.Length; i++)
        {
            args[i] = CreateArgument(parameters[i], factoryContext, out var hasTryParse, out var hasBindAsync, out var isAsParameters);

            if (!isAsParameters)
            {
                factoryContext.EndpointBuilder.Metadata.Add(new ParameterBindingMetadata(
                    name: parameters[i].Name!,
                    parameterInfo: parameters[i],
                    hasTryParse: hasTryParse,
                    hasBindAsync: hasBindAsync,
                    isOptional: IsOptionalParameter(parameters[i], factoryContext)
                ));
            }
            factoryContext.ArgumentTypes[i] = parameters[i].ParameterType;
            factoryContext.BoxedArgs[i] = Expression.Convert(args[i], typeof(object));
        }

        if (factoryContext.HasInferredBody && factoryContext.DisableInferredFromBody)
        {
            var errorMessage = BuildErrorMessageForInferredBodyParameter(factoryContext);
            throw new InvalidOperationException(errorMessage);
        }

        if (factoryContext.JsonRequestBodyParameter is not null &&
            factoryContext.FirstFormRequestBodyParameter is not null)
        {
            var errorMessage = BuildErrorMessageForFormAndJsonBodyParameters(factoryContext);
            throw new InvalidOperationException(errorMessage);
        }
        if (factoryContext.HasMultipleBodyParameters)
        {
            var errorMessage = BuildErrorMessageForMultipleBodyParameters(factoryContext);
            throw new InvalidOperationException(errorMessage);
        }

        return args;
    }

    private static Expression CreateArgument(ParameterInfo parameter, RequestDelegateFactoryContext factoryContext, out bool hasTryParse, out bool hasBindAsync, out bool isAsParameters)
    {
        hasTryParse = false;
        hasBindAsync = false;
        isAsParameters = false;
        if (parameter.Name is null)
        {
            throw new InvalidOperationException($"Encountered a parameter of type '{parameter.ParameterType}' without a name. Parameters must have a name.");
        }

        if (parameter.ParameterType.IsByRef)
        {
            var attribute = "ref";

            if (parameter.Attributes.HasFlag(ParameterAttributes.In))
            {
                attribute = "in";
            }
            else if (parameter.Attributes.HasFlag(ParameterAttributes.Out))
            {
                attribute = "out";
            }

            throw new NotSupportedException($"The by reference parameter '{attribute} {TypeNameHelper.GetTypeDisplayName(parameter.ParameterType, fullName: false)} {parameter.Name}' is not supported.");
        }

        var parameterCustomAttributes = parameter.GetCustomAttributes();

        if (parameterCustomAttributes.OfType<IFromRouteMetadata>().FirstOrDefault() is { } routeAttribute)
        {
            var routeName = routeAttribute.Name ?? parameter.Name;
            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.RouteAttribute);
            if (factoryContext.RouteParameters is { } routeParams && !routeParams.Contains(routeName, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"'{routeName}' is not a route parameter.");
            }

            return BindParameterFromProperty(parameter, RouteValuesExpr, RouteValuesIndexerProperty, routeName, factoryContext, "route");
        }
        else if (parameterCustomAttributes.OfType<IFromQueryMetadata>().FirstOrDefault() is { } queryAttribute)
        {
            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.QueryAttribute);
            return BindParameterFromProperty(parameter, QueryExpr, QueryIndexerProperty, queryAttribute.Name ?? parameter.Name, factoryContext, "query string");
        }
        else if (parameterCustomAttributes.OfType<IFromHeaderMetadata>().FirstOrDefault() is { } headerAttribute)
        {
            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.HeaderAttribute);
            return BindParameterFromProperty(parameter, HeadersExpr, HeaderIndexerProperty, headerAttribute.Name ?? parameter.Name, factoryContext, "header");
        }
        else if (parameterCustomAttributes.OfType<IFromBodyMetadata>().FirstOrDefault() is { } bodyAttribute)
        {
            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.BodyAttribute);

            if (parameter.ParameterType == typeof(Stream))
            {
                return RequestStreamExpr;
            }
            else if (parameter.ParameterType == typeof(PipeReader))
            {
                return RequestPipeReaderExpr;
            }

            return BindParameterFromBody(parameter, bodyAttribute.AllowEmpty, factoryContext);
        }
        else if (parameterCustomAttributes.OfType<IFromFormMetadata>().FirstOrDefault() is { } formAttribute)
        {
            if (parameter.ParameterType == typeof(IFormFileCollection))
            {
                if (!string.IsNullOrEmpty(formAttribute.Name))
                {
                    throw new NotSupportedException(
                        $"Assigning a value to the {nameof(IFromFormMetadata)}.{nameof(IFromFormMetadata.Name)} property is not supported for parameters of type {nameof(IFormFileCollection)}.");
                }

                return BindParameterFromFormFiles(parameter, factoryContext);
            }
            else if (parameter.ParameterType == typeof(IFormFile))
            {
                return BindParameterFromFormFile(parameter, formAttribute.Name ?? parameter.Name, factoryContext, RequestDelegateFactoryConstants.FormFileAttribute);
            }
            else if (parameter.ParameterType == typeof(IFormCollection))
            {
                if (!string.IsNullOrEmpty(formAttribute.Name))
                {
                    throw new NotSupportedException(
                        $"Assigning a value to the {nameof(IFromFormMetadata)}.{nameof(IFromFormMetadata.Name)} property is not supported for parameters of type {nameof(IFormCollection)}.");

                }
                return BindParameterFromFormCollection(parameter, factoryContext);
            }
            // Continue to use the simple binding support that exists in RDF/RDG for currently
            // supported scenarios to maintain compatible semantics between versions of RDG.
            // For complex types, leverage the shared form binding infrastructure. For example,
            // shared form binding does not currently only supports types that implement IParsable
            // while RDF's binding implementation supports all TryParse implementations.
            var useSimpleBinding = parameter.ParameterType == typeof(string) ||
                parameter.ParameterType == typeof(StringValues) ||
                parameter.ParameterType == typeof(StringValues?) ||
                ParameterBindingMethodCache.Instance.HasTryParseMethod(parameter.ParameterType) ||
                (parameter.ParameterType.IsArray && ParameterBindingMethodCache.Instance.HasTryParseMethod(parameter.ParameterType.GetElementType()!));
            hasTryParse = useSimpleBinding;
            return useSimpleBinding
                ? BindParameterFromFormItem(parameter, formAttribute.Name ?? parameter.Name, factoryContext)
                : BindComplexParameterFromFormItem(parameter, string.IsNullOrEmpty(formAttribute.Name) ? parameter.Name : formAttribute.Name, factoryContext);
        }
        else if (parameter.CustomAttributes.Any(a => typeof(IFromServiceMetadata).IsAssignableFrom(a.AttributeType)))
        {
            if (parameterCustomAttributes.OfType<FromKeyedServicesAttribute>().FirstOrDefault() is not null)
            {
                throw new NotSupportedException(
                    $"The {nameof(FromKeyedServicesAttribute)} is not supported on parameters that are also annotated with {nameof(IFromServiceMetadata)}.");
            }
            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.ServiceAttribute);
            return BindParameterFromService(parameter, factoryContext);
        }
        else if (parameterCustomAttributes.OfType<FromKeyedServicesAttribute>().FirstOrDefault() is { } keyedServicesAttribute)
        {
            if (factoryContext.ServiceProviderIsService is not IServiceProviderIsKeyedService)
            {
                throw new InvalidOperationException($"Unable to resolve service referenced by {nameof(FromKeyedServicesAttribute)}. The service provider doesn't support keyed services.");
            }
            var key = keyedServicesAttribute.Key;
            return BindParameterFromKeyedService(parameter, key, factoryContext);
        }
        else if (parameterCustomAttributes.OfType<AsParametersAttribute>().Any())
        {
            isAsParameters = true;
            if (parameter is PropertyAsParameterInfo)
            {
                throw new NotSupportedException(
                    $"Nested {nameof(AsParametersAttribute)} is not supported and should be used only for handler parameters.");
            }

            return BindParameterFromProperties(parameter, factoryContext);
        }
        else if (parameter.ParameterType == typeof(HttpContext))
        {
            return HttpContextExpr;
        }
        else if (parameter.ParameterType == typeof(HttpRequest))
        {
            return HttpRequestExpr;
        }
        else if (parameter.ParameterType == typeof(HttpResponse))
        {
            return HttpResponseExpr;
        }
        else if (parameter.ParameterType == typeof(ClaimsPrincipal))
        {
            return UserExpr;
        }
        else if (parameter.ParameterType == typeof(CancellationToken))
        {
            return RequestAbortedExpr;
        }
        else if (parameter.ParameterType == typeof(IFormCollection))
        {
            return BindParameterFromFormCollection(parameter, factoryContext);
        }
        else if (parameter.ParameterType == typeof(IFormFileCollection))
        {
            return BindParameterFromFormFiles(parameter, factoryContext);
        }
        else if (parameter.ParameterType == typeof(IFormFile))
        {
            return BindParameterFromFormFile(parameter, parameter.Name, factoryContext, RequestDelegateFactoryConstants.FormFileParameter);
        }
        else if (parameter.ParameterType == typeof(Stream))
        {
            return RequestStreamExpr;
        }
        else if (parameter.ParameterType == typeof(PipeReader))
        {
            return RequestPipeReaderExpr;
        }
        else if (ParameterBindingMethodCache.Instance.HasBindAsyncMethod(parameter))
        {
            hasBindAsync = true;
            return BindParameterFromBindAsync(parameter, factoryContext);
        }
        else if (parameter.ParameterType == typeof(string) || ParameterBindingMethodCache.Instance.HasTryParseMethod(parameter.ParameterType))
        {
            hasTryParse = true;
            // 1. We bind from route values only, if route parameters are non-null and the parameter name is in that set.
            // 2. We bind from query only, if route parameters are non-null and the parameter name is NOT in that set.
            // 3. Otherwise, we fallback to route or query if route parameters is null (it means we don't know what route parameters are defined). This case only happens
            // when RDF.Create is manually invoked.
            if (factoryContext.RouteParameters is { } routeParams)
            {
                if (routeParams.Contains(parameter.Name, StringComparer.OrdinalIgnoreCase))
                {
                    // We're in the fallback case and we have a parameter and route parameter match so don't fallback
                    // to query string in this case
                    factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.RouteParameter);
                    return BindParameterFromProperty(parameter, RouteValuesExpr, RouteValuesIndexerProperty, parameter.Name, factoryContext, "route");
                }
                else
                {
                    factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.QueryStringParameter);
                    return BindParameterFromProperty(parameter, QueryExpr, QueryIndexerProperty, parameter.Name, factoryContext, "query string");
                }
            }

            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.RouteOrQueryStringParameter);
            return BindParameterFromRouteValueOrQueryString(parameter, parameter.Name, factoryContext);
        }
        else if (factoryContext.DisableInferredFromBody && (
            parameter.ParameterType == typeof(string[]) ||
                 parameter.ParameterType == typeof(StringValues) ||
                 parameter.ParameterType == typeof(StringValues?) ||
                (parameter.ParameterType.IsArray && ParameterBindingMethodCache.Instance.HasTryParseMethod(parameter.ParameterType.GetElementType()!))))
        {
            // We only infer parameter types if you have an array of TryParsables/string[]/StringValues/StringValues?, and DisableInferredFromBody is true
            hasTryParse = true;
            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.QueryStringParameter);
            return BindParameterFromProperty(parameter, QueryExpr, QueryIndexerProperty, parameter.Name, factoryContext, "query string");
        }
        else
        {
            if (factoryContext.ServiceProviderIsService is IServiceProviderIsService serviceProviderIsService)
            {
                if (serviceProviderIsService.IsService(parameter.ParameterType))
                {
                    factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.ServiceParameter);
                    return Expression.Call(GetRequiredServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
                }
            }

            factoryContext.HasInferredBody = true;
            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.BodyParameter);
            return BindParameterFromBody(parameter, allowEmpty: false, factoryContext);
        }
    }

    private static Expression CreateMethodCall(MethodInfo methodInfo, Expression? target, Expression[] arguments) =>
        target is null ?
            Expression.Call(methodInfo, arguments) :
            Expression.Call(target, methodInfo, arguments);

    private static ValueTask<object?> WrapObjectAsValueTask(object? obj)
    {
        return ValueTask.FromResult<object?>(obj);
    }

    // If we're calling TryParse or validating parameter optionality and
    // wasParamCheckFailure indicates it failed, set a 400 StatusCode instead of calling the method.
    private static Expression CreateParamCheckingResponseWritingMethodCall(Type returnType, RequestDelegateFactoryContext factoryContext)
    {
        // {
        //     string tempSourceString;
        //     bool wasParamCheckFailure = false;
        //
        //     // Assume "int param1" is the first parameter, "[FromRoute] int? param2 = 42" is the second parameter ...
        //     int param1_local;
        //     int? param2_local;
        //     // ...
        //
        //     tempSourceString = httpContext.RouteValue["param1"] ?? httpContext.Query["param1"];
        //
        //     if (tempSourceString != null)
        //     {
        //         if (!int.TryParse(tempSourceString, out param1_local))
        //         {
        //             wasParamCheckFailure = true;
        //             Log.ParameterBindingFailed(httpContext, "Int32", "id", tempSourceString)
        //         }
        //     }
        //
        //     tempSourceString = httpContext.RouteValue["param2"];
        //     // ...
        //
        //     return wasParamCheckFailure ?
        //         {
        //              httpContext.Response.StatusCode = 400;
        //              return Task.CompletedTask;
        //         } :
        //         {
        //             // Logic generated by AddResponseWritingToMethodCall() that calls handler(param1_local, param2_local, ...)
        //         };
        // }

        var localVariables = new ParameterExpression[factoryContext.ExtraLocals.Count + 1];
        var checkParamAndCallMethod = new Expression[factoryContext.ParamCheckExpressions.Count + 1];

        for (var i = 0; i < factoryContext.ExtraLocals.Count; i++)
        {
            localVariables[i] = factoryContext.ExtraLocals[i];
        }

        for (var i = 0; i < factoryContext.ParamCheckExpressions.Count; i++)
        {
            checkParamAndCallMethod[i] = factoryContext.ParamCheckExpressions[i];
        }

        localVariables[factoryContext.ExtraLocals.Count] = WasParamCheckFailureExpr;

        // If filters have been registered, we set the `wasParamCheckFailure` property
        // but do not return from the invocation to allow the filters to run.
        if (factoryContext.EndpointBuilder.FilterFactories.Count > 0)
        {
            // if (wasParamCheckFailure)
            // {
            //   httpContext.Response.StatusCode = 400;
            // }
            // return RequestDelegateFactory.ExecuteObjectReturn(invocationPipeline.Invoke(context) as object);
            var checkWasParamCheckFailureWithFilters = Expression.Block(
                Expression.IfThen(
                    WasParamCheckFailureExpr,
                    Expression.Assign(StatusCodeExpr, Expression.Constant(400))),
                AddResponseWritingToMethodCall(factoryContext.MethodCall!, returnType, factoryContext)
            );

            checkParamAndCallMethod[factoryContext.ParamCheckExpressions.Count] = checkWasParamCheckFailureWithFilters;
        }
        else
        {
            // wasParamCheckFailure ? {
            //  httpContext.Response.StatusCode = 400;
            //  return Task.CompletedTask;
            // } : {
            //  return RequestDelegateFactory.ExecuteObjectReturn(invocationPipeline.Invoke(context) as object);
            // }
            var checkWasParamCheckFailure = Expression.Condition(
                WasParamCheckFailureExpr,
                Expression.Block(
                    Expression.Assign(StatusCodeExpr, Expression.Constant(400)),
                    CompletedTaskExpr),
                AddResponseWritingToMethodCall(factoryContext.MethodCall!, returnType, factoryContext));
            checkParamAndCallMethod[factoryContext.ParamCheckExpressions.Count] = checkWasParamCheckFailure;
        }

        return Expression.Block(localVariables, checkParamAndCallMethod);
    }

    private static void PopulateBuiltInResponseTypeMetadata(Type returnType, EndpointBuilder builder)
    {
        if (returnType.IsByRefLike)
        {
            throw GetUnsupportedReturnTypeException(returnType);
        }

        var isAwaitable = false;
        if (CoercedAwaitableInfo.IsTypeAwaitable(returnType, out var coercedAwaitableInfo))
        {
            returnType = coercedAwaitableInfo.AwaitableInfo.ResultType;
            isAwaitable = true;
        }

        // Skip void returns and IResults. IResults might implement IEndpointMetadataProvider but otherwise we don't know what it might do.
        if (!isAwaitable && (returnType == typeof(void) || typeof(IResult).IsAssignableFrom(returnType)))
        {
            return;
        }

        if (returnType == typeof(string))
        {
            builder.Metadata.Add(ProducesResponseTypeMetadata.CreateUnvalidated(type: typeof(string), statusCode: 200, PlaintextContentType));
        }
        else if (returnType == typeof(void))
        {
            builder.Metadata.Add(ProducesResponseTypeMetadata.CreateUnvalidated(returnType, statusCode: 200, PlaintextContentType));
        }
        else
        {
            builder.Metadata.Add(ProducesResponseTypeMetadata.CreateUnvalidated(returnType, statusCode: 200, DefaultAcceptsAndProducesContentType));
        }
    }

    private static Expression AddResponseWritingToMethodCall(Expression methodCall, Type returnType, RequestDelegateFactoryContext factoryContext)
    {
        // Exact request delegate match
        if (returnType == typeof(void))
        {
            return Expression.Block(methodCall, CompletedTaskExpr);
        }
        else if (returnType == typeof(object))
        {
            return Expression.Call(
                ExecuteAwaitedReturnMethod,
                methodCall,
                HttpContextExpr,
                Expression.Constant(factoryContext.JsonSerializerOptions.GetReadOnlyTypeInfo(typeof(object)), typeof(JsonTypeInfo<object>)));
        }
        else if (returnType == typeof(ValueTask<object>))
        {
            return Expression.Call(ExecuteValueTaskOfObjectMethod,
                methodCall,
                HttpContextExpr,
                Expression.Constant(factoryContext.JsonSerializerOptions.GetReadOnlyTypeInfo(typeof(object)), typeof(JsonTypeInfo<object>)));
        }
        else if (returnType == typeof(Task<object>))
        {
            return Expression.Call(ExecuteTaskOfObjectMethod,
                methodCall,
                HttpContextExpr,
                Expression.Constant(factoryContext.JsonSerializerOptions.GetReadOnlyTypeInfo(typeof(object)), typeof(JsonTypeInfo<object>)));
        }
        else if (CoercedAwaitableInfo.IsTypeAwaitable(returnType, out var coercedAwaitableInfo))
        {
            if (coercedAwaitableInfo.CoercerResultType is { } coercedType)
            {
                returnType = coercedType;
            }

            if (coercedAwaitableInfo.CoercerExpression is { } coercerExpression)
            {
                methodCall = Expression.Invoke(coercerExpression, methodCall);
            }

            if (returnType == typeof(Task))
            {
                return methodCall;
            }
            else if (returnType == typeof(ValueTask))
            {
                return Expression.Call(
                    ExecuteValueTaskMethod,
                    methodCall);
            }
            else if (returnType.IsGenericType &&
                     returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var typeArg = returnType.GetGenericArguments()[0];

                if (typeof(IResult).IsAssignableFrom(typeArg))
                {
                    return Expression.Call(
                        ExecuteTaskResultOfTMethod.MakeGenericMethod(typeArg),
                        methodCall,
                        HttpContextExpr);
                }
                // ExecuteTask<T>(handler(..), httpContext);
                else if (typeArg == typeof(string))
                {
                    return Expression.Call(
                        ExecuteTaskOfStringMethod,
                        methodCall,
                        HttpContextExpr);
                }
                else if (typeArg == typeof(object))
                {
                    return Expression.Call(
                        ExecuteTaskOfObjectMethod,
                        methodCall,
                        HttpContextExpr,
                        Expression.Constant(factoryContext.JsonSerializerOptions.GetReadOnlyTypeInfo(typeof(object)), typeof(JsonTypeInfo<object>)));
                }
                else
                {
                    var jsonTypeInfo = factoryContext.JsonSerializerOptions.GetReadOnlyTypeInfo(typeArg);

                    if (jsonTypeInfo.HasKnownPolymorphism())
                    {
                        return Expression.Call(
                            ExecuteTaskOfTFastMethod.MakeGenericMethod(typeArg),
                            methodCall,
                            HttpContextExpr,
                            Expression.Constant(jsonTypeInfo, typeof(JsonTypeInfo<>).MakeGenericType(typeArg)));
                    }

                    return Expression.Call(
                        ExecuteTaskOfTMethod.MakeGenericMethod(typeArg),
                        methodCall,
                        HttpContextExpr,
                        Expression.Constant(jsonTypeInfo, typeof(JsonTypeInfo<>).MakeGenericType(typeArg)));
                }
            }
            else if (returnType.IsGenericType &&
                     returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                var typeArg = returnType.GetGenericArguments()[0];

                if (typeof(IResult).IsAssignableFrom(typeArg))
                {
                    return Expression.Call(
                        ExecuteValueResultTaskOfTMethod.MakeGenericMethod(typeArg),
                        methodCall,
                        HttpContextExpr);
                }
                // ExecuteTask<T>(handler(..), httpContext);
                else if (typeArg == typeof(string))
                {
                    return Expression.Call(
                        ExecuteValueTaskOfStringMethod,
                        methodCall,
                        HttpContextExpr);
                }
                else if (typeArg == typeof(object))
                {
                    return Expression.Call(
                        ExecuteValueTaskOfObjectMethod,
                        methodCall,
                        HttpContextExpr,
                        Expression.Constant(factoryContext.JsonSerializerOptions.GetReadOnlyTypeInfo(typeof(object)), typeof(JsonTypeInfo<object>)));
                }
                else
                {
                    var jsonTypeInfo = factoryContext.JsonSerializerOptions.GetReadOnlyTypeInfo(typeArg);

                    if (jsonTypeInfo.HasKnownPolymorphism())
                    {
                        return Expression.Call(
                            ExecuteValueTaskOfTFastMethod.MakeGenericMethod(typeArg),
                            methodCall,
                            HttpContextExpr,
                            Expression.Constant(jsonTypeInfo, typeof(JsonTypeInfo<>).MakeGenericType(typeArg)));
                    }

                    return Expression.Call(
                        ExecuteValueTaskOfTMethod.MakeGenericMethod(typeArg),
                        methodCall,
                        HttpContextExpr,
                        Expression.Constant(jsonTypeInfo, typeof(JsonTypeInfo<>).MakeGenericType(typeArg)));
                }
            }
            else
            {
                // TODO: Handle custom awaitables
                throw GetUnsupportedReturnTypeException(returnType);
            }
        }
        else if (typeof(IResult).IsAssignableFrom(returnType))
        {
            if (returnType.IsValueType)
            {
                var box = Expression.TypeAs(methodCall, typeof(IResult));
                return Expression.Call(ResultWriteResponseAsyncMethod, box, HttpContextExpr);
            }
            return Expression.Call(ResultWriteResponseAsyncMethod, methodCall, HttpContextExpr);
        }
        else if (returnType == typeof(string))
        {
            return Expression.Call(StringResultWriteResponseAsyncMethod, HttpContextExpr, methodCall);
        }
        else if (returnType.IsByRefLike)
        {
            throw GetUnsupportedReturnTypeException(returnType);
        }
        else
        {
            var jsonTypeInfo = factoryContext.JsonSerializerOptions.GetReadOnlyTypeInfo(returnType);

            if (jsonTypeInfo.HasKnownPolymorphism())
            {
                return Expression.Call(
                    JsonResultWriteResponseOfTFastAsyncMethod.MakeGenericMethod(returnType),
                    HttpResponseExpr,
                    methodCall,
                    Expression.Constant(jsonTypeInfo, typeof(JsonTypeInfo<>).MakeGenericType(returnType)));

            }

            return Expression.Call(
                JsonResultWriteResponseOfTAsyncMethod.MakeGenericMethod(returnType),
                HttpResponseExpr,
                methodCall,
                Expression.Constant(jsonTypeInfo, typeof(JsonTypeInfo<>).MakeGenericType(returnType)));
        }
    }

    private static Func<object?, HttpContext, Task> HandleRequestBodyAndCompileRequestDelegate(Expression responseWritingMethodCall, RequestDelegateFactoryContext factoryContext)
    {
        if (factoryContext.JsonRequestBodyParameter is null && !factoryContext.ReadForm)
        {
            if (factoryContext.ParameterBinders.Count > 0)
            {
                // We need to generate the code for reading from the custom binders calling into the delegate
                var continuation = Expression.Lambda<Func<object?, HttpContext, object?[], Task>>(
                    responseWritingMethodCall, TargetExpr, HttpContextExpr, BoundValuesArrayExpr).Compile();

                // Looping over arrays is faster
                var binders = factoryContext.ParameterBinders.ToArray();
                var count = binders.Length;

                return async (target, httpContext) =>
                {
                    var boundValues = new object?[count];

                    for (var i = 0; i < count; i++)
                    {
                        boundValues[i] = await binders[i](httpContext);
                    }

                    await continuation(target, httpContext, boundValues);
                };
            }

            return Expression.Lambda<Func<object?, HttpContext, Task>>(
                responseWritingMethodCall, TargetExpr, HttpContextExpr).Compile();
        }

        if (factoryContext.ReadForm)
        {
            return HandleRequestBodyAndCompileRequestDelegateForForm(responseWritingMethodCall, factoryContext);
        }
        else
        {
            return HandleRequestBodyAndCompileRequestDelegateForJson(responseWritingMethodCall, factoryContext);
        }
    }

    private static Func<object?, HttpContext, Task> HandleRequestBodyAndCompileRequestDelegateForJson(Expression responseWritingMethodCall, RequestDelegateFactoryContext factoryContext)
    {
        Debug.Assert(factoryContext.JsonRequestBodyParameter is not null, "factoryContext.JsonRequestBodyParameter is null for a JSON body.");

        var bodyType = factoryContext.JsonRequestBodyParameter.ParameterType;
        var jsonTypeInfo = factoryContext.JsonSerializerOptions.GetReadOnlyTypeInfo(bodyType);
        var parameterTypeName = TypeNameHelper.GetTypeDisplayName(factoryContext.JsonRequestBodyParameter.ParameterType, fullName: false);
        var parameterName = factoryContext.JsonRequestBodyParameter.Name;

        Debug.Assert(parameterName is not null, "CreateArgument() should throw if parameter.Name is null.");

        if (factoryContext.ParameterBinders.Count > 0)
        {
            // We need to generate the code for reading from the body before calling into the delegate
            var continuation = Expression.Lambda<Func<object?, HttpContext, object?, object?[], Task>>(
            responseWritingMethodCall, TargetExpr, HttpContextExpr, BodyValueExpr, BoundValuesArrayExpr).Compile();

            // Looping over arrays is faster
            var binders = factoryContext.ParameterBinders.ToArray();
            var count = binders.Length;

            return async (target, httpContext) =>
            {
                // Run these first so that they can potentially read and rewind the body
                var boundValues = new object?[count];

                for (var i = 0; i < count; i++)
                {
                    boundValues[i] = await binders[i](httpContext);
                }

                var (bodyValue, successful) = await TryReadBodyAsync(
                    httpContext,
                    bodyType,
                    parameterTypeName,
                    parameterName,
                    factoryContext.AllowEmptyRequestBody,
                    factoryContext.ThrowOnBadRequest,
                    jsonTypeInfo);

                if (!successful)
                {
                    return;
                }

                await continuation(target, httpContext, bodyValue, boundValues);
            };
        }
        else
        {
            // We need to generate the code for reading from the body before calling into the delegate
            var continuation = Expression.Lambda<Func<object?, HttpContext, object?, Task>>(
            responseWritingMethodCall, TargetExpr, HttpContextExpr, BodyValueExpr).Compile();

            return async (target, httpContext) =>
            {
                var (bodyValue, successful) = await TryReadBodyAsync(
                    httpContext,
                    bodyType,
                    parameterTypeName,
                    parameterName,
                    factoryContext.AllowEmptyRequestBody,
                    factoryContext.ThrowOnBadRequest,
                    jsonTypeInfo);

                if (!successful)
                {
                    return;
                }

                await continuation(target, httpContext, bodyValue);
            };
        }

        static async Task<(object? FormValue, bool Successful)> TryReadBodyAsync(
            HttpContext httpContext,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type bodyType,
            string parameterTypeName,
            string parameterName,
            bool allowEmptyRequestBody,
            bool throwOnBadRequest,
            JsonTypeInfo jsonTypeInfo)
        {
            object? defaultBodyValue = null;

            if (allowEmptyRequestBody && bodyType.IsValueType)
            {
                defaultBodyValue = CreateValueType(bodyType);
            }

            var bodyValue = defaultBodyValue;
            var feature = httpContext.Features.Get<IHttpRequestBodyDetectionFeature>();

            if (feature?.CanHaveBody == true)
            {
                if (!httpContext.Request.HasJsonContentType())
                {
                    Log.UnexpectedJsonContentType(httpContext, httpContext.Request.ContentType, throwOnBadRequest);
                    httpContext.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    return (null, false);
                }
                try
                {
                    bodyValue = await httpContext.Request.ReadFromJsonAsync(jsonTypeInfo);
                }
                catch (BadHttpRequestException ex)
                {
                    Log.RequestBodyIOException(httpContext, ex);
                    httpContext.Response.StatusCode = ex.StatusCode;
                    return (null, false);
                }
                catch (IOException ex)
                {
                    Log.RequestBodyIOException(httpContext, ex);
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return (null, false);
                }
                catch (JsonException ex)
                {
                    Log.InvalidJsonRequestBody(httpContext, parameterTypeName, parameterName, ex, throwOnBadRequest);
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return (null, false);
                }
            }

            return (bodyValue, true);
        }
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
        Justification = "CreateValueType is only called on a ValueType. You can always create an instance of a ValueType.")]
    private static object? CreateValueType(Type t) => RuntimeHelpers.GetUninitializedObject(t);

    private static Func<object?, HttpContext, Task> HandleRequestBodyAndCompileRequestDelegateForForm(
        Expression responseWritingMethodCall,
        RequestDelegateFactoryContext factoryContext)
    {
        Debug.Assert(factoryContext.FirstFormRequestBodyParameter is not null, "factoryContext.FirstFormRequestBodyParameter is null for a form body.");

        // If there are multiple parameters associated with the form, just use the name of
        // the first one to report the failure to bind the parameter if reading the form fails.
        var parameterTypeName = TypeNameHelper.GetTypeDisplayName(factoryContext.FirstFormRequestBodyParameter.ParameterType, fullName: false);
        var parameterName = factoryContext.FirstFormRequestBodyParameter.Name;

        Debug.Assert(parameterName is not null, "CreateArgument() should throw if parameter.Name is null.");

        if (factoryContext.ParameterBinders.Count > 0)
        {
            // We need to generate the code for reading from the body or form before calling into the delegate
            var continuation = Expression.Lambda<Func<object?, HttpContext, object?, object?[], Task>>(
            responseWritingMethodCall, TargetExpr, HttpContextExpr, BodyValueExpr, BoundValuesArrayExpr).Compile();

            // Looping over arrays is faster
            var binders = factoryContext.ParameterBinders.ToArray();
            var count = binders.Length;

            return async (target, httpContext) =>
            {
                // Run these first so that they can potentially read and rewind the body
                var boundValues = new object?[count];

                for (var i = 0; i < count; i++)
                {
                    boundValues[i] = await binders[i](httpContext);
                }

                var (formValue, successful) = await TryReadFormAsync(
                    httpContext,
                    parameterTypeName,
                    parameterName,
                    factoryContext.ThrowOnBadRequest);

                if (!successful)
                {
                    return;
                }

                await continuation(target, httpContext, formValue, boundValues);
            };
        }
        else
        {
            // We need to generate the code for reading from the form before calling into the delegate
            var continuation = Expression.Lambda<Func<object?, HttpContext, object?, Task>>(
            responseWritingMethodCall, TargetExpr, HttpContextExpr, BodyValueExpr).Compile();

            return async (target, httpContext) =>
            {
                var (formValue, successful) = await TryReadFormAsync(
                    httpContext,
                    parameterTypeName,
                    parameterName,
                    factoryContext.ThrowOnBadRequest);

                if (!successful)
                {
                    return;
                }

                await continuation(target, httpContext, formValue);
            };
        }

        static async Task<(object? FormValue, bool Successful)> TryReadFormAsync(
            HttpContext httpContext,
            string parameterTypeName,
            string parameterName,
            bool throwOnBadRequest)
        {
            object? formValue = null;
            var feature = httpContext.Features.Get<IHttpRequestBodyDetectionFeature>();

            if (feature?.CanHaveBody == false)
            {
                Log.UnexpectedRequestWithoutBody(httpContext, parameterTypeName, parameterName, throwOnBadRequest);
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return (null, false);
            }

            if (httpContext.Features.Get<IAntiforgeryValidationFeature>() is { IsValid: false } antiforgeryValidationFeature)
            {
                Log.InvalidAntiforgeryToken(httpContext, parameterTypeName, parameterName, antiforgeryValidationFeature.Error!, throwOnBadRequest);
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return (null, false);
            }

            if (!httpContext.Request.HasFormContentType)
            {
                Log.UnexpectedNonFormContentType(httpContext, httpContext.Request.ContentType, throwOnBadRequest);
                httpContext.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                return (null, false);
            }

            try
            {
                formValue = await httpContext.Request.ReadFormAsync();
            }
            catch (BadHttpRequestException ex)
            {
                Log.RequestBodyIOException(httpContext, ex);
                httpContext.Response.StatusCode = ex.StatusCode;
                return (null, false);
            }
            catch (IOException ex)
            {
                Log.RequestBodyIOException(httpContext, ex);
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return (null, false);
            }
            catch (InvalidDataException ex)
            {
                Log.InvalidFormRequestBody(httpContext, parameterTypeName, parameterName, ex, throwOnBadRequest);
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return (null, false);
            }

            return (formValue, true);
        }
    }

    private static Expression GetValueFromProperty(MemberExpression sourceExpression, PropertyInfo itemProperty, string key, Type? returnType = null)
    {
        var indexArguments = new[] { Expression.Constant(key) };
        var indexExpression = Expression.MakeIndex(sourceExpression, itemProperty, indexArguments);
        return Expression.Convert(indexExpression, returnType ?? typeof(string));
    }

    private static Expression BindParameterFromProperties(ParameterInfo parameter, RequestDelegateFactoryContext factoryContext)
    {
        var parameterType = parameter.ParameterType;
        var isNullable = Nullable.GetUnderlyingType(parameterType) != null ||
            factoryContext.NullabilityContext.Create(parameter)?.ReadState == NullabilityState.Nullable;

        if (isNullable)
        {
            throw new InvalidOperationException($"The nullable type '{TypeNameHelper.GetTypeDisplayName(parameter.ParameterType, fullName: false)}' is not supported, mark the parameter as non-nullable.");
        }

        var argumentExpression = Expression.Variable(parameter.ParameterType, $"{parameter.Name}_local");
        var (constructor, parameters) = ParameterBindingMethodCache.Instance.FindConstructor(parameterType);

        Expression initExpression;

        if (constructor is not null && parameters is { Length: > 0 })
        {
            //  arg_local = new T(....)

            var constructorArguments = new Expression[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameterInfo =
                    new PropertyAsParameterInfo(parameters[i].PropertyInfo, parameters[i].ParameterInfo, factoryContext.NullabilityContext);
                Debug.Assert(parameterInfo.Name != null, "Parameter name must be set for parameters resolved from properties.");
                constructorArguments[i] = CreateArgument(parameterInfo, factoryContext, out var hasTryParse, out var hasBindAsync, out var _);
                factoryContext.Parameters.Add(parameterInfo);
                factoryContext.EndpointBuilder.Metadata.Add(new ParameterBindingMetadata(parameterInfo.Name, parameterInfo, hasTryParse: hasTryParse, hasBindAsync: hasBindAsync, isOptional: parameterInfo.IsOptional));
            }

            initExpression = Expression.New(constructor, constructorArguments);
        }
        else
        {
            //  arg_local = new T()
            //  {
            //      arg_local.Property[0] = expression[0],
            //      arg_local.Property[n] = expression[n],
            //  }

            var properties = parameterType.GetProperties();
            var bindings = new List<MemberBinding>(properties.Length);

            for (var i = 0; i < properties.Length; i++)
            {
                // For parameterless ctor we will init only writable properties.
                if (properties[i].CanWrite && properties[i].GetSetMethod(nonPublic: false) != null)
                {
                    var parameterInfo = new PropertyAsParameterInfo(properties[i], factoryContext.NullabilityContext);
                    Debug.Assert(parameterInfo.Name != null, "Parameter name must be set for parameters resolved from properties.");
                    bindings.Add(Expression.Bind(properties[i], CreateArgument(parameterInfo, factoryContext, out var hasTryParse, out var hasBindAsync, out var _)));
                    factoryContext.Parameters.Add(parameterInfo);
                    factoryContext.EndpointBuilder.Metadata.Add(new ParameterBindingMetadata(parameterInfo.Name, parameterInfo, hasTryParse: hasTryParse, hasBindAsync: hasBindAsync, isOptional: parameterInfo.IsOptional));
                }
            }

            var newExpression = constructor is null ?
                Expression.New(parameterType) :
                Expression.New(constructor);

            initExpression = Expression.MemberInit(newExpression, bindings);
        }

        factoryContext.ParamCheckExpressions.Add(
            Expression.Assign(argumentExpression, initExpression));

        factoryContext.TrackedParameters.Add(parameter.Name!, RequestDelegateFactoryConstants.PropertyAsParameter);
        factoryContext.ExtraLocals.Add(argumentExpression);

        return argumentExpression;
    }

    private static Expression BindParameterFromService(ParameterInfo parameter, RequestDelegateFactoryContext factoryContext)
    {
        var isOptional = IsOptionalParameter(parameter, factoryContext);

        if (isOptional)
        {
            return Expression.Call(GetServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
        }
        return Expression.Call(GetRequiredServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
    }

    private static Expression BindParameterFromKeyedService(ParameterInfo parameter, object key, RequestDelegateFactoryContext factoryContext)
    {
        var isOptional = IsOptionalParameter(parameter, factoryContext);

        if (isOptional)
        {
            return Expression.Call(GetKeyedServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr, Expression.Convert(
                Expression.Constant(key),
                typeof(object)));
        }
        return Expression.Call(GetRequiredKeyedServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr, Expression.Convert(
            Expression.Constant(key),
            typeof(object)));
    }

    private static Expression BindParameterFromValue(ParameterInfo parameter, Expression valueExpression, RequestDelegateFactoryContext factoryContext, string source)
    {
        if (parameter.ParameterType == typeof(string) || parameter.ParameterType == typeof(string[])
            || parameter.ParameterType == typeof(StringValues) || parameter.ParameterType == typeof(StringValues?))
        {
            return BindParameterFromExpression(parameter, valueExpression, factoryContext, source);
        }

        var isOptional = IsOptionalParameter(parameter, factoryContext);
        var argument = Expression.Variable(parameter.ParameterType, $"{parameter.Name}_local");

        var parameterTypeNameConstant = Expression.Constant(TypeNameHelper.GetTypeDisplayName(parameter.ParameterType, fullName: false));
        var parameterNameConstant = Expression.Constant(parameter.Name);
        var sourceConstant = Expression.Constant(source);

        factoryContext.UsingTempSourceString = true;

        var targetParseType = parameter.ParameterType.IsArray ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;

        var underlyingNullableType = Nullable.GetUnderlyingType(targetParseType);
        var isNotNullable = underlyingNullableType is null;

        var nonNullableParameterType = underlyingNullableType ?? targetParseType;
        var tryParseMethodCall = ParameterBindingMethodCache.Instance.FindTryParseMethod(nonNullableParameterType);

        if (tryParseMethodCall is null)
        {
            var typeName = TypeNameHelper.GetTypeDisplayName(targetParseType, fullName: false);
            throw new InvalidOperationException($"{parameter.Name} must have a valid TryParse method to support converting from a string. No public static bool {typeName}.TryParse(string, out {typeName}) method found for {parameter.Name}.");
        }

        // string tempSourceString;
        // bool wasParamCheckFailure = false;
        //
        // // Assume "int param1" is the first parameter and "[FromRoute] int? param2 = 42" is the second parameter.
        // int param1_local;
        // int? param2_local;
        //
        // tempSourceString = httpContext.RouteValue["param1"] ?? httpContext.Query["param1"];
        //
        // if (tempSourceString != null)
        // {
        //     if (!int.TryParse(tempSourceString, out param1_local))
        //     {
        //         wasParamCheckFailure = true;
        //         Log.ParameterBindingFailed(httpContext, "Int32", "id", tempSourceString)
        //     }
        // }
        //
        // tempSourceString = httpContext.RouteValue["param2"];
        //
        // if (tempSourceString != null)
        // {
        //     if (int.TryParse(tempSourceString, out int parsedValue))
        //     {
        //         param2_local = parsedValue;
        //     }
        //     else
        //     {
        //         wasParamCheckFailure = true;
        //         Log.ParameterBindingFailed(httpContext, "Int32", "id", tempSourceString)
        //     }
        // }
        // else
        // {
        //     param2_local = 42;
        // }

        // string[]? values = httpContext.Request.Query["param1"].ToArray();
        // int[] param_local = values.Length > 0 ? new int[values.Length] : Array.Empty<int>();

        // if (values != null)
        // {
        //     int index = 0;
        //     while (index < values.Length)
        //     {
        //         tempSourceString = values[i];
        //         if (int.TryParse(tempSourceString, out var parsedValue))
        //         {
        //             param_local[i] = parsedValue;
        //         }
        //         else
        //         {
        //             wasParamCheckFailure = true;
        //             Log.ParameterBindingFailed(httpContext, "Int32[]", "param1", tempSourceString);
        //             break;
        //         }
        //
        //         index++
        //     }
        // }

        // If the parameter is nullable, create a "parsedValue" local to TryParse into since we cannot use the parameter directly.
        var parsedValue = Expression.Variable(nonNullableParameterType, "parsedValue");

        var failBlock = Expression.Block(
            Expression.Assign(WasParamCheckFailureExpr, Expression.Constant(true)),
            Expression.Call(LogParameterBindingFailedMethod,
                HttpContextExpr, parameterTypeNameConstant, parameterNameConstant,
                TempSourceStringExpr, Expression.Constant(factoryContext.ThrowOnBadRequest)));

        var tryParseCall = tryParseMethodCall(parsedValue, Expression.Constant(CultureInfo.InvariantCulture));

        // The following code is generated if the parameter is required and
        // the method should not be matched.
        //
        // if (tempSourceString == null)
        // {
        //      wasParamCheckFailure = true;
        //      Log.RequiredParameterNotProvided(httpContext, "Int32", "param1");
        // }
        var checkRequiredParaseableParameterBlock = Expression.Block(
            Expression.IfThen(TempSourceStringNullExpr,
                Expression.Block(
                    Expression.Assign(WasParamCheckFailureExpr, Expression.Constant(true)),
                    Expression.Call(LogRequiredParameterNotProvidedMethod,
                        HttpContextExpr, parameterTypeNameConstant, parameterNameConstant, sourceConstant,
                        Expression.Constant(factoryContext.ThrowOnBadRequest))
                )
            )
        );

        var index = Expression.Variable(typeof(int), "index");

        // If the parameter is nullable, we need to assign the "parsedValue" local to the nullable parameter on success.
        var tryParseExpression = Expression.Block(new[] { parsedValue },
                Expression.IfThenElse(tryParseCall,
                    Expression.Assign(parameter.ParameterType.IsArray ? Expression.ArrayAccess(argument, index) : argument, Expression.Convert(parsedValue, targetParseType)),
                    failBlock));

        var ifNotNullTryParse = !parameter.HasDefaultValue
            ? Expression.IfThen(TempSourceStringNotNullExpr, tryParseExpression)
            : Expression.IfThenElse(TempSourceStringNotNullExpr, tryParseExpression,
                Expression.Assign(argument,
                Expression.Constant(parameter.DefaultValue, parameter.ParameterType)));

        var loopExit = Expression.Label();

        // REVIEW: We can reuse this like we reuse temp source string
        var stringArrayExpr = parameter.ParameterType.IsArray ? Expression.Variable(typeof(string[]), "tempStringArray") : null;
        var elementTypeNullabilityInfo = parameter.ParameterType.IsArray ? factoryContext.NullabilityContext.Create(parameter)?.ElementType : null;

        // Determine optionality of the element type of the array
        var elementTypeOptional = !isNotNullable || (elementTypeNullabilityInfo?.ReadState != NullabilityState.NotNull);

        // The loop that populates the resulting array values
        var arrayLoop = parameter.ParameterType.IsArray ? Expression.Block(
                        // param_local = new int[values.Length];
                        Expression.Assign(argument, Expression.NewArrayBounds(parameter.ParameterType.GetElementType()!, Expression.ArrayLength(stringArrayExpr!))),
                        // index = 0
                        Expression.Assign(index, Expression.Constant(0)),
                        // while (index < values.Length)
                        Expression.Loop(
                            Expression.Block(
                                Expression.IfThenElse(
                                    Expression.LessThan(index, Expression.ArrayLength(stringArrayExpr!)),
                                        // tempSourceString = values[index];
                                        Expression.Block(
                                            Expression.Assign(TempSourceStringExpr, Expression.ArrayIndex(stringArrayExpr!, index)),
                                            elementTypeOptional ? Expression.IfThen(TempSourceStringIsNotNullOrEmptyExpr, tryParseExpression)
                                                                : tryParseExpression
                                        ),
                                       // else break
                                       Expression.Break(loopExit)
                                 ),
                                 // index++
                                 Expression.PostIncrementAssign(index)
                            )
                        , loopExit)
                    ) : null;

        var fullParamCheckBlock = (parameter.ParameterType.IsArray, isOptional) switch
        {
            // (isArray: true, optional: true)
            (true, true) =>

            Expression.Block(
                new[] { index, stringArrayExpr! },
                // values = httpContext.Request.Query["id"];
                Expression.Assign(stringArrayExpr!, valueExpression),
                Expression.IfThen(
                    Expression.NotEqual(stringArrayExpr!, Expression.Constant(null)),
                    arrayLoop!
                )
            ),

            // (isArray: true, optional: false)
            (true, false) =>

            Expression.Block(
                new[] { index, stringArrayExpr! },
                // values = httpContext.Request.Query["id"];
                Expression.Assign(stringArrayExpr!, valueExpression),
                Expression.IfThenElse(
                    Expression.NotEqual(stringArrayExpr!, Expression.Constant(null)),
                    arrayLoop!,
                    failBlock
                )
            ),

            // (isArray: false, optional: false)
            (false, false) =>

            Expression.Block(
                // tempSourceString = httpContext.RequestValue["id"];
                Expression.Assign(TempSourceStringExpr, valueExpression),
                // if (tempSourceString == null) { ... } only produced when parameter is required
                checkRequiredParaseableParameterBlock,
                // if (tempSourceString != null) { ... }
                ifNotNullTryParse),

            // (isArray: false, optional: true)
            (false, true) =>

            Expression.Block(
                // tempSourceString = httpContext.RequestValue["id"];
                Expression.Assign(TempSourceStringExpr, valueExpression),
                // if (tempSourceString != null) { ... }
                ifNotNullTryParse)
        };

        factoryContext.ExtraLocals.Add(argument);
        factoryContext.ParamCheckExpressions.Add(fullParamCheckBlock);

        return argument;
    }

    private static Expression BindParameterFromExpression(
        ParameterInfo parameter,
        Expression valueExpression,
        RequestDelegateFactoryContext factoryContext,
        string source)
    {
        var nullability = factoryContext.NullabilityContext.Create(parameter);
        var isOptional = IsOptionalParameter(parameter, factoryContext);

        var argument = Expression.Variable(parameter.ParameterType, $"{parameter.Name}_local");

        var parameterTypeNameConstant = Expression.Constant(TypeNameHelper.GetTypeDisplayName(parameter.ParameterType, fullName: false));
        var parameterNameConstant = Expression.Constant(parameter.Name);
        var sourceConstant = Expression.Constant(source);

        if (!isOptional)
        {
            // The following is produced if the parameter is required:
            //
            // argument = value["param1"];
            // if (argument == null)
            // {
            //      wasParamCheckFailure = true;
            //      Log.RequiredParameterNotProvided(httpContext, "TypeOfValue", "param1");
            // }
            var checkRequiredStringParameterBlock = Expression.Block(
                Expression.Assign(argument, valueExpression),
                Expression.IfThen(Expression.Equal(argument, Expression.Constant(null)),
                    Expression.Block(
                        Expression.Assign(WasParamCheckFailureExpr, Expression.Constant(true)),
                        Expression.Call(LogRequiredParameterNotProvidedMethod,
                            HttpContextExpr, parameterTypeNameConstant, parameterNameConstant, sourceConstant,
                            Expression.Constant(factoryContext.ThrowOnBadRequest))
                    )
                )
            );

            // NOTE: when StringValues is used as a parameter, value["some_unpresent_parameter"] returns StringValue.Empty, and it's equivalent to (string?)null

            factoryContext.ExtraLocals.Add(argument);
            factoryContext.ParamCheckExpressions.Add(checkRequiredStringParameterBlock);
            return argument;
        }

        // Allow nullable parameters that don't have a default value
        if (nullability.ReadState != NullabilityState.NotNull && !parameter.HasDefaultValue)
        {
            if (parameter.ParameterType == typeof(StringValues?))
            {
                // when Nullable<StringValues> is used and the actual value is StringValues.Empty, we should pass in a Nullable<StringValues>
                return Expression.Block(
                    Expression.Condition(Expression.Equal(valueExpression, Expression.Convert(Expression.Constant(StringValues.Empty), parameter.ParameterType)),
                            Expression.Convert(Expression.Constant(null), parameter.ParameterType),
                            valueExpression
                        )
                    );
            }
            return valueExpression;
        }

        // The following is produced if the parameter is optional. Note that we convert the
        // default value to the target ParameterType to address scenarios where the user is
        // is setting null as the default value in a context where nullability is disabled.
        //
        // param1_local = httpContext.RouteValue["param1"] ?? httpContext.Query["param1"];
        // param1_local != null ? param1_local : Convert(null, Int32)
        return Expression.Block(
            Expression.Condition(Expression.NotEqual(valueExpression, Expression.Constant(null)),
                valueExpression,
                Expression.Convert(Expression.Constant(parameter.DefaultValue), parameter.ParameterType)));
    }

    private static Expression BindParameterFromProperty(ParameterInfo parameter, MemberExpression property, PropertyInfo itemProperty, string key, RequestDelegateFactoryContext factoryContext, string source) =>
        BindParameterFromValue(parameter, GetValueFromProperty(property, itemProperty, key, GetExpressionType(parameter.ParameterType)), factoryContext, source);

    private static Type? GetExpressionType(Type type) =>
        type.IsArray ? typeof(string[]) :
        type == typeof(StringValues) ? typeof(StringValues) :
        type == typeof(StringValues?) ? typeof(StringValues?) :
        null;

    private static Expression BindParameterFromRouteValueOrQueryString(ParameterInfo parameter, string key, RequestDelegateFactoryContext factoryContext)
    {
        var routeValue = GetValueFromProperty(RouteValuesExpr, RouteValuesIndexerProperty, key);
        var queryValue = GetValueFromProperty(QueryExpr, QueryIndexerProperty, key);
        return BindParameterFromValue(parameter, Expression.Coalesce(routeValue, queryValue), factoryContext, "route or query string");
    }

    private static Expression BindParameterFromBindAsync(ParameterInfo parameter, RequestDelegateFactoryContext factoryContext)
    {
        // We reference the boundValues array by parameter index here
        var isOptional = IsOptionalParameter(parameter, factoryContext);

        // Get the BindAsync method for the type.
        var bindAsyncMethod = ParameterBindingMethodCache.Instance.FindBindAsyncMethod(parameter);
        // We know BindAsync exists because there's no way to opt-in without defining the method on the type.
        Debug.Assert(bindAsyncMethod.Expression is not null);

        // Compile the delegate to the BindAsync method for this parameter index
        var bindAsyncDelegate = Expression.Lambda<Func<HttpContext, ValueTask<object?>>>(bindAsyncMethod.Expression, HttpContextExpr).Compile();
        factoryContext.ParameterBinders.Add(bindAsyncDelegate);

        // boundValues[index]
        var boundValueExpr = Expression.ArrayIndex(BoundValuesArrayExpr, Expression.Constant(factoryContext.ParameterBinders.Count - 1));

        if (!isOptional)
        {
            var typeName = TypeNameHelper.GetTypeDisplayName(parameter.ParameterType, fullName: false);
            var message = bindAsyncMethod.ParamCount == 2 ? $"{typeName}.BindAsync(HttpContext, ParameterInfo)" : $"{typeName}.BindAsync(HttpContext)";
            var checkRequiredBodyBlock = Expression.Block(
                    Expression.IfThen(
                    Expression.Equal(boundValueExpr, Expression.Constant(null)),
                        Expression.Block(
                            Expression.Assign(WasParamCheckFailureExpr, Expression.Constant(true)),
                            Expression.Call(LogRequiredParameterNotProvidedMethod,
                                    HttpContextExpr,
                                    Expression.Constant(typeName),
                                    Expression.Constant(parameter.Name),
                                    Expression.Constant(message),
                                    Expression.Constant(factoryContext.ThrowOnBadRequest))
                        )
                    )
                );

            factoryContext.ParamCheckExpressions.Add(checkRequiredBodyBlock);
        }

        // (ParameterType)boundValues[i]
        return Expression.Convert(boundValueExpr, parameter.ParameterType);
    }

    private static void AddInferredAcceptsMetadata(RequestDelegateFactoryContext factoryContext, Type type, string[] contentTypes)
    {
        if (factoryContext.MetadataAlreadyInferred)
        {
            return;
        }

        factoryContext.EndpointBuilder.Metadata.Add(new AcceptsMetadata(contentTypes, type, factoryContext.AllowEmptyRequestBody));
    }

    private static void InferFormAcceptsMetadata(RequestDelegateFactoryContext factoryContext)
    {
        if (factoryContext.ReadFormFile)
        {
            AddInferredAcceptsMetadata(factoryContext, factoryContext.FirstFormRequestBodyParameter!.ParameterType, FormFileContentType);
        }
        else
        {
            AddInferredAcceptsMetadata(factoryContext, factoryContext.FirstFormRequestBodyParameter!.ParameterType, FormContentType);
        }
    }

    private static void InferAntiforgeryMetadata(RequestDelegateFactoryContext factoryContext)
    {
        if (factoryContext.MetadataAlreadyInferred)
        {
            return;
        }

        factoryContext.EndpointBuilder.Metadata.Add(AntiforgeryMetadata.ValidationRequired);
    }

    private static Expression BindParameterFromFormCollection(
        ParameterInfo parameter,
        RequestDelegateFactoryContext factoryContext)
    {
        factoryContext.FirstFormRequestBodyParameter ??= parameter;
        factoryContext.TrackedParameters.Add(parameter.Name!, RequestDelegateFactoryConstants.FormCollectionParameter);
        factoryContext.ReadForm = true;

        return BindParameterFromExpression(
            parameter,
            FormExpr,
            factoryContext,
            "body");
    }

    private static Expression BindParameterFromFormItem(
        ParameterInfo parameter,
        string key,
        RequestDelegateFactoryContext factoryContext)
    {
        var valueExpression = GetValueFromProperty(FormExpr, FormIndexerProperty, key, GetExpressionType(parameter.ParameterType));

        factoryContext.FirstFormRequestBodyParameter ??= parameter;
        factoryContext.TrackedParameters.Add(key, RequestDelegateFactoryConstants.FormAttribute);
        factoryContext.ReadForm = true;

        return BindParameterFromValue(
            parameter,
            valueExpression,
            factoryContext,
            "form");
    }

    private static void UpdateFormBindingArgumentExpressions(RequestDelegateFactoryContext factoryContext)
    {
        if (factoryContext.ArgumentExpressions == null || factoryContext.ArgumentExpressions.Length == 0)
        {
            return;
        }

        for (var i = 0; i < factoryContext.ArgumentExpressions.Length; i++)
        {
            var parameter = factoryContext.Parameters[i];
            var formAttribute = parameter.GetCustomAttributes().OfType<IFromFormMetadata>().FirstOrDefault();
            var key = formAttribute == null || string.IsNullOrEmpty(formAttribute.Name) ? parameter.Name! : formAttribute.Name;
            if (factoryContext.TrackedParameters.TryGetValue(key, out var trackedParameter) && trackedParameter == RequestDelegateFactoryConstants.FormBindingAttribute)
            {
                factoryContext.ArgumentExpressions[i] = BindComplexParameterFromFormItem(parameter, key, factoryContext, true);
            }
        }
    }

    private static Expression BindComplexParameterFromFormItem(
        ParameterInfo parameter,
        string key,
        RequestDelegateFactoryContext factoryContext,
        bool setExpressions = false)
    {
        factoryContext.FirstFormRequestBodyParameter ??= parameter;
        factoryContext.TrackedParameters.TryAdd(key, RequestDelegateFactoryConstants.FormBindingAttribute);
        factoryContext.ReadForm = true;

        // var name_local;
        var formArgument = Expression.Variable(parameter.ParameterType, $"{parameter.Name}_local");

        // Delay setting the generated LINQ expressions until
        // metadata has already been inferred so that we can read from `FormMappingOptionsMetadata`.
        if (!setExpressions)
        {
            return formArgument;
        }

        var formDataMapperOptions = factoryContext.FormDataMapperOptions;
        var formMappingOptionsMetadatas = factoryContext.EndpointBuilder.Metadata.OfType<FormMappingOptionsMetadata>();
        foreach (var formMappingOptionsMetadata in formMappingOptionsMetadatas)
        {
            formDataMapperOptions.MaxRecursionDepth = formMappingOptionsMetadata.MaxRecursionDepth ?? formDataMapperOptions.MaxRecursionDepth;
            formDataMapperOptions.MaxCollectionSize = formMappingOptionsMetadata.MaxCollectionSize ?? formDataMapperOptions.MaxCollectionSize;
            formDataMapperOptions.MaxKeyBufferSize = formMappingOptionsMetadata.MaxKeySize ?? formDataMapperOptions.MaxKeyBufferSize;
        }

        // var name_reader;
        // var form_dict;
        // var form_buffer;
        var formReader = Expression.Variable(typeof(FormDataReader), $"{parameter.Name}_reader");
        var formDict = Expression.Variable(typeof(IReadOnlyDictionary<FormKey, StringValues>), "form_dict");
        var formBuffer = Expression.Variable(typeof(char[]), "form_buffer");
        var formDataMappingException = Expression.Variable(typeof(FormDataMappingException), "form_exception");

        // ProcessForm(context.Request.Form, form_dict, form_buffer);
        var processFormExpr = Expression.Call(ProcessFormMethod, FormExpr, Expression.Constant(formDataMapperOptions.MaxKeyBufferSize), formDict, formBuffer);
        // name_reader = new FormDataReader(form_dict, CultureInfo.InvariantCulture, form_buffer.AsMemory(0, formDataMapperOptions.MaxKeyBufferSize), httpContext.Request.Form.Files);
        var initializeReaderExpr = Expression.Assign(
            formReader,
            Expression.New(FormDataReaderConstructor,
                formDict,
                Expression.Constant(CultureInfo.InvariantCulture),
                Expression.Call(AsMemoryMethod, formBuffer, Expression.Constant(0), Expression.Constant(formDataMapperOptions.MaxKeyBufferSize)),
                FormFilesExpr));
        // name_reader.MaxRecursionDepth = formDataMapperOptions.MaxRecursionDepth;
        var setMaxRecursionDepthExpr = Expression.Assign(
            Expression.Property(formReader, nameof(FormDataReader.MaxRecursionDepth)),
            Expression.Constant(formDataMapperOptions.MaxRecursionDepth));
        // FormDataMapper.Map<string>(name_reader, FormDataMapperOptions);
        var invokeMapMethodExpr = Expression.Call(
            FormDataMapperMapMethod.MakeGenericMethod(parameter.ParameterType),
            formReader,
            Expression.Constant(formDataMapperOptions));
        // if (form_buffer != null)
        // {
        //   ArrayPool<char>.Shared.Return(form_buffer, false);
        // }
        var returnBufferExpr = Expression.Call(
            Expression.Property(null, typeof(ArrayPool<char>).GetProperty(nameof(ArrayPool<char>.Shared))!),
            ArrayPoolSharedReturnMethod,
            formBuffer,
            Expression.Constant(false));
        var conditionalReturnBufferExpr = Expression.IfThen(
            Expression.NotEqual(formBuffer, Expression.Constant(null)),
            returnBufferExpr);

        var parameterTypeNameConstant = Expression.Constant(TypeNameHelper.GetTypeDisplayName(parameter.ParameterType, fullName: false));
        var parameterNameConstant = Expression.Constant(parameter.Name);

        // try
        // {
        //   ProcessForm(context.Request.Form, form_dict, form_buffer);
        //   name_reader = new FormDataReader(form_dict, CultureInfo.InvariantCulture, form_buffer.AsMemory(0, FormDataMapperOptions.MaxKeyBufferSize));
        //   name_reader.MaxRecursionDepth = formDataMapperOptions.MaxRecursionDepth;
        //   name_local = FormDataMapper.Map<string>(name_reader, FormDataMapperOptions);
        // }
        // catch (FormDataMappingException e)
        // {
        //    wasParamCheckFailure = true;
        //    LogFormMappingFailedMethod(httpContext, "string", "name", ex, factoryContext.ThrowOnBadRequest);
        // }
        // finally
        // {
        //   if (form_buffer != null)
        //   {
        //     ArrayPool<char>.Shared.Return(form_buffer, false);
        //   }
        // }
        var bindAndCheckForm = Expression.Block(
            new[] { formReader, formDict, formBuffer, formDataMappingException },
            Expression.TryCatchFinally(
                Expression.Block(
                    typeof(void),
                    processFormExpr,
                    initializeReaderExpr,
                    setMaxRecursionDepthExpr,
                    Expression.Assign(formArgument, invokeMapMethodExpr)),
                conditionalReturnBufferExpr,
                Expression.Catch(formDataMappingException, Expression.Block(
                    typeof(void),
                    Expression.Assign(WasParamCheckFailureExpr, Expression.Constant(true)),
                    Expression.Call(
                        LogFormMappingFailedMethod,
                        HttpContextExpr,
                        parameterTypeNameConstant,
                        parameterNameConstant,
                        formDataMappingException,
                        Expression.Constant(factoryContext.ThrowOnBadRequest))
                )))
        );

        factoryContext.ParamCheckExpressions.Add(bindAndCheckForm);
        factoryContext.ExtraLocals.Add(formArgument);

        return formArgument;
    }

    private static void ProcessForm(IFormCollection form, int maxKeyBufferSize, ref IReadOnlyDictionary<FormKey, StringValues> formDictionary, ref char[] buffer)
    {
        var dictionary = new Dictionary<FormKey, StringValues>();
        foreach (var (key, value) in form)
        {
            dictionary.Add(new FormKey(key.AsMemory()), value);
        }
        formDictionary = dictionary.AsReadOnly();
        buffer = ArrayPool<char>.Shared.Rent(maxKeyBufferSize);
    }

    private static Expression BindParameterFromFormFiles(
        ParameterInfo parameter,
        RequestDelegateFactoryContext factoryContext)
    {
        factoryContext.FirstFormRequestBodyParameter ??= parameter;
        factoryContext.TrackedParameters.Add(parameter.Name!, RequestDelegateFactoryConstants.FormFileParameter);
        factoryContext.ReadForm = true;
        factoryContext.ReadFormFile = true;

        return BindParameterFromExpression(
            parameter,
            FormFilesExpr,
            factoryContext,
            "body");
    }

    private static Expression BindParameterFromFormFile(
        ParameterInfo parameter,
        string key,
        RequestDelegateFactoryContext factoryContext,
        string trackedParameterSource)
    {
        var valueExpression = GetValueFromProperty(FormFilesExpr, FormFilesIndexerProperty, key, typeof(IFormFile));

        factoryContext.FirstFormRequestBodyParameter ??= parameter;
        factoryContext.TrackedParameters.Add(key, trackedParameterSource);
        factoryContext.ReadForm = true;
        factoryContext.ReadFormFile = true;

        return BindParameterFromExpression(
            parameter,
            valueExpression,
            factoryContext,
            "form file");
    }

    private static Expression BindParameterFromBody(ParameterInfo parameter, bool allowEmpty, RequestDelegateFactoryContext factoryContext)
    {
        if (factoryContext.JsonRequestBodyParameter is not null)
        {
            factoryContext.HasMultipleBodyParameters = true;
            var parameterName = parameter.Name;

            Debug.Assert(parameterName is not null, "CreateArgument() should throw if parameter.Name is null.");

            if (factoryContext.TrackedParameters.ContainsKey(parameterName))
            {
                factoryContext.TrackedParameters.Remove(parameterName);
                factoryContext.TrackedParameters.Add(parameterName, "UNKNOWN");
            }
        }

        var isOptional = IsOptionalParameter(parameter, factoryContext);

        factoryContext.JsonRequestBodyParameter = parameter;
        factoryContext.AllowEmptyRequestBody = allowEmpty || isOptional;
        AddInferredAcceptsMetadata(factoryContext, parameter.ParameterType, DefaultAcceptsAndProducesContentType);

        if (!factoryContext.AllowEmptyRequestBody)
        {
            if (factoryContext.HasInferredBody)
            {
                // if (bodyValue == null)
                // {
                //    wasParamCheckFailure = true;
                //    Log.ImplicitBodyNotProvided(httpContext, "todo", ThrowOnBadRequest);
                // }
                factoryContext.ParamCheckExpressions.Add(Expression.Block(
                    Expression.IfThen(
                        Expression.Equal(BodyValueExpr, Expression.Constant(null)),
                        Expression.Block(
                            Expression.Assign(WasParamCheckFailureExpr, Expression.Constant(true)),
                            Expression.Call(LogImplicitBodyNotProvidedMethod,
                                HttpContextExpr,
                                Expression.Constant(parameter.Name),
                                Expression.Constant(factoryContext.ThrowOnBadRequest)
                            )
                        )
                    )
                ));
            }
            else
            {
                // If the parameter is required or the user has not explicitly
                // set allowBody to be empty then validate that it is required.
                //
                // if (bodyValue == null)
                // {
                //      wasParamCheckFailure = true;
                //      Log.RequiredParameterNotProvided(httpContext, "Todo", "todo", "body", ThrowOnBadRequest);
                // }
                var checkRequiredBodyBlock = Expression.Block(
                    Expression.IfThen(
                    Expression.Equal(BodyValueExpr, Expression.Constant(null)),
                        Expression.Block(
                            Expression.Assign(WasParamCheckFailureExpr, Expression.Constant(true)),
                            Expression.Call(LogRequiredParameterNotProvidedMethod,
                                HttpContextExpr,
                                Expression.Constant(TypeNameHelper.GetTypeDisplayName(parameter.ParameterType, fullName: false)),
                                Expression.Constant(parameter.Name),
                                Expression.Constant("body"),
                                Expression.Constant(factoryContext.ThrowOnBadRequest))
                        )
                    )
                );
                factoryContext.ParamCheckExpressions.Add(checkRequiredBodyBlock);
            }
        }
        else if (parameter.HasDefaultValue)
        {
            // Convert(bodyValue ?? SomeDefault, Todo)
            return Expression.Convert(
                Expression.Coalesce(BodyValueExpr, Expression.Constant(parameter.DefaultValue)),
                parameter.ParameterType);
        }

        // Convert(bodyValue, Todo)
        return Expression.Convert(BodyValueExpr, parameter.ParameterType);
    }

    private static bool IsOptionalParameter(ParameterInfo parameter, RequestDelegateFactoryContext factoryContext)
    {
        if (parameter is PropertyAsParameterInfo argument)
        {
            return argument.IsOptional;
        }

        // - Parameters representing value or reference types with a default value
        // under any nullability context are treated as optional.
        // - Value type parameters without a default value in an oblivious
        // nullability context are required.
        // - Reference type parameters without a default value in an oblivious
        // nullability context are optional.
        var nullabilityInfo = factoryContext.NullabilityContext.Create(parameter);
        return parameter.HasDefaultValue
            || nullabilityInfo.ReadState != NullabilityState.NotNull;
    }

    private static MethodInfo GetMethodInfo<T>(Expression<T> expr)
    {
        var mc = (MethodCallExpression)expr.Body;
        return mc.Method;
    }

    private static MemberInfo GetMemberInfo<T>(Expression<T> expr)
    {
        var mc = (MemberExpression)expr.Body;
        return mc.Member;
    }

    // The result of the method is null so we fallback to some runtime logic.
    // First we check if the result is IResult, Task<IResult> or ValueTask<IResult>. If
    // it is, we await if necessary then execute the result.
    // Then we check to see if it's Task<object> or ValueTask<object>. If it is, we await
    // if necessary and restart the cycle until we've reached a terminal state (unknown type).
    // We currently don't handle Task<unknown> or ValueTask<unknown>. We can support this later if this
    // ends up being a common scenario.
    private static Task ExecuteValueTaskOfObject(ValueTask<object> valueTask, HttpContext httpContext, JsonTypeInfo<object> jsonTypeInfo)
    {
        static async Task ExecuteAwaited(ValueTask<object> valueTask, HttpContext httpContext, JsonTypeInfo<object> jsonTypeInfo)
        {
            await ExecuteAwaitedReturn(await valueTask, httpContext, jsonTypeInfo);
        }

        if (valueTask.IsCompletedSuccessfully)
        {
            return ExecuteAwaitedReturn(valueTask.GetAwaiter().GetResult(), httpContext, jsonTypeInfo);
        }

        return ExecuteAwaited(valueTask, httpContext, jsonTypeInfo);
    }

    private static Task ExecuteTaskOfObject(Task<object> task, HttpContext httpContext, JsonTypeInfo<object> jsonTypeInfo)
    {
        static async Task ExecuteAwaited(Task<object> task, HttpContext httpContext, JsonTypeInfo<object> jsonTypeInfo)
        {
            await ExecuteAwaitedReturn(await task, httpContext, jsonTypeInfo);
        }

        if (task.IsCompletedSuccessfully)
        {
            return ExecuteAwaitedReturn(task.GetAwaiter().GetResult(), httpContext, jsonTypeInfo);
        }

        return ExecuteAwaited(task, httpContext, jsonTypeInfo);
    }

    private static Task ExecuteAwaitedReturn(object obj, HttpContext httpContext, JsonTypeInfo<object> jsonTypeInfo)
    {
        return ExecuteHandlerHelper.ExecuteReturnAsync(obj, httpContext, jsonTypeInfo);
    }

    private static Task ExecuteTaskOfTFast<T>(Task<T> task, HttpContext httpContext, JsonTypeInfo<T> jsonTypeInfo)
    {
        EnsureRequestTaskNotNull(task);

        static async Task ExecuteAwaited(Task<T> task, HttpContext httpContext, JsonTypeInfo<T> jsonTypeInfo)
        {
            await WriteJsonResponseFast(httpContext.Response, await task, jsonTypeInfo);
        }

        if (task.IsCompletedSuccessfully)
        {
            return WriteJsonResponseFast(httpContext.Response, task.GetAwaiter().GetResult(), jsonTypeInfo);
        }

        return ExecuteAwaited(task, httpContext, jsonTypeInfo);
    }

    private static Task ExecuteTaskOfT<T>(Task<T> task, HttpContext httpContext, JsonTypeInfo<T> jsonTypeInfo)
    {
        EnsureRequestTaskNotNull(task);

        static async Task ExecuteAwaited(Task<T> task, HttpContext httpContext, JsonTypeInfo<T> jsonTypeInfo)
        {
            await WriteJsonResponse(httpContext.Response, await task, jsonTypeInfo);
        }

        if (task.IsCompletedSuccessfully)
        {
            return WriteJsonResponse(httpContext.Response, task.GetAwaiter().GetResult(), jsonTypeInfo);
        }

        return ExecuteAwaited(task, httpContext, jsonTypeInfo);
    }

    private static Task ExecuteTaskOfString(Task<string?> task, HttpContext httpContext)
    {
        ExecuteHandlerHelper.SetPlaintextContentType(httpContext);
        EnsureRequestTaskNotNull(task);

        static async Task ExecuteAwaited(Task<string> task, HttpContext httpContext)
        {
            await httpContext.Response.WriteAsync(await task);
        }

        if (task.IsCompletedSuccessfully)
        {
            return httpContext.Response.WriteAsync(task.GetAwaiter().GetResult()!);
        }

        return ExecuteAwaited(task!, httpContext);
    }

    private static Task ExecuteWriteStringResponseAsync(HttpContext httpContext, string text)
    {
        ExecuteHandlerHelper.SetPlaintextContentType(httpContext);
        return httpContext.Response.WriteAsync(text);
    }

    private static Task ExecuteValueTask(ValueTask task)
    {
        static async Task ExecuteAwaited(ValueTask task)
        {
            await task;
        }

        if (task.IsCompletedSuccessfully)
        {
            task.GetAwaiter().GetResult();
            return Task.CompletedTask;
        }

        return ExecuteAwaited(task);
    }

    private static ValueTask<object?> ExecuteTaskWithEmptyResult(Task task)
    {
        static async ValueTask<object?> ExecuteAwaited(Task task)
        {
            await task;
            return EmptyHttpResult.Instance;
        }

        if (task.IsCompletedSuccessfully)
        {
            return new ValueTask<object?>(EmptyHttpResult.Instance);
        }

        return ExecuteAwaited(task);
    }

    private static ValueTask<object?> ExecuteValueTaskWithEmptyResult(ValueTask valueTask)
    {
        static async ValueTask<object?> ExecuteAwaited(ValueTask task)
        {
            await task;
            return EmptyHttpResult.Instance;
        }

        if (valueTask.IsCompletedSuccessfully)
        {
            valueTask.GetAwaiter().GetResult();
            return new ValueTask<object?>(EmptyHttpResult.Instance);
        }

        return ExecuteAwaited(valueTask);
    }

    private static Task ExecuteValueTaskOfTFast<T>(ValueTask<T> task, HttpContext httpContext, JsonTypeInfo<T> jsonTypeInfo)
    {
        static async Task ExecuteAwaited(ValueTask<T> task, HttpContext httpContext, JsonTypeInfo<T> jsonTypeInfo)
        {
            await WriteJsonResponseFast(httpContext.Response, await task, jsonTypeInfo);
        }

        if (task.IsCompletedSuccessfully)
        {
            return WriteJsonResponseFast(httpContext.Response, task.GetAwaiter().GetResult(), jsonTypeInfo);
        }

        return ExecuteAwaited(task, httpContext, jsonTypeInfo);
    }

    private static Task ExecuteValueTaskOfT<T>(ValueTask<T> task, HttpContext httpContext, JsonTypeInfo<T> jsonTypeInfo)
    {
        static async Task ExecuteAwaited(ValueTask<T> task, HttpContext httpContext, JsonTypeInfo<T> jsonTypeInfo)
        {
            await WriteJsonResponse(httpContext.Response, await task, jsonTypeInfo);
        }

        if (task.IsCompletedSuccessfully)
        {
            return WriteJsonResponse(httpContext.Response, task.GetAwaiter().GetResult(), jsonTypeInfo);
        }

        return ExecuteAwaited(task, httpContext, jsonTypeInfo);
    }

    private static Task ExecuteValueTaskOfString(ValueTask<string?> task, HttpContext httpContext)
    {
        ExecuteHandlerHelper.SetPlaintextContentType(httpContext);

        static async Task ExecuteAwaited(ValueTask<string> task, HttpContext httpContext)
        {
            await httpContext.Response.WriteAsync(await task);
        }

        if (task.IsCompletedSuccessfully)
        {
            return httpContext.Response.WriteAsync(task.GetAwaiter().GetResult()!);
        }

        return ExecuteAwaited(task!, httpContext);
    }

    private static Task ExecuteValueTaskResult<T>(ValueTask<T?> task, HttpContext httpContext) where T : IResult
    {
        static async Task ExecuteAwaited(ValueTask<T> task, HttpContext httpContext)
        {
            await EnsureRequestResultNotNull(await task).ExecuteAsync(httpContext);
        }

        if (task.IsCompletedSuccessfully)
        {
            return EnsureRequestResultNotNull(task.GetAwaiter().GetResult()).ExecuteAsync(httpContext);
        }

        return ExecuteAwaited(task!, httpContext);
    }

    private static async Task ExecuteTaskResult<T>(Task<T?> task, HttpContext httpContext) where T : IResult
    {
        EnsureRequestTaskOfNotNull(task);

        await EnsureRequestResultNotNull(await task).ExecuteAsync(httpContext);
    }

    private static async Task ExecuteResultWriteResponse(IResult? result, HttpContext httpContext)
    {
        await EnsureRequestResultNotNull(result).ExecuteAsync(httpContext);
    }

    // This method will not check for polymorphism and will
    // leverage the STJ polymorphism support.
    private static Task WriteJsonResponseFast<T>(HttpResponse response, T value, JsonTypeInfo<T> jsonTypeInfo)
        => HttpResponseJsonExtensions.WriteAsJsonAsync(response, value, jsonTypeInfo, default);

    private static Task WriteJsonResponse<T>(HttpResponse response, T? value, JsonTypeInfo<T> jsonTypeInfo)
    {
        return ExecuteHandlerHelper.WriteJsonResponseAsync(response, value, jsonTypeInfo);
    }

    private static NotSupportedException GetUnsupportedReturnTypeException(Type returnType)
    {
        return new NotSupportedException($"Unsupported return type: {TypeNameHelper.GetTypeDisplayName(returnType)}");
    }

    private static class RequestDelegateFactoryConstants
    {
        public const string RouteAttribute = "Route (Attribute)";
        public const string QueryAttribute = "Query (Attribute)";
        public const string HeaderAttribute = "Header (Attribute)";
        public const string BodyAttribute = "Body (Attribute)";
        public const string ServiceAttribute = "Service (Attribute)";
        public const string FormFileAttribute = "Form File (Attribute)";
        public const string FormAttribute = "Form (Attribute)";
        public const string FormBindingAttribute = "Form Binding (Attribute)";
        public const string RouteParameter = "Route (Inferred)";
        public const string QueryStringParameter = "Query String (Inferred)";
        public const string ServiceParameter = "Services (Inferred)";
        public const string BodyParameter = "Body (Inferred)";
        public const string RouteOrQueryStringParameter = "Route or Query String (Inferred)";
        public const string FormFileParameter = "Form File (Inferred)";
        public const string FormCollectionParameter = "Form Collection (Inferred)";
        public const string PropertyAsParameter = "As Parameter (Attribute)";
    }

    private static partial class Log
    {
        // This doesn't take a shouldThrow parameter because an IOException indicates an aborted request rather than a "bad" request so
        // a BadHttpRequestException feels wrong. The client shouldn't be able to read the Developer Exception Page at any rate.
        public static void RequestBodyIOException(HttpContext httpContext, IOException exception)
            => RequestBodyIOException(GetLogger(httpContext), exception);

        [LoggerMessage(RequestDelegateCreationLogging.RequestBodyIOExceptionEventId, LogLevel.Debug, RequestDelegateCreationLogging.RequestBodyIOExceptionMessage, EventName = RequestDelegateCreationLogging.RequestBodyIOExceptionEventName)]
        private static partial void RequestBodyIOException(ILogger logger, IOException exception);

        public static void InvalidJsonRequestBody(HttpContext httpContext, string parameterTypeName, string parameterName, Exception exception, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, RequestDelegateCreationLogging.InvalidJsonRequestBodyExceptionMessage, parameterTypeName, parameterName);
                throw new BadHttpRequestException(message, exception);
            }

            InvalidJsonRequestBody(GetLogger(httpContext), parameterTypeName, parameterName, exception);
        }

        [LoggerMessage(RequestDelegateCreationLogging.InvalidJsonRequestBodyEventId, LogLevel.Debug, RequestDelegateCreationLogging.InvalidJsonRequestBodyLogMessage, EventName = RequestDelegateCreationLogging.InvalidJsonRequestBodyEventName)]
        private static partial void InvalidJsonRequestBody(ILogger logger, string parameterType, string parameterName, Exception exception);

        public static void ParameterBindingFailed(HttpContext httpContext, string parameterTypeName, string parameterName, string sourceValue, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, RequestDelegateCreationLogging.ParameterBindingFailedExceptionMessage, parameterTypeName, parameterName, sourceValue);
                throw new BadHttpRequestException(message);
            }

            ParameterBindingFailed(GetLogger(httpContext), parameterTypeName, parameterName, sourceValue);
        }

        [LoggerMessage(RequestDelegateCreationLogging.ParameterBindingFailedEventId, LogLevel.Debug, RequestDelegateCreationLogging.ParameterBindingFailedLogMessage, EventName = RequestDelegateCreationLogging.ParameterBindingFailedEventName)]
        private static partial void ParameterBindingFailed(ILogger logger, string parameterType, string parameterName, string sourceValue);

        public static void RequiredParameterNotProvided(HttpContext httpContext, string parameterTypeName, string parameterName, string source, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, RequestDelegateCreationLogging.RequiredParameterNotProvidedExceptionMessage, parameterTypeName, parameterName, source);
                throw new BadHttpRequestException(message);
            }

            RequiredParameterNotProvided(GetLogger(httpContext), parameterTypeName, parameterName, source);
        }

        [LoggerMessage(RequestDelegateCreationLogging.RequiredParameterNotProvidedEventId, LogLevel.Debug, RequestDelegateCreationLogging.RequiredParameterNotProvidedLogMessage, EventName = RequestDelegateCreationLogging.RequiredParameterNotProvidedEventName)]
        private static partial void RequiredParameterNotProvided(ILogger logger, string parameterType, string parameterName, string source);

        public static void ImplicitBodyNotProvided(HttpContext httpContext, string parameterName, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, RequestDelegateCreationLogging.ImplicitBodyNotProvidedExceptionMessage, parameterName);
                throw new BadHttpRequestException(message);
            }

            ImplicitBodyNotProvided(GetLogger(httpContext), parameterName);
        }

        [LoggerMessage(RequestDelegateCreationLogging.ImplicitBodyNotProvidedEventId, LogLevel.Debug, RequestDelegateCreationLogging.ImplicitBodyNotProvidedLogMessage, EventName = RequestDelegateCreationLogging.ImplicitBodyNotProvidedEventName)]
        private static partial void ImplicitBodyNotProvided(ILogger logger, string parameterName);

        public static void UnexpectedJsonContentType(HttpContext httpContext, string? contentType, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, RequestDelegateCreationLogging.UnexpectedJsonContentTypeExceptionMessage, contentType);
                throw new BadHttpRequestException(message, StatusCodes.Status415UnsupportedMediaType);
            }

            UnexpectedJsonContentType(GetLogger(httpContext), contentType ?? "(none)");
        }

        [LoggerMessage(RequestDelegateCreationLogging.UnexpectedJsonContentTypeEventId, LogLevel.Debug, RequestDelegateCreationLogging.UnexpectedJsonContentTypeLogMessage, EventName = RequestDelegateCreationLogging.UnexpectedJsonContentTypeEventName)]
        private static partial void UnexpectedJsonContentType(ILogger logger, string contentType);

        public static void UnexpectedNonFormContentType(HttpContext httpContext, string? contentType, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, RequestDelegateCreationLogging.UnexpectedFormContentTypeExceptionMessage, contentType);
                throw new BadHttpRequestException(message, StatusCodes.Status415UnsupportedMediaType);
            }

            UnexpectedNonFormContentType(GetLogger(httpContext), contentType ?? "(none)");
        }

        [LoggerMessage(RequestDelegateCreationLogging.UnexpectedFormContentTypeEventId, LogLevel.Debug, RequestDelegateCreationLogging.UnexpectedFormContentTypeLogMessage, EventName = RequestDelegateCreationLogging.UnexpectedFormContentTypeLogEventName)]
        private static partial void UnexpectedNonFormContentType(ILogger logger, string contentType);

        public static void InvalidFormRequestBody(HttpContext httpContext, string parameterTypeName, string parameterName, Exception exception, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, RequestDelegateCreationLogging.InvalidFormRequestBodyExceptionMessage, parameterTypeName, parameterName);
                throw new BadHttpRequestException(message, exception);
            }

            InvalidFormRequestBody(GetLogger(httpContext), parameterTypeName, parameterName, exception);
        }

        [LoggerMessage(RequestDelegateCreationLogging.InvalidFormRequestBodyEventId, LogLevel.Debug, RequestDelegateCreationLogging.InvalidFormRequestBodyLogMessage, EventName = RequestDelegateCreationLogging.InvalidFormRequestBodyEventName)]
        private static partial void InvalidFormRequestBody(ILogger logger, string parameterType, string parameterName, Exception exception);

        public static void InvalidAntiforgeryToken(HttpContext httpContext, string parameterTypeName, string parameterName, Exception exception, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, RequestDelegateCreationLogging.InvalidAntiforgeryTokenExceptionMessage, parameterTypeName, parameterName);
                throw new BadHttpRequestException(message, exception);
            }

            InvalidAntiforgeryToken(GetLogger(httpContext), parameterTypeName, parameterName, exception);
        }

        [LoggerMessage(RequestDelegateCreationLogging.InvalidAntiforgeryTokenEventId, LogLevel.Debug, RequestDelegateCreationLogging.InvalidAntiforgeryTokenLogMessage, EventName = RequestDelegateCreationLogging.InvalidAntiforgeryTokenEventName)]
        private static partial void InvalidAntiforgeryToken(ILogger logger, string parameterType, string parameterName, Exception exception);

        public static void FormDataMappingFailed(HttpContext httpContext, string parameterTypeName, string parameterName, FormDataMappingException exception, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, exception.Error.Message.Format, exception.Error.Message.GetArguments());
                throw new BadHttpRequestException(message, exception);
            }

            FormDataMappingFailed(GetLogger(httpContext), parameterTypeName, parameterName, exception);
        }

        [LoggerMessage(RequestDelegateCreationLogging.FormDataMappingFailedEventId, LogLevel.Debug, RequestDelegateCreationLogging.FormDataMappingFailedLogMessage, EventName = RequestDelegateCreationLogging.FormDataMappingFailedEventName)]
        private static partial void FormDataMappingFailed(ILogger logger, string parameterType, string parameterName, Exception exception);

        public static void UnexpectedRequestWithoutBody(HttpContext httpContext, string parameterTypeName, string parameterName, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, RequestDelegateCreationLogging.UnexpectedRequestWithoutBodyExceptionMessage, parameterTypeName, parameterName);
                throw new BadHttpRequestException(message);
            }

            UnexpectedRequestWithoutBody(GetLogger(httpContext), parameterTypeName, parameterName);
        }

        [LoggerMessage(RequestDelegateCreationLogging.UnexpectedRequestWithoutBodyEventId, LogLevel.Debug, RequestDelegateCreationLogging.UnexpectedRequestWithoutBodyLogMessage, EventName = RequestDelegateCreationLogging.UnexpectedRequestWithoutBodyEventName)]
        private static partial void UnexpectedRequestWithoutBody(ILogger logger, string parameterType, string parameterName);

        private static ILogger GetLogger(HttpContext httpContext)
        {
            var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            return loggerFactory.CreateLogger(typeof(RequestDelegateFactory));
        }
    }

    private static void EnsureRequestTaskOfNotNull<T>(Task<T?> task) where T : IResult
    {
        if (task is null)
        {
            throw new InvalidOperationException("The IResult in Task<IResult> response must not be null.");
        }
    }

    private static void EnsureRequestTaskNotNull(Task? task)
    {
        if (task is null)
        {
            throw new InvalidOperationException("The Task returned by the Delegate must not be null.");
        }
    }

    private static IResult EnsureRequestResultNotNull(IResult? result)
    {
        if (result is null)
        {
            throw new InvalidOperationException("The IResult returned by the Delegate must not be null.");
        }

        return result;
    }

    private static string BuildErrorMessageForMultipleBodyParameters(RequestDelegateFactoryContext factoryContext)
    {
        var errorMessage = new StringBuilder();
        errorMessage.AppendLine("Failure to infer one or more parameters.");
        errorMessage.AppendLine("Below is the list of parameters that we found: ");
        errorMessage.AppendLine();
        errorMessage.AppendLine(FormattableString.Invariant($"{"Parameter",-20}| {"Source",-30}"));
        errorMessage.AppendLine("---------------------------------------------------------------------------------");

        FormatTrackedParameters(factoryContext, errorMessage);

        errorMessage.AppendLine().AppendLine();
        errorMessage.AppendLine("Did you mean to register the \"UNKNOWN\" parameters as a Service?")
            .AppendLine();
        return errorMessage.ToString();
    }

    private static string BuildErrorMessageForInferredBodyParameter(RequestDelegateFactoryContext factoryContext)
    {
        var errorMessage = new StringBuilder();
        errorMessage.AppendLine("Body was inferred but the method does not allow inferred body parameters.");
        errorMessage.AppendLine("Below is the list of parameters that we found: ");
        errorMessage.AppendLine();
        errorMessage.AppendLine(FormattableString.Invariant($"{"Parameter",-20}| {"Source",-30}"));
        errorMessage.AppendLine("---------------------------------------------------------------------------------");

        FormatTrackedParameters(factoryContext, errorMessage);

        errorMessage.AppendLine().AppendLine();
        errorMessage.AppendLine("Did you mean to register the \"Body (Inferred)\" parameter(s) as a Service or apply the [FromServices] or [FromBody] attribute?")
            .AppendLine();
        return errorMessage.ToString();
    }

    private static string BuildErrorMessageForFormAndJsonBodyParameters(RequestDelegateFactoryContext factoryContext)
    {
        var errorMessage = new StringBuilder();
        errorMessage.AppendLine("An action cannot use both form and JSON body parameters.");
        errorMessage.AppendLine("Below is the list of parameters that we found: ");
        errorMessage.AppendLine();
        errorMessage.AppendLine(FormattableString.Invariant($"{"Parameter",-20}| {"Source",-30}"));
        errorMessage.AppendLine("---------------------------------------------------------------------------------");

        FormatTrackedParameters(factoryContext, errorMessage);

        return errorMessage.ToString();
    }

    private static void FormatTrackedParameters(RequestDelegateFactoryContext factoryContext, StringBuilder errorMessage)
    {
        foreach (var kv in factoryContext.TrackedParameters)
        {
            errorMessage.AppendLine(FormattableString.Invariant($"{kv.Key,-19} | {kv.Value,-15}"));
        }
    }

    private sealed class RdfEndpointBuilder : EndpointBuilder
    {
        public RdfEndpointBuilder(IServiceProvider applicationServices)
        {
            ApplicationServices = applicationServices;
        }

        public override Endpoint Build()
        {
            throw new NotSupportedException();
        }
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
        public object? GetService(Type serviceType) => null;
    }
}
