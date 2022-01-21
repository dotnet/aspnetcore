// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.IO.Pipelines;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Creates <see cref="RequestDelegate"/> implementations from <see cref="Delegate"/> request handlers.
/// </summary>
public static partial class RequestDelegateFactory
{
    private static readonly ParameterBindingMethodCache ParameterBindingMethodCache = new();

    private static readonly MethodInfo ExecuteTaskOfTMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTask), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteTaskOfStringMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTaskOfString), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteValueTaskOfTMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskOfT), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteValueTaskMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTask), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteValueTaskOfStringMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskOfString), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteTaskResultOfTMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTaskResult), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteValueResultTaskOfTMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskResult), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteObjectReturnMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteObjectReturn), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo GetRequiredServiceMethod = typeof(ServiceProviderServiceExtensions).GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(IServiceProvider) })!;
    private static readonly MethodInfo GetServiceMethod = typeof(ServiceProviderServiceExtensions).GetMethod(nameof(ServiceProviderServiceExtensions.GetService), BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(IServiceProvider) })!;
    private static readonly MethodInfo ResultWriteResponseAsyncMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteResultWriteResponse), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo StringResultWriteResponseAsyncMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteWriteStringResponseAsync), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo JsonResultWriteResponseAsyncMethod = GetMethodInfo<Func<HttpResponse, object, Task>>((response, value) => HttpResponseJsonExtensions.WriteAsJsonAsync(response, value, default));

    private static readonly MethodInfo LogParameterBindingFailedMethod = GetMethodInfo<Action<HttpContext, string, string, string, bool>>((httpContext, parameterType, parameterName, sourceValue, shouldThrow) =>
        Log.ParameterBindingFailed(httpContext, parameterType, parameterName, sourceValue, shouldThrow));
    private static readonly MethodInfo LogRequiredParameterNotProvidedMethod = GetMethodInfo<Action<HttpContext, string, string, string, bool>>((httpContext, parameterType, parameterName, source, shouldThrow) =>
        Log.RequiredParameterNotProvided(httpContext, parameterType, parameterName, source, shouldThrow));
    private static readonly MethodInfo LogImplicitBodyNotProvidedMethod = GetMethodInfo<Action<HttpContext, string, bool>>((httpContext, parameterName, shouldThrow) =>
        Log.ImplicitBodyNotProvided(httpContext, parameterName, shouldThrow));

    private static readonly ParameterExpression TargetExpr = Expression.Parameter(typeof(object), "target");
    private static readonly ParameterExpression BodyValueExpr = Expression.Parameter(typeof(object), "bodyValue");
    private static readonly ParameterExpression WasParamCheckFailureExpr = Expression.Variable(typeof(bool), "wasParamCheckFailure");
    private static readonly ParameterExpression BoundValuesArrayExpr = Expression.Parameter(typeof(object[]), "boundValues");

    private static readonly ParameterExpression HttpContextExpr = ParameterBindingMethodCache.HttpContextExpr;
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

    private static readonly ParameterExpression TempSourceStringExpr = ParameterBindingMethodCache.TempSourceStringExpr;
    private static readonly BinaryExpression TempSourceStringNotNullExpr = Expression.NotEqual(TempSourceStringExpr, Expression.Constant(null));
    private static readonly BinaryExpression TempSourceStringNullExpr = Expression.Equal(TempSourceStringExpr, Expression.Constant(null));
    private static readonly string[] DefaultAcceptsContentType = new[] { "application/json" };
    private static readonly string[] FormFileContentType = new[] { "multipart/form-data" };

    /// <summary>
    /// Creates a <see cref="RequestDelegate"/> implementation for <paramref name="handler"/>.
    /// </summary>
    /// <param name="handler">A request handler with any number of custom parameters that often produces a response with its return value.</param>
    /// <param name="options">The <see cref="RequestDelegateFactoryOptions"/> used to configure the behavior of the handler.</param>
    /// <returns>The <see cref="RequestDelegateResult"/>.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static RequestDelegateResult Create(Delegate handler, RequestDelegateFactoryOptions? options = null)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var targetExpression = handler.Target switch
        {
            object => Expression.Convert(TargetExpr, handler.Target.GetType()),
            null => null,
        };

        var factoryContext = CreateFactoryContext(options);
        var targetableRequestDelegate = CreateTargetableRequestDelegate(handler.Method, targetExpression, factoryContext);

        return new RequestDelegateResult(httpContext => targetableRequestDelegate(handler.Target, httpContext), factoryContext.Metadata);
    }

    /// <summary>
    /// Creates a <see cref="RequestDelegate"/> implementation for <paramref name="methodInfo"/>.
    /// </summary>
    /// <param name="methodInfo">A request handler with any number of custom parameters that often produces a response with its return value.</param>
    /// <param name="targetFactory">Creates the <see langword="this"/> for the non-static method.</param>
    /// <param name="options">The <see cref="RequestDelegateFactoryOptions"/> used to configure the behavior of the handler.</param>
    /// <returns>The <see cref="RequestDelegate"/>.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static RequestDelegateResult Create(MethodInfo methodInfo, Func<HttpContext, object>? targetFactory = null, RequestDelegateFactoryOptions? options = null)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
    {
        if (methodInfo is null)
        {
            throw new ArgumentNullException(nameof(methodInfo));
        }

        if (methodInfo.DeclaringType is null)
        {
            throw new ArgumentException($"{nameof(methodInfo)} does not have a declaring type.");
        }

        var factoryContext = CreateFactoryContext(options);

        if (targetFactory is null)
        {
            if (methodInfo.IsStatic)
            {
                var untargetableRequestDelegate = CreateTargetableRequestDelegate(methodInfo, targetExpression: null, factoryContext);

                return new RequestDelegateResult(httpContext => untargetableRequestDelegate(null, httpContext), factoryContext.Metadata);
            }

            targetFactory = context => Activator.CreateInstance(methodInfo.DeclaringType)!;
        }

        var targetExpression = Expression.Convert(TargetExpr, methodInfo.DeclaringType);
        var targetableRequestDelegate = CreateTargetableRequestDelegate(methodInfo, targetExpression, factoryContext);

        return new RequestDelegateResult(httpContext => targetableRequestDelegate(targetFactory(httpContext), httpContext), factoryContext.Metadata);
    }

    private static FactoryContext CreateFactoryContext(RequestDelegateFactoryOptions? options) =>
        new()
        {
            ServiceProviderIsService = options?.ServiceProvider?.GetService<IServiceProviderIsService>(),
            RouteParameters = options?.RouteParameterNames?.ToList(),
            ThrowOnBadRequest = options?.ThrowOnBadRequest ?? false,
            DisableInferredFromBody = options?.DisableInferBodyFromParameters ?? false,
        };

    private static Func<object?, HttpContext, Task> CreateTargetableRequestDelegate(MethodInfo methodInfo, Expression? targetExpression, FactoryContext factoryContext)
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

        var arguments = CreateArguments(methodInfo.GetParameters(), factoryContext);

        var responseWritingMethodCall = factoryContext.ParamCheckExpressions.Count > 0 ?
            CreateParamCheckingResponseWritingMethodCall(methodInfo, targetExpression, arguments, factoryContext) :
            CreateResponseWritingMethodCall(methodInfo, targetExpression, arguments);

        if (factoryContext.UsingTempSourceString)
        {
            responseWritingMethodCall = Expression.Block(new[] { TempSourceStringExpr }, responseWritingMethodCall);
        }

        return HandleRequestBodyAndCompileRequestDelegate(responseWritingMethodCall, factoryContext);
    }

    private static Expression[] CreateArguments(ParameterInfo[]? parameters, FactoryContext factoryContext)
    {
        if (parameters is null || parameters.Length == 0)
        {
            return Array.Empty<Expression>();
        }

        var args = new Expression[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            args[i] = CreateArgument(parameters[i], factoryContext);
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

    private static Expression CreateArgument(ParameterInfo parameter, FactoryContext factoryContext)
    {
        if (parameter.Name is null)
        {
            throw new InvalidOperationException($"Encountered a parameter of type '{parameter.ParameterType}' without a name. Parameters must have a name.");
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

            return BindParameterFromProperty(parameter, RouteValuesExpr, routeName, factoryContext, "route");
        }
        else if (parameterCustomAttributes.OfType<IFromQueryMetadata>().FirstOrDefault() is { } queryAttribute)
        {
            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.QueryAttribute);
            return BindParameterFromProperty(parameter, QueryExpr, queryAttribute.Name ?? parameter.Name, factoryContext, "query string");
        }
        else if (parameterCustomAttributes.OfType<IFromHeaderMetadata>().FirstOrDefault() is { } headerAttribute)
        {
            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.HeaderAttribute);
            return BindParameterFromProperty(parameter, HeadersExpr, headerAttribute.Name ?? parameter.Name, factoryContext, "header");
        }
        else if (parameterCustomAttributes.OfType<IFromBodyMetadata>().FirstOrDefault() is { } bodyAttribute)
        {
            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.BodyAttribute);
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
            else if (parameter.ParameterType != typeof(IFormFile))
            {
                throw new NotSupportedException(
                    $"{nameof(IFromFormMetadata)} is only supported for parameters of type {nameof(IFormFileCollection)} and {nameof(IFormFile)}.");
            }

            return BindParameterFromFormFile(parameter, formAttribute.Name ?? parameter.Name, factoryContext, RequestDelegateFactoryConstants.FormFileAttribute);
        }
        else if (parameter.CustomAttributes.Any(a => typeof(IFromServiceMetadata).IsAssignableFrom(a.AttributeType)))
        {
            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.ServiceAttribute);
            return BindParameterFromService(parameter, factoryContext);
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
        else if (ParameterBindingMethodCache.HasBindAsyncMethod(parameter))
        {
            return BindParameterFromBindAsync(parameter, factoryContext);
        }
        else if (parameter.ParameterType == typeof(string) || ParameterBindingMethodCache.HasTryParseMethod(parameter))
        {
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
                    return BindParameterFromProperty(parameter, RouteValuesExpr, parameter.Name, factoryContext, "route");
                }
                else
                {
                    factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.QueryStringParameter);
                    return BindParameterFromProperty(parameter, QueryExpr, parameter.Name, factoryContext, "query string");
                }
            }

            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.RouteOrQueryStringParameter);
            return BindParameterFromRouteValueOrQueryString(parameter, parameter.Name, factoryContext);
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

    private static Expression CreateResponseWritingMethodCall(MethodInfo methodInfo, Expression? target, Expression[] arguments)
    {
        var callMethod = CreateMethodCall(methodInfo, target, arguments);
        return AddResponseWritingToMethodCall(callMethod, methodInfo.ReturnType);
    }

    // If we're calling TryParse or validating parameter optionality and
    // wasParamCheckFailure indicates it failed, set a 400 StatusCode instead of calling the method.
    private static Expression CreateParamCheckingResponseWritingMethodCall(
        MethodInfo methodInfo, Expression? target, Expression[] arguments, FactoryContext factoryContext)
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

        var set400StatusAndReturnCompletedTask = Expression.Block(
                Expression.Assign(StatusCodeExpr, Expression.Constant(400)),
                CompletedTaskExpr);

        var methodCall = CreateMethodCall(methodInfo, target, arguments);

        var checkWasParamCheckFailure = Expression.Condition(WasParamCheckFailureExpr,
            set400StatusAndReturnCompletedTask,
            AddResponseWritingToMethodCall(methodCall, methodInfo.ReturnType));

        checkParamAndCallMethod[factoryContext.ParamCheckExpressions.Count] = checkWasParamCheckFailure;

        return Expression.Block(localVariables, checkParamAndCallMethod);
    }

    private static Expression AddResponseWritingToMethodCall(Expression methodCall, Type returnType)
    {
        // Exact request delegate match
        if (returnType == typeof(void))
        {
            return Expression.Block(methodCall, CompletedTaskExpr);
        }
        else if (returnType == typeof(object))
        {
            return Expression.Call(ExecuteObjectReturnMethod, methodCall, HttpContextExpr);
        }
        else if (returnType == typeof(ValueTask<object>))
        {
            // REVIEW: We can avoid this box if it becomes a performance issue
            var box = Expression.TypeAs(methodCall, typeof(object));
            return Expression.Call(ExecuteObjectReturnMethod, box, HttpContextExpr);
        }
        else if (returnType == typeof(Task<object>))
        {
            var convert = Expression.Convert(methodCall, typeof(object));
            return Expression.Call(ExecuteObjectReturnMethod, convert, HttpContextExpr);
        }
        else if (AwaitableInfo.IsTypeAwaitable(returnType, out _))
        {
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
                else
                {
                    return Expression.Call(
                        ExecuteTaskOfTMethod.MakeGenericMethod(typeArg),
                        methodCall,
                        HttpContextExpr);
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
                else
                {
                    return Expression.Call(
                        ExecuteValueTaskOfTMethod.MakeGenericMethod(typeArg),
                        methodCall,
                        HttpContextExpr);
                }
            }
            else
            {
                // TODO: Handle custom awaitables
                throw new NotSupportedException($"Unsupported return type: {returnType}");
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
        else if (returnType.IsValueType)
        {
            var box = Expression.TypeAs(methodCall, typeof(object));
            return Expression.Call(JsonResultWriteResponseAsyncMethod, HttpResponseExpr, box, Expression.Constant(CancellationToken.None));
        }
        else
        {
            return Expression.Call(JsonResultWriteResponseAsyncMethod, HttpResponseExpr, methodCall, Expression.Constant(CancellationToken.None));
        }
    }

    private static Func<object?, HttpContext, Task> HandleRequestBodyAndCompileRequestDelegate(Expression responseWritingMethodCall, FactoryContext factoryContext)
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

    private static Func<object?, HttpContext, Task> HandleRequestBodyAndCompileRequestDelegateForJson(Expression responseWritingMethodCall, FactoryContext factoryContext)
    {
        Debug.Assert(factoryContext.JsonRequestBodyParameter is not null, "factoryContext.JsonRequestBodyParameter is null for a JSON body.");

        var bodyType = factoryContext.JsonRequestBodyParameter.ParameterType;
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
                    factoryContext.ThrowOnBadRequest);

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
                    factoryContext.ThrowOnBadRequest);

                if (!successful)
                {
                    return;
                }

                await continuation(target, httpContext, bodyValue);
            };
        }

        static async Task<(object? FormValue, bool Successful)> TryReadBodyAsync(
            HttpContext httpContext,
            Type bodyType,
            string parameterTypeName,
            string parameterName,
            bool allowEmptyRequestBody,
            bool throwOnBadRequest)
        {
            object? defaultBodyValue = null;

            if (allowEmptyRequestBody && bodyType.IsValueType)
            {
                defaultBodyValue = Activator.CreateInstance(bodyType);
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
                    bodyValue = await httpContext.Request.ReadFromJsonAsync(bodyType);
                }
                catch (IOException ex)
                {
                    Log.RequestBodyIOException(httpContext, ex);
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

    private static Func<object?, HttpContext, Task> HandleRequestBodyAndCompileRequestDelegateForForm(
        Expression responseWritingMethodCall,
        FactoryContext factoryContext)
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

            if (feature?.CanHaveBody == true)
            {
                if (!httpContext.Request.HasFormContentType)
                {
                    Log.UnexpectedNonFormContentType(httpContext, httpContext.Request.ContentType, throwOnBadRequest);
                    httpContext.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    return (null, false);
                }

                ThrowIfRequestIsAuthenticated(httpContext);

                try
                {
                    formValue = await httpContext.Request.ReadFormAsync();
                }
                catch (IOException ex)
                {
                    Log.RequestBodyIOException(httpContext, ex);
                    return (null, false);
                }
                catch (InvalidDataException ex)
                {
                    Log.InvalidFormRequestBody(httpContext, parameterTypeName, parameterName, ex, throwOnBadRequest);
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return (null, false);
                }
            }

            return (formValue, true);
        }

        static void ThrowIfRequestIsAuthenticated(HttpContext httpContext)
        {
            if (httpContext.Connection.ClientCertificate is not null)
            {
                throw new BadHttpRequestException(
                    "Support for binding parameters from an HTTP request's form is not currently supported " +
                    "if the request is associated with a client certificate. Use of an HTTP request form is " +
                    "not currently secure for HTTP requests in scenarios which require authentication.");
            }

            if (!StringValues.IsNullOrEmpty(httpContext.Request.Headers.Authorization))
            {
                throw new BadHttpRequestException(
                    "Support for binding parameters from an HTTP request's form is not currently supported " +
                    "if the request contains an \"Authorization\" HTTP request header. Use of an HTTP request form is " +
                    "not currently secure for HTTP requests in scenarios which require authentication.");
            }

            if (!StringValues.IsNullOrEmpty(httpContext.Request.Headers.Cookie))
            {
                throw new BadHttpRequestException(
                    "Support for binding parameters from an HTTP request's form is not currently supported " +
                    "if the request contains a \"Cookie\" HTTP request header. Use of an HTTP request form is " +
                    "not currently secure for HTTP requests in scenarios which require authentication.");
            }
        }
    }

    private static Expression GetValueFromProperty(Expression sourceExpression, string key, Type? returnType = null)
    {
        var itemProperty = sourceExpression.Type.GetProperty("Item");
        var indexArguments = new[] { Expression.Constant(key) };
        var indexExpression = Expression.MakeIndex(sourceExpression, itemProperty, indexArguments);
        return Expression.Convert(indexExpression, returnType ?? typeof(string));
    }

    private static Expression BindParameterFromService(ParameterInfo parameter, FactoryContext factoryContext)
    {
        var isOptional = IsOptionalParameter(parameter, factoryContext);

        if (isOptional)
        {
            return Expression.Call(GetServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
        }
        return Expression.Call(GetRequiredServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
    }

    private static Expression BindParameterFromValue(ParameterInfo parameter, Expression valueExpression, FactoryContext factoryContext, string source)
    {
        var isOptional = IsOptionalParameter(parameter, factoryContext);

        var argument = Expression.Variable(parameter.ParameterType, $"{parameter.Name}_local");

        var parameterTypeNameConstant = Expression.Constant(TypeNameHelper.GetTypeDisplayName(parameter.ParameterType, fullName: false));
        var parameterNameConstant = Expression.Constant(parameter.Name);
        var sourceConstant = Expression.Constant(source);

        if (parameter.ParameterType == typeof(string))
        {
            return BindParameterFromExpression(parameter, valueExpression, factoryContext, source);
        }

        factoryContext.UsingTempSourceString = true;

        var underlyingNullableType = Nullable.GetUnderlyingType(parameter.ParameterType);
        var isNotNullable = underlyingNullableType is null;

        var nonNullableParameterType = underlyingNullableType ?? parameter.ParameterType;
        var tryParseMethodCall = ParameterBindingMethodCache.FindTryParseMethod(nonNullableParameterType);

        if (tryParseMethodCall is null)
        {
            var typeName = TypeNameHelper.GetTypeDisplayName(parameter.ParameterType, fullName: false);
            throw new InvalidOperationException($"No public static bool {typeName}.TryParse(string, out {typeName}) method found for {parameter.Name}.");
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

        // If the parameter is nullable, create a "parsedValue" local to TryParse into since we cannot use the parameter directly.
        var parsedValue = isNotNullable ? argument : Expression.Variable(nonNullableParameterType, "parsedValue");

        var failBlock = Expression.Block(
            Expression.Assign(WasParamCheckFailureExpr, Expression.Constant(true)),
            Expression.Call(LogParameterBindingFailedMethod,
                HttpContextExpr, parameterTypeNameConstant, parameterNameConstant,
                TempSourceStringExpr, Expression.Constant(factoryContext.ThrowOnBadRequest)));

        var tryParseCall = tryParseMethodCall(parsedValue);

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

        // If the parameter is nullable, we need to assign the "parsedValue" local to the nullable parameter on success.
        Expression tryParseExpression = isNotNullable ?
            Expression.IfThen(Expression.Not(tryParseCall), failBlock) :
            Expression.Block(new[] { parsedValue },
                Expression.IfThenElse(tryParseCall,
                    Expression.Assign(argument, Expression.Convert(parsedValue, parameter.ParameterType)),
                    failBlock));

        var ifNotNullTryParse = !parameter.HasDefaultValue ?
            Expression.IfThen(TempSourceStringNotNullExpr, tryParseExpression) :
            Expression.IfThenElse(TempSourceStringNotNullExpr,
                tryParseExpression,
                Expression.Assign(argument, Expression.Constant(parameter.DefaultValue)));

        var fullParamCheckBlock = !isOptional
            ? Expression.Block(
                // tempSourceString = httpContext.RequestValue["id"];
                Expression.Assign(TempSourceStringExpr, valueExpression),
                // if (tempSourceString == null) { ... } only produced when parameter is required
                checkRequiredParaseableParameterBlock,
                // if (tempSourceString != null) { ... }
                ifNotNullTryParse)
            : Expression.Block(
                // tempSourceString = httpContext.RequestValue["id"];
                Expression.Assign(TempSourceStringExpr, valueExpression),
                // if (tempSourceString != null) { ... }
                ifNotNullTryParse);

        factoryContext.ExtraLocals.Add(argument);
        factoryContext.ParamCheckExpressions.Add(fullParamCheckBlock);

        return argument;
    }

    private static Expression BindParameterFromExpression(
        ParameterInfo parameter,
        Expression valueExpression,
        FactoryContext factoryContext,
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

            factoryContext.ExtraLocals.Add(argument);
            factoryContext.ParamCheckExpressions.Add(checkRequiredStringParameterBlock);
            return argument;
        }

        // Allow nullable parameters that don't have a default value
        if (nullability.ReadState != NullabilityState.NotNull && !parameter.HasDefaultValue)
        {
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

    private static Expression BindParameterFromProperty(ParameterInfo parameter, MemberExpression property, string key, FactoryContext factoryContext, string source) =>
        BindParameterFromValue(parameter, GetValueFromProperty(property, key), factoryContext, source);

    private static Expression BindParameterFromRouteValueOrQueryString(ParameterInfo parameter, string key, FactoryContext factoryContext)
    {
        var routeValue = GetValueFromProperty(RouteValuesExpr, key);
        var queryValue = GetValueFromProperty(QueryExpr, key);
        return BindParameterFromValue(parameter, Expression.Coalesce(routeValue, queryValue), factoryContext, "route or query string");
    }

    private static Expression BindParameterFromBindAsync(ParameterInfo parameter, FactoryContext factoryContext)
    {
        // We reference the boundValues array by parameter index here
        var nullability = factoryContext.NullabilityContext.Create(parameter);
        var isOptional = IsOptionalParameter(parameter, factoryContext);

        // Get the BindAsync method for the type.
        var bindAsyncMethod = ParameterBindingMethodCache.FindBindAsyncMethod(parameter);
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

    private static Expression BindParameterFromFormFiles(
        ParameterInfo parameter,
        FactoryContext factoryContext)
    {
        if (factoryContext.FirstFormRequestBodyParameter is null)
        {
            factoryContext.FirstFormRequestBodyParameter = parameter;
        }

        factoryContext.TrackedParameters.Add(parameter.Name!, RequestDelegateFactoryConstants.FormFileParameter);

        // Do not duplicate the metadata if there are multiple form parameters
        if (!factoryContext.ReadForm)
        {
            factoryContext.Metadata.Add(new AcceptsMetadata(parameter.ParameterType, factoryContext.AllowEmptyRequestBody, FormFileContentType));
        }

        factoryContext.ReadForm = true;

        return BindParameterFromExpression(parameter, FormFilesExpr, factoryContext, "body");
    }

    private static Expression BindParameterFromFormFile(
        ParameterInfo parameter,
        string key,
        FactoryContext factoryContext,
        string trackedParameterSource)
    {
        if (factoryContext.FirstFormRequestBodyParameter is null)
        {
            factoryContext.FirstFormRequestBodyParameter = parameter;
        }

        factoryContext.TrackedParameters.Add(key, trackedParameterSource);

        // Do not duplicate the metadata if there are multiple form parameters
        if (!factoryContext.ReadForm)
        {
            factoryContext.Metadata.Add(new AcceptsMetadata(parameter.ParameterType, factoryContext.AllowEmptyRequestBody, FormFileContentType));
        }

        factoryContext.ReadForm = true;

        var valueExpression = GetValueFromProperty(FormFilesExpr, key, typeof(IFormFile));

        return BindParameterFromExpression(parameter, valueExpression, factoryContext, "form file");
    }

    private static Expression BindParameterFromBody(ParameterInfo parameter, bool allowEmpty, FactoryContext factoryContext)
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
        factoryContext.Metadata.Add(new AcceptsMetadata(parameter.ParameterType, factoryContext.AllowEmptyRequestBody, DefaultAcceptsContentType));

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

    private static bool IsOptionalParameter(ParameterInfo parameter, FactoryContext factoryContext)
    {
        // - Parameters representing value or reference types with a default value
        // under any nullability context are treated as optional.
        // - Value type parameters without a default value in an oblivious
        // nullability context are required.
        // - Reference type parameters without a default value in an oblivious
        // nullability context are optional.
        var nullability = factoryContext.NullabilityContext.Create(parameter);
        return parameter.HasDefaultValue
            || nullability.ReadState != NullabilityState.NotNull;
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
    private static async Task ExecuteObjectReturn(object? obj, HttpContext httpContext)
    {
        // See if we need to unwrap Task<object> or ValueTask<object>
        if (obj is Task<object> taskObj)
        {
            obj = await taskObj;
        }
        else if (obj is ValueTask<object> valueTaskObj)
        {
            obj = await valueTaskObj;
        }
        else if (obj is Task<IResult?> task)
        {
            await ExecuteTaskResult(task, httpContext);
            return;
        }
        else if (obj is ValueTask<IResult?> valueTask)
        {
            await ExecuteValueTaskResult(valueTask, httpContext);
            return;
        }
        else if (obj is Task<string?> taskString)
        {
            await ExecuteTaskOfString(taskString, httpContext);
            return;
        }
        else if (obj is ValueTask<string?> valueTaskString)
        {
            await ExecuteValueTaskOfString(valueTaskString, httpContext);
            return;
        }

        // Terminal built ins
        if (obj is IResult result)
        {
            await ExecuteResultWriteResponse(result, httpContext);
        }
        else if (obj is string stringValue)
        {
            SetPlaintextContentType(httpContext);
            await httpContext.Response.WriteAsync(stringValue);
        }
        else
        {
            // Otherwise, we JSON serialize when we reach the terminal state
            await httpContext.Response.WriteAsJsonAsync(obj);
        }
    }

    private static Task ExecuteTask<T>(Task<T> task, HttpContext httpContext)
    {
        EnsureRequestTaskNotNull(task);

        static async Task ExecuteAwaited(Task<T> task, HttpContext httpContext)
        {
            await httpContext.Response.WriteAsJsonAsync(await task);
        }

        if (task.IsCompletedSuccessfully)
        {
            return httpContext.Response.WriteAsJsonAsync(task.GetAwaiter().GetResult());
        }

        return ExecuteAwaited(task, httpContext);
    }

    private static Task ExecuteTaskOfString(Task<string?> task, HttpContext httpContext)
    {
        SetPlaintextContentType(httpContext);
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
        SetPlaintextContentType(httpContext);
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
        }

        return ExecuteAwaited(task);
    }

    private static Task ExecuteValueTaskOfT<T>(ValueTask<T> task, HttpContext httpContext)
    {
        static async Task ExecuteAwaited(ValueTask<T> task, HttpContext httpContext)
        {
            await httpContext.Response.WriteAsJsonAsync(await task);
        }

        if (task.IsCompletedSuccessfully)
        {
            return httpContext.Response.WriteAsJsonAsync(task.GetAwaiter().GetResult());
        }

        return ExecuteAwaited(task, httpContext);
    }

    private static Task ExecuteValueTaskOfString(ValueTask<string?> task, HttpContext httpContext)
    {
        SetPlaintextContentType(httpContext);

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

    private class FactoryContext
    {
        // Options
        public IServiceProviderIsService? ServiceProviderIsService { get; init; }
        public List<string>? RouteParameters { get; init; }
        public bool ThrowOnBadRequest { get; init; }
        public bool DisableInferredFromBody { get; init; }

        // Temporary State
        public ParameterInfo? JsonRequestBodyParameter { get; set; }
        public bool AllowEmptyRequestBody { get; set; }

        public bool UsingTempSourceString { get; set; }
        public List<ParameterExpression> ExtraLocals { get; } = new();
        public List<Expression> ParamCheckExpressions { get; } = new();
        public List<Func<HttpContext, ValueTask<object?>>> ParameterBinders { get; } = new();

        public Dictionary<string, string> TrackedParameters { get; } = new();
        public bool HasMultipleBodyParameters { get; set; }
        public bool HasInferredBody { get; set; }

        public List<object> Metadata { get; } = new();

        public NullabilityInfoContext NullabilityContext { get; } = new();

        public bool ReadForm { get; set; }
        public ParameterInfo? FirstFormRequestBodyParameter { get; set; }
    }

    private static class RequestDelegateFactoryConstants
    {
        public const string RouteAttribute = "Route (Attribute)";
        public const string QueryAttribute = "Query (Attribute)";
        public const string HeaderAttribute = "Header (Attribute)";
        public const string BodyAttribute = "Body (Attribute)";
        public const string ServiceAttribute = "Service (Attribute)";
        public const string FormFileAttribute = "Form File (Attribute)";
        public const string RouteParameter = "Route (Inferred)";
        public const string QueryStringParameter = "Query String (Inferred)";
        public const string ServiceParameter = "Services (Inferred)";
        public const string BodyParameter = "Body (Inferred)";
        public const string RouteOrQueryStringParameter = "Route or Query String (Inferred)";
        public const string FormFileParameter = "Form File (Inferred)";
    }

    private static partial class Log
    {
        private const string InvalidJsonRequestBodyMessage = @"Failed to read parameter ""{ParameterType} {ParameterName}"" from the request body as JSON.";
        private const string InvalidJsonRequestBodyExceptionMessage = @"Failed to read parameter ""{0} {1}"" from the request body as JSON.";

        private const string ParameterBindingFailedLogMessage = @"Failed to bind parameter ""{ParameterType} {ParameterName}"" from ""{SourceValue}"".";
        private const string ParameterBindingFailedExceptionMessage = @"Failed to bind parameter ""{0} {1}"" from ""{2}"".";

        private const string RequiredParameterNotProvidedLogMessage = @"Required parameter ""{ParameterType} {ParameterName}"" was not provided from {Source}.";
        private const string RequiredParameterNotProvidedExceptionMessage = @"Required parameter ""{0} {1}"" was not provided from {2}.";

        private const string UnexpectedJsonContentTypeLogMessage = @"Expected a supported JSON media type but got ""{ContentType}"".";
        private const string UnexpectedJsonContentTypeExceptionMessage = @"Expected a supported JSON media type but got ""{0}"".";

        private const string ImplicitBodyNotProvidedLogMessage = @"Implicit body inferred for parameter ""{ParameterName}"" but no body was provided. Did you mean to use a Service instead?";
        private const string ImplicitBodyNotProvidedExceptionMessage = @"Implicit body inferred for parameter ""{0}"" but no body was provided. Did you mean to use a Service instead?";

        private const string InvalidFormRequestBodyMessage = @"Failed to read parameter ""{ParameterType} {ParameterName}"" from the request body as form.";
        private const string InvalidFormRequestBodyExceptionMessage = @"Failed to read parameter ""{0} {1}"" from the request body as form.";

        private const string UnexpectedFormContentTypeLogMessage = @"Expected a supported form media type but got ""{ContentType}"".";
        private const string UnexpectedFormContentTypeExceptionMessage = @"Expected a supported form media type but got ""{0}"".";

        // This doesn't take a shouldThrow parameter because an IOException indicates an aborted request rather than a "bad" request so
        // a BadHttpRequestException feels wrong. The client shouldn't be able to read the Developer Exception Page at any rate.
        public static void RequestBodyIOException(HttpContext httpContext, IOException exception)
            => RequestBodyIOException(GetLogger(httpContext), exception);

        [LoggerMessage(1, LogLevel.Debug, "Reading the request body failed with an IOException.", EventName = "RequestBodyIOException")]
        private static partial void RequestBodyIOException(ILogger logger, IOException exception);

        public static void InvalidJsonRequestBody(HttpContext httpContext, string parameterTypeName, string parameterName, Exception exception, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, InvalidJsonRequestBodyExceptionMessage, parameterTypeName, parameterName);
                throw new BadHttpRequestException(message, exception);
            }

            InvalidJsonRequestBody(GetLogger(httpContext), parameterTypeName, parameterName, exception);
        }

        [LoggerMessage(2, LogLevel.Debug, InvalidJsonRequestBodyMessage, EventName = "InvalidJsonRequestBody")]
        private static partial void InvalidJsonRequestBody(ILogger logger, string parameterType, string parameterName, Exception exception);

        public static void ParameterBindingFailed(HttpContext httpContext, string parameterTypeName, string parameterName, string sourceValue, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, ParameterBindingFailedExceptionMessage, parameterTypeName, parameterName, sourceValue);
                throw new BadHttpRequestException(message);
            }

            ParameterBindingFailed(GetLogger(httpContext), parameterTypeName, parameterName, sourceValue);
        }

        [LoggerMessage(3, LogLevel.Debug, ParameterBindingFailedLogMessage, EventName = "ParameterBindingFailed")]
        private static partial void ParameterBindingFailed(ILogger logger, string parameterType, string parameterName, string sourceValue);

        public static void RequiredParameterNotProvided(HttpContext httpContext, string parameterTypeName, string parameterName, string source, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, RequiredParameterNotProvidedExceptionMessage, parameterTypeName, parameterName, source);
                throw new BadHttpRequestException(message);
            }

            RequiredParameterNotProvided(GetLogger(httpContext), parameterTypeName, parameterName, source);
        }

        [LoggerMessage(4, LogLevel.Debug, RequiredParameterNotProvidedLogMessage, EventName = "RequiredParameterNotProvided")]
        private static partial void RequiredParameterNotProvided(ILogger logger, string parameterType, string parameterName, string source);

        public static void ImplicitBodyNotProvided(HttpContext httpContext, string parameterName, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, ImplicitBodyNotProvidedExceptionMessage, parameterName);
                throw new BadHttpRequestException(message);
            }

            ImplicitBodyNotProvided(GetLogger(httpContext), parameterName);
        }

        [LoggerMessage(5, LogLevel.Debug, ImplicitBodyNotProvidedLogMessage, EventName = "ImplicitBodyNotProvided")]
        private static partial void ImplicitBodyNotProvided(ILogger logger, string parameterName);

        public static void UnexpectedJsonContentType(HttpContext httpContext, string? contentType, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, UnexpectedJsonContentTypeExceptionMessage, contentType);
                throw new BadHttpRequestException(message, StatusCodes.Status415UnsupportedMediaType);
            }

            UnexpectedJsonContentType(GetLogger(httpContext), contentType ?? "(none)");
        }

        [LoggerMessage(6, LogLevel.Debug, UnexpectedJsonContentTypeLogMessage, EventName = "UnexpectedContentType")]
        private static partial void UnexpectedJsonContentType(ILogger logger, string contentType);

        public static void UnexpectedNonFormContentType(HttpContext httpContext, string? contentType, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, UnexpectedFormContentTypeExceptionMessage, contentType);
                throw new BadHttpRequestException(message, StatusCodes.Status415UnsupportedMediaType);
            }

            UnexpectedNonFormContentType(GetLogger(httpContext), contentType ?? "(none)");
        }

        [LoggerMessage(7, LogLevel.Debug, UnexpectedFormContentTypeLogMessage, EventName = "UnexpectedNonFormContentType")]
        private static partial void UnexpectedNonFormContentType(ILogger logger, string contentType);

        public static void InvalidFormRequestBody(HttpContext httpContext, string parameterTypeName, string parameterName, Exception exception, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, InvalidFormRequestBodyExceptionMessage, parameterTypeName, parameterName);
                throw new BadHttpRequestException(message, exception);
            }

            InvalidFormRequestBody(GetLogger(httpContext), parameterTypeName, parameterName, exception);
        }

        [LoggerMessage(8, LogLevel.Debug, InvalidFormRequestBodyMessage, EventName = "InvalidFormRequestBody")]
        private static partial void InvalidFormRequestBody(ILogger logger, string parameterType, string parameterName, Exception exception);

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

    private static void SetPlaintextContentType(HttpContext httpContext)
    {
        httpContext.Response.ContentType ??= "text/plain; charset=utf-8";
    }

    private static string BuildErrorMessageForMultipleBodyParameters(FactoryContext factoryContext)
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

    private static string BuildErrorMessageForInferredBodyParameter(FactoryContext factoryContext)
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

    private static string BuildErrorMessageForFormAndJsonBodyParameters(FactoryContext factoryContext)
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

    private static void FormatTrackedParameters(FactoryContext factoryContext, StringBuilder errorMessage)
    {
        foreach (var kv in factoryContext.TrackedParameters)
        {
            errorMessage.AppendLine(FormattableString.Invariant($"{kv.Key,-19} | {kv.Value,-15}"));
        }
    }
}
