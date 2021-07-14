// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Creates <see cref="RequestDelegate"/> implementations from <see cref="Delegate"/> request handlers.
    /// </summary>
    public static partial class RequestDelegateFactory
    {
        private static readonly MethodInfo ExecuteTaskOfTMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTask), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteTaskOfStringMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTaskOfString), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteValueTaskOfTMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskOfT), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteValueTaskMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTask), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteValueTaskOfStringMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskOfString), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteTaskResultOfTMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTaskResult), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteValueResultTaskOfTMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskResult), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteObjectReturnMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteObjectReturn), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo GetRequiredServiceMethod = typeof(ServiceProviderServiceExtensions).GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(IServiceProvider) })!;
        private static readonly MethodInfo ResultWriteResponseAsyncMethod = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteResultWriteResponse), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo StringResultWriteResponseAsyncMethod = GetMethodInfo<Func<HttpResponse, string, Task>>((response, text) => HttpResponseWritingExtensions.WriteAsync(response, text, default));
        private static readonly MethodInfo JsonResultWriteResponseAsyncMethod = GetMethodInfo<Func<HttpResponse, object, Task>>((response, value) => HttpResponseJsonExtensions.WriteAsJsonAsync(response, value, default));
        private static readonly MethodInfo LogParameterBindingFailureMethod = GetMethodInfo<Action<HttpContext, string, string, string>>((httpContext, parameterType, parameterName, sourceValue) =>
            Log.ParameterBindingFailed(httpContext, parameterType, parameterName, sourceValue));

        private static readonly ParameterExpression TargetExpr = Expression.Parameter(typeof(object), "target");
        private static readonly ParameterExpression HttpContextExpr = Expression.Parameter(typeof(HttpContext), "httpContext");
        private static readonly ParameterExpression BodyValueExpr = Expression.Parameter(typeof(object), "bodyValue");
        private static readonly ParameterExpression WasTryParseFailureExpr = Expression.Variable(typeof(bool), "wasTryParseFailure");
        private static readonly ParameterExpression TempSourceStringExpr = TryParseMethodCache.TempSourceStringExpr;

        private static readonly MemberExpression RequestServicesExpr = Expression.Property(HttpContextExpr, nameof(HttpContext.RequestServices));
        private static readonly MemberExpression HttpRequestExpr = Expression.Property(HttpContextExpr, nameof(HttpContext.Request));
        private static readonly MemberExpression HttpResponseExpr = Expression.Property(HttpContextExpr, nameof(HttpContext.Response));
        private static readonly MemberExpression RequestAbortedExpr = Expression.Property(HttpContextExpr, nameof(HttpContext.RequestAborted));
        private static readonly MemberExpression UserExpr = Expression.Property(HttpContextExpr, nameof(HttpContext.User));
        private static readonly MemberExpression RouteValuesExpr = Expression.Property(HttpRequestExpr, nameof(HttpRequest.RouteValues));
        private static readonly MemberExpression QueryExpr = Expression.Property(HttpRequestExpr, nameof(HttpRequest.Query));
        private static readonly MemberExpression HeadersExpr = Expression.Property(HttpRequestExpr, nameof(HttpRequest.Headers));
        private static readonly MemberExpression StatusCodeExpr = Expression.Property(HttpResponseExpr, nameof(HttpResponse.StatusCode));
        private static readonly MemberExpression ContentTypeExpr = Expression.Property(HttpResponseExpr, nameof(HttpResponse.ContentType));
        private static readonly MemberExpression CompletedTaskExpr = Expression.Property(null, (PropertyInfo)GetMemberInfo<Func<Task>>(() => Task.CompletedTask));

        private static readonly BinaryExpression TempSourceStringNotNullExpr = Expression.NotEqual(TempSourceStringExpr, Expression.Constant(null));

        /// <summary>
        /// Creates a <see cref="RequestDelegate"/> implementation for <paramref name="action"/>.
        /// </summary>
        /// <param name="action">A request handler with any number of custom parameters that often produces a response with its return value.</param>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> instance used to detect which parameters are services.</param>
        /// <returns>The <see cref="RequestDelegate"/>.</returns>
        public static RequestDelegate Create(Delegate action, IServiceProvider? serviceProvider)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var targetExpression = action.Target switch
            {
                object => Expression.Convert(TargetExpr, action.Target.GetType()),
                null => null,
            };

            var targetableRequestDelegate = CreateTargetableRequestDelegate(action.Method, serviceProvider, targetExpression);

            return httpContext =>
            {
                return targetableRequestDelegate(action.Target, httpContext);
            };
        }

        /// <summary>
        /// Creates a <see cref="RequestDelegate"/> implementation for <paramref name="methodInfo"/>.
        /// </summary>
        /// <param name="methodInfo">A static request handler with any number of custom parameters that often produces a response with its return value.</param>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> instance used to detect which parameters are services.</param>
        /// <returns>The <see cref="RequestDelegate"/>.</returns>
        public static RequestDelegate Create(MethodInfo methodInfo, IServiceProvider? serviceProvider)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            var targetableRequestDelegate = CreateTargetableRequestDelegate(methodInfo, serviceProvider, targetExpression: null);

            return httpContext =>
            {
                return targetableRequestDelegate(null, httpContext);
            };
        }

        /// <summary>
        /// Creates a <see cref="RequestDelegate"/> implementation for <paramref name="methodInfo"/>.
        /// </summary>
        /// <param name="methodInfo">A request handler with any number of custom parameters that often produces a response with its return value.</param>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> instance used to detect which parameters are services.</param>
        /// <param name="targetFactory">Creates the <see langword="this"/> for the non-static method.</param>
        /// <returns>The <see cref="RequestDelegate"/>.</returns>
        public static RequestDelegate Create(MethodInfo methodInfo, IServiceProvider? serviceProvider, Func<HttpContext, object> targetFactory)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            if (targetFactory is null)
            {
                throw new ArgumentNullException(nameof(targetFactory));
            }

            if (methodInfo.DeclaringType is null)
            {
                throw new ArgumentException($"A {nameof(targetFactory)} was provided, but {nameof(methodInfo)} does not have a Declaring type.");
            }

            var targetExpression = Expression.Convert(TargetExpr, methodInfo.DeclaringType);
            var targetableRequestDelegate = CreateTargetableRequestDelegate(methodInfo, serviceProvider, targetExpression);

            return httpContext =>
            {
                return targetableRequestDelegate(targetFactory(httpContext), httpContext);
            };
        }

        private static Func<object?, HttpContext, Task> CreateTargetableRequestDelegate(MethodInfo methodInfo, IServiceProvider? serviceProvider, Expression? targetExpression)
        {
            // Non void return type

            // Task Invoke(HttpContext httpContext)
            // {
            //     // Action parameters are bound from the request, services, etc... based on attribute and type information.
            //     return ExecuteTask(action(...), httpContext);
            // }

            // void return type

            // Task Invoke(HttpContext httpContext)
            // {
            //     action(...);
            //     return default;
            // }

            var factoryContext = new FactoryContext()
            {
                ServiceProviderIsService = serviceProvider?.GetService<IServiceProviderIsService>()
            };

            var arguments = CreateArguments(methodInfo.GetParameters(), factoryContext);

            var responseWritingMethodCall = factoryContext.TryParseParams.Count > 0 ?
                CreateTryParseCheckingResponseWritingMethodCall(methodInfo, targetExpression, arguments, factoryContext) :
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

            return args;
        }

        private static Expression CreateArgument(ParameterInfo parameter, FactoryContext factoryContext)
        {
            if (parameter.Name is null)
            {
                throw new InvalidOperationException("A parameter does not have a name! Was it generated? All parameters must be named.");
            }

            var parameterCustomAttributes = parameter.GetCustomAttributes();

            if (parameterCustomAttributes.OfType<IFromRouteMetadata>().FirstOrDefault() is { } routeAttribute)
            {
                return BindParameterFromProperty(parameter, RouteValuesExpr, routeAttribute.Name ?? parameter.Name, factoryContext);
            }
            else if (parameterCustomAttributes.OfType<IFromQueryMetadata>().FirstOrDefault() is { } queryAttribute)
            {
                return BindParameterFromProperty(parameter, QueryExpr, queryAttribute.Name ?? parameter.Name, factoryContext);
            }
            else if (parameterCustomAttributes.OfType<IFromHeaderMetadata>().FirstOrDefault() is { } headerAttribute)
            {
                return BindParameterFromProperty(parameter, HeadersExpr, headerAttribute.Name ?? parameter.Name, factoryContext);
            }
            else if (parameterCustomAttributes.OfType<IFromBodyMetadata>().FirstOrDefault() is { } bodyAttribute)
            {
                return BindParameterFromBody(parameter.ParameterType, bodyAttribute.AllowEmpty, factoryContext);
            }
            else if (parameter.CustomAttributes.Any(a => typeof(IFromServiceMetadata).IsAssignableFrom(a.AttributeType)))
            {
                return Expression.Call(GetRequiredServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
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
            else if (parameter.ParameterType == typeof(string) || TryParseMethodCache.HasTryParseMethod(parameter))
            {
                return BindParameterFromRouteValueOrQueryString(parameter, parameter.Name, factoryContext);
            }
            else
            {
                if (factoryContext.ServiceProviderIsService is IServiceProviderIsService serviceProviderIsService)
                {
                    // If the parameter resolves as a service then get it from services
                    if (serviceProviderIsService.IsService(parameter.ParameterType))
                    {
                        return Expression.Call(GetRequiredServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
                    }
                }

                return BindParameterFromBody(parameter.ParameterType, allowEmpty: false, factoryContext);
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

        // If we're calling TryParse and wasTryParseFailure indicates it failed, set a 400 StatusCode instead of calling the method.
        private static Expression CreateTryParseCheckingResponseWritingMethodCall(
            MethodInfo methodInfo, Expression? target, Expression[] arguments, FactoryContext factoryContext)
        {
            // {
            //     string tempSourceString;
            //     bool wasTryParseFailure = false;
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
            //             wasTryParseFailure = true;
            //             Log.ParameterBindingFailed(httpContext, "Int32", "id", tempSourceString)
            //         }
            //     }
            //
            //     tempSourceString = httpContext.RouteValue["param2"];
            //     // ...
            //
            //     return wasTryParseFailure ?
            //         {
            //              httpContext.Response.StatusCode = 400;
            //              return Task.CompletedTask;
            //         } :
            //         {
            //             // Logic generated by AddResponseWritingToMethodCall() that calls action(param1_local, param2_local, ...)
            //         };
            // }

            var localVariables = new ParameterExpression[factoryContext.TryParseParams.Count + 1];
            var tryParseAndCallMethod = new Expression[factoryContext.TryParseParams.Count + 1];

            for (var i = 0; i < factoryContext.TryParseParams.Count; i++)
            {
                (localVariables[i], tryParseAndCallMethod[i]) = factoryContext.TryParseParams[i];
            }

            localVariables[factoryContext.TryParseParams.Count] = WasTryParseFailureExpr;

            var set400StatusAndReturnCompletedTask = Expression.Block(
                    Expression.Assign(StatusCodeExpr, Expression.Constant(400)),
                    CompletedTaskExpr);

            var methodCall = CreateMethodCall(methodInfo, target, arguments);

            var checkWasTryParseFailure = Expression.Condition(WasTryParseFailureExpr,
                set400StatusAndReturnCompletedTask,
                AddResponseWritingToMethodCall(methodCall, methodInfo.ReturnType));

            tryParseAndCallMethod[factoryContext.TryParseParams.Count] = checkWasTryParseFailure;

            return Expression.Block(localVariables, tryParseAndCallMethod);
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
                    // ExecuteTask<T>(action(..), httpContext);
                    else if (typeArg == typeof(string))
                    {
                        var conditionalContentTypeExpr = HandleNullContentType();
                        return Expression.Block(
                                conditionalContentTypeExpr,
                                Expression.Call(ExecuteTaskOfStringMethod, methodCall, HttpContextExpr));
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
                    // ExecuteTask<T>(action(..), httpContext);
                    else if (typeArg == typeof(string))
                    {
                        var conditionalContentTypeExpr = HandleNullContentType();
                        return Expression.Block(
                                conditionalContentTypeExpr,
                                Expression.Call(ExecuteValueTaskOfStringMethod, methodCall, HttpContextExpr));
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
                return Expression.Call(ResultWriteResponseAsyncMethod, methodCall, HttpContextExpr);
            }
            else if (returnType == typeof(string))
            {
                var conditionalContentTypeExpr = HandleNullContentType();

                return Expression.Block(
                    conditionalContentTypeExpr,
                    Expression.Call(StringResultWriteResponseAsyncMethod, HttpResponseExpr, methodCall, Expression.Constant(CancellationToken.None))
                );
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

        private static ConditionalExpression HandleNullContentType()
        {
            return Expression.IfThen(
                   Expression.Equal(
                     ContentTypeExpr,
                     Expression.Constant(null)
                 ),
                 Expression.Assign(ContentTypeExpr, Expression.Constant(ContentTypeConstants.PlainTextContentTypeWithCharset))
            );
        }

        private static Func<object?, HttpContext, Task> HandleRequestBodyAndCompileRequestDelegate(Expression responseWritingMethodCall, FactoryContext factoryContext)
        {
            if (factoryContext.JsonRequestBodyType is null)
            {
                return Expression.Lambda<Func<object?, HttpContext, Task>>(
                    responseWritingMethodCall, TargetExpr, HttpContextExpr).Compile();
            }

            // We need to generate the code for reading from the body before calling into the delegate
            var invoker = Expression.Lambda<Func<object?, HttpContext, object?, Task>>(
                responseWritingMethodCall, TargetExpr, HttpContextExpr, BodyValueExpr).Compile();

            var bodyType = factoryContext.JsonRequestBodyType;
            object? defaultBodyValue = null;

            if (factoryContext.AllowEmptyRequestBody && bodyType.IsValueType)
            {
                defaultBodyValue = Activator.CreateInstance(bodyType);
            }

            return async (target, httpContext) =>
            {
                object? bodyValue;

                if (factoryContext.AllowEmptyRequestBody && httpContext.Request.ContentLength == 0)
                {
                    bodyValue = defaultBodyValue;
                }
                else
                {
                    try
                    {
                        bodyValue = await httpContext.Request.ReadFromJsonAsync(bodyType);
                    }
                    catch (IOException ex)
                    {
                        Log.RequestBodyIOException(httpContext, ex);
                        return;
                    }
                    catch (InvalidDataException ex)
                    {
                        Log.RequestBodyInvalidDataException(httpContext, ex);
                        httpContext.Response.StatusCode = 400;
                        return;
                    }
                }

                await invoker(target, httpContext, bodyValue);
            };
        }

        private static Expression GetValueFromProperty(Expression sourceExpression, string key)
        {
            var itemProperty = sourceExpression.Type.GetProperty("Item");
            var indexArguments = new[] { Expression.Constant(key) };
            var indexExpression = Expression.MakeIndex(sourceExpression, itemProperty, indexArguments);
            return Expression.Convert(indexExpression, typeof(string));
        }

        private static Expression BindParameterFromValue(ParameterInfo parameter, Expression valueExpression, FactoryContext factoryContext)
        {
            if (parameter.ParameterType == typeof(string))
            {
                if (!parameter.HasDefaultValue)
                {
                    return valueExpression;
                }

                factoryContext.UsingTempSourceString = true;
                return Expression.Block(
                    Expression.Assign(TempSourceStringExpr, valueExpression),
                    Expression.Condition(TempSourceStringNotNullExpr,
                        TempSourceStringExpr,
                        Expression.Constant(parameter.DefaultValue)));
            }

            factoryContext.UsingTempSourceString = true;

            var underlyingNullableType = Nullable.GetUnderlyingType(parameter.ParameterType);
            var isNotNullable = underlyingNullableType is null;

            var nonNullableParameterType = underlyingNullableType ?? parameter.ParameterType;
            var tryParseMethodCall = TryParseMethodCache.FindTryParseMethod(nonNullableParameterType);

            if (tryParseMethodCall is null)
            {
                throw new InvalidOperationException($"No public static bool {parameter.ParameterType.Name}.TryParse(string, out {parameter.ParameterType.Name}) method found for {parameter.Name}.");
            }

            // string tempSourceString;
            // bool wasTryParseFailure = false;
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
            //         wasTryParseFailure = true;
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
            //         wasTryParseFailure = true;
            //         Log.ParameterBindingFailed(httpContext, "Int32", "id", tempSourceString)
            //     }
            // }
            // else
            // {
            //     param2_local = 42;
            // }

            var argument = Expression.Variable(parameter.ParameterType, $"{parameter.Name}_local");

            // If the parameter is nullable, create a "parsedValue" local to TryParse into since we cannot the parameter directly.
            var parsedValue = isNotNullable ? argument : Expression.Variable(nonNullableParameterType, "parsedValue");

            var parameterTypeNameConstant = Expression.Constant(parameter.ParameterType.Name);
            var parameterNameConstant = Expression.Constant(parameter.Name);

            var failBlock = Expression.Block(
                Expression.Assign(WasTryParseFailureExpr, Expression.Constant(true)),
                Expression.Call(LogParameterBindingFailureMethod,
                    HttpContextExpr, parameterTypeNameConstant, parameterNameConstant, TempSourceStringExpr));

            var tryParseCall = tryParseMethodCall(parsedValue);

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

            var fullTryParseBlock = Expression.Block(
                // tempSourceString = httpContext.RequestValue["id"];
                Expression.Assign(TempSourceStringExpr, valueExpression),
                // if (tempSourceString != null) { ... }
                ifNotNullTryParse);

            factoryContext.TryParseParams.Add((argument, fullTryParseBlock));

            return argument;
        }

        private static Expression BindParameterFromProperty(ParameterInfo parameter, MemberExpression property, string key, FactoryContext factoryContext) =>
            BindParameterFromValue(parameter, GetValueFromProperty(property, key), factoryContext);

        private static Expression BindParameterFromRouteValueOrQueryString(ParameterInfo parameter, string key, FactoryContext factoryContext)
        {
            var routeValue = GetValueFromProperty(RouteValuesExpr, key);
            var queryValue = GetValueFromProperty(QueryExpr, key);
            return BindParameterFromValue(parameter, Expression.Coalesce(routeValue, queryValue), factoryContext);
        }

        private static Expression BindParameterFromBody(Type parameterType, bool allowEmpty, FactoryContext factoryContext)
        {
            if (factoryContext.JsonRequestBodyType is not null)
            {
                throw new InvalidOperationException("Action cannot have more than one FromBody attribute.");
            }

            factoryContext.JsonRequestBodyType = parameterType;
            factoryContext.AllowEmptyRequestBody = allowEmpty;

            return Expression.Convert(BodyValueExpr, parameterType);
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
            public Type? JsonRequestBodyType { get; set; }
            public bool AllowEmptyRequestBody { get; set; }
            public IServiceProviderIsService? ServiceProviderIsService { get; init; }

            public bool UsingTempSourceString { get; set; }
            public List<(ParameterExpression, Expression)> TryParseParams { get; } = new();
        }

        private static partial class Log
        {
            public static void RequestBodyIOException(HttpContext httpContext, IOException exception)
                => RequestBodyIOException(GetLogger(httpContext), exception);

            [LoggerMessage(1, LogLevel.Debug, "Reading the request body failed with an IOException.", EventName = "RequestBodyIOException")]
            private static partial void RequestBodyIOException(ILogger logger, IOException exception);

            public static void RequestBodyInvalidDataException(HttpContext httpContext, InvalidDataException exception)
                => RequestBodyInvalidDataException(GetLogger(httpContext), exception);

            [LoggerMessage(2, LogLevel.Debug, "Reading the request body failed with an InvalidDataException.", EventName = "RequestBodyInvalidDataException")]
            private static partial void RequestBodyInvalidDataException(ILogger logger, InvalidDataException exception);

            public static void ParameterBindingFailed(HttpContext httpContext, string parameterTypeName, string parameterName, string sourceValue)
                => ParameterBindingFailed(GetLogger(httpContext), parameterTypeName, parameterName, sourceValue);

            [LoggerMessage(3, LogLevel.Debug,
                @"Failed to bind parameter ""{ParameterType} {ParameterName}"" from ""{SourceValue}"".",
                EventName = "ParamaterBindingFailed")]
            private static partial void ParameterBindingFailed(ILogger logger, string parameterType, string parameterName, string sourceValue);

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
    }
}
