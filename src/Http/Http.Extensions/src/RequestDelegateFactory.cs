// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Builds <see cref="RequestDelegate"/> implementations from <see cref="Delegate"/> request handlers.
    /// </summary>
    public static class RequestDelegateFactory
    {
        private static readonly MethodInfo ChangeTypeMethodInfo = GetMethodInfo<Func<object, Type, object>>((value, type) => Convert.ChangeType(value, type, CultureInfo.InvariantCulture));
        private static readonly MethodInfo ExecuteTaskOfTMethodInfo = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTask), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteTaskOfStringMethodInfo = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTaskOfString), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteValueTaskOfTMethodInfo = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskOfT), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteValueTaskMethodInfo = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTask), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteValueTaskOfStringMethodInfo = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskOfString), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteTaskResultOfTMethodInfo = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteTaskResult), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteValueResultTaskOfTMethodInfo = typeof(RequestDelegateFactory).GetMethod(nameof(ExecuteValueTaskResult), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo GetRequiredServiceMethodInfo = typeof(ServiceProviderServiceExtensions).GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(IServiceProvider) })!;
        private static readonly MethodInfo ResultWriteResponseAsync = typeof(IResult).GetMethod(nameof(IResult.ExecuteAsync), BindingFlags.Public | BindingFlags.Instance)!;
        private static readonly MethodInfo StringResultWriteResponseAsync = GetMethodInfo<Func<HttpResponse, string, Task>>((response, text) => HttpResponseWritingExtensions.WriteAsync(response, text, default));
        private static readonly MethodInfo JsonResultWriteResponseAsync = GetMethodInfo<Func<HttpResponse, object, Task>>((response, value) => HttpResponseJsonExtensions.WriteAsJsonAsync(response, value, default));
        private static readonly MemberInfo CompletedTaskMemberInfo = GetMemberInfo<Func<Task>>(() => Task.CompletedTask);

        private static readonly ParameterExpression TargetArg = Expression.Parameter(typeof(object), "target");
        private static readonly ParameterExpression HttpContextParameter = Expression.Parameter(typeof(HttpContext), "httpContext");
        private static readonly ParameterExpression DeserializedBodyArg = Expression.Parameter(typeof(object), "bodyValue");

        private static readonly MemberExpression RequestServicesExpr = Expression.Property(HttpContextParameter, nameof(HttpContext.RequestServices));
        private static readonly MemberExpression HttpRequestExpr = Expression.Property(HttpContextParameter, nameof(HttpContext.Request));
        private static readonly MemberExpression HttpResponseExpr = Expression.Property(HttpContextParameter, nameof(HttpContext.Response));
        private static readonly MemberExpression RequestAbortedExpr = Expression.Property(HttpContextParameter, nameof(HttpContext.RequestAborted));

        /// <summary>
        /// Builds a <see cref="RequestDelegate"/> implementation for <paramref name="action"/>.
        /// </summary>
        /// <param name="action">A request handler with any number of custom parameters that often produces a response with its return value.</param>
        /// <returns>The <see cref="RequestDelegate"/></returns>
        public static RequestDelegate Build(Delegate action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var targetExpression = action.Target switch
            {
                { } => Expression.Convert(TargetArg, action.Target.GetType()),
                null => null,
            };

            var untargetedRequestDelegate = BuildRequestDelegate(action.Method, targetExpression);

            return httpContext =>
            {
                return untargetedRequestDelegate(action.Target, httpContext);
            };
        }

        /// <summary>
        /// Builds a <see cref="RequestDelegate"/> implementation for <paramref name="methodInfo"/>.
        /// </summary>
        /// <param name="methodInfo">A static request handler with any number of custom parameters that often produces a response with its return value.</param>
        /// <returns>The <see cref="RequestDelegate"/></returns>
        public static RequestDelegate Build(MethodInfo methodInfo)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            var untargetedRequestDelegate = BuildRequestDelegate(methodInfo, targetExpression: null);

            return httpContext =>
            {
                return untargetedRequestDelegate(null, httpContext);
            };
        }

        /// <summary>
        /// Builds a <see cref="RequestDelegate"/> implementation for <paramref name="methodInfo"/>.
        /// </summary>
        /// <param name="methodInfo">A request handler with any number of custom parameters that often produces a response with its return value.</param>
        /// <param name="targetFactory">Creates the <see langword="this"/> for the non-static method.</param>
        /// <returns>The <see cref="RequestDelegate"/></returns>
        public static RequestDelegate Build(MethodInfo methodInfo, Func<HttpContext, object> targetFactory)
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

            var targetExpression = Expression.Convert(TargetArg, methodInfo.DeclaringType);
            var untargetedRequestDelegate = BuildRequestDelegate(methodInfo, targetExpression);

            return httpContext =>
            {
                return untargetedRequestDelegate(targetFactory(httpContext), httpContext);
            };
        }

        private static Func<object?, HttpContext, Task> BuildRequestDelegate(MethodInfo methodInfo, Expression? targetExpression)
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

            var consumeBodyDirectly = false;
            var consumeBodyAsForm = false;
            Type? bodyType = null;
            var allowEmptyBody = false;

            // This argument represents the deserialized body returned from IHttpRequestReader
            // when the method has a FromBody attribute declared

            var methodParameters = methodInfo.GetParameters();
            var args = new List<Expression>(methodParameters.Length);

            foreach (var parameter in methodParameters)
            {
                Expression paramterExpression = Expression.Default(parameter.ParameterType);

                var parameterCustomAttributes = parameter.GetCustomAttributes();

                if (parameterCustomAttributes.OfType<IFromRouteMetadata>().FirstOrDefault() is { } routeAttribute)
                {
                    var routeValuesProperty = Expression.Property(HttpRequestExpr, nameof(HttpRequest.RouteValues));
                    paramterExpression = BindParamenter(routeValuesProperty, parameter, routeAttribute.Name);
                }
                else if (parameterCustomAttributes.OfType<IFromQueryMetadata>().FirstOrDefault() is { } queryAttribute)
                {
                    var queryProperty = Expression.Property(HttpRequestExpr, nameof(HttpRequest.Query));
                    paramterExpression = BindParamenter(queryProperty, parameter, queryAttribute.Name);
                }
                else if (parameterCustomAttributes.OfType<IFromHeaderMetadata>().FirstOrDefault() is { } headerAttribute)
                {
                    var headersProperty = Expression.Property(HttpRequestExpr, nameof(HttpRequest.Headers));
                    paramterExpression = BindParamenter(headersProperty, parameter, headerAttribute.Name);
                }
                else if (parameterCustomAttributes.OfType<IFromBodyMetadata>().FirstOrDefault() is { } bodyAttribute)
                {
                    if (consumeBodyDirectly)
                    {
                        throw new InvalidOperationException("Action cannot have more than one FromBody attribute.");
                    }

                    if (consumeBodyAsForm)
                    {
                        ThrowCannotReadBodyDirectlyAndAsForm();
                    }

                    consumeBodyDirectly = true;
                    allowEmptyBody = bodyAttribute.AllowEmpty;
                    bodyType = parameter.ParameterType;
                    paramterExpression = Expression.Convert(DeserializedBodyArg, bodyType);
                }
                else if (parameterCustomAttributes.OfType<IFromFormMetadata>().FirstOrDefault() is { } formAttribute)
                {
                    if (consumeBodyDirectly)
                    {
                        ThrowCannotReadBodyDirectlyAndAsForm();
                    }

                    consumeBodyAsForm = true;

                    var formProperty = Expression.Property(HttpRequestExpr, nameof(HttpRequest.Form));
                    paramterExpression = BindParamenter(formProperty, parameter, parameter.Name);
                }
                else if (parameter.CustomAttributes.Any(a => typeof(IFromServiceMetadata).IsAssignableFrom(a.AttributeType)))
                {
                    paramterExpression = Expression.Call(GetRequiredServiceMethodInfo.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
                }
                else if (parameter.ParameterType == typeof(IFormCollection))
                {
                    if (consumeBodyDirectly)
                    {
                        ThrowCannotReadBodyDirectlyAndAsForm();
                    }

                    consumeBodyAsForm = true;

                    paramterExpression = Expression.Property(HttpRequestExpr, nameof(HttpRequest.Form));
                }
                else if (parameter.ParameterType == typeof(HttpContext))
                {
                    paramterExpression = HttpContextParameter;
                }
                else if (parameter.ParameterType == typeof(CancellationToken))
                {
                    paramterExpression = RequestAbortedExpr;
                }

                args.Add(paramterExpression);
            }

            Expression? body = null;

            MethodCallExpression methodCall;

            if (targetExpression is null)
            {
                methodCall = Expression.Call(methodInfo, args);
            }
            else
            {
                methodCall = Expression.Call(targetExpression, methodInfo, args);
            }

            // Exact request delegate match
            if (methodInfo.ReturnType == typeof(void))
            {
                var bodyExpressions = new List<Expression>
                {
                    methodCall,
                    Expression.Property(null, (PropertyInfo)CompletedTaskMemberInfo)
                };

                body = Expression.Block(bodyExpressions);
            }
            else if (AwaitableInfo.IsTypeAwaitable(methodInfo.ReturnType, out var info))
            {
                if (methodInfo.ReturnType == typeof(Task))
                {
                    body = methodCall;
                }
                else if (methodInfo.ReturnType == typeof(ValueTask))
                {
                    body = Expression.Call(
                                        ExecuteValueTaskMethodInfo,
                                        methodCall);
                }
                else if (methodInfo.ReturnType.IsGenericType &&
                         methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var typeArg = methodInfo.ReturnType.GetGenericArguments()[0];

                    if (typeof(IResult).IsAssignableFrom(typeArg))
                    {
                        body = Expression.Call(
                                           ExecuteTaskResultOfTMethodInfo.MakeGenericMethod(typeArg),
                                           methodCall,
                                           HttpContextParameter);
                    }
                    else
                    {
                        // ExecuteTask<T>(action(..), httpContext);
                        if (typeArg == typeof(string))
                        {
                            body = Expression.Call(
                                             ExecuteTaskOfStringMethodInfo,
                                             methodCall,
                                             HttpContextParameter);
                        }
                        else
                        {
                            body = Expression.Call(
                                             ExecuteTaskOfTMethodInfo.MakeGenericMethod(typeArg),
                                             methodCall,
                                             HttpContextParameter);
                        }
                    }
                }
                else if (methodInfo.ReturnType.IsGenericType &&
                         methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                {
                    var typeArg = methodInfo.ReturnType.GetGenericArguments()[0];

                    if (typeof(IResult).IsAssignableFrom(typeArg))
                    {
                        body = Expression.Call(
                                           ExecuteValueResultTaskOfTMethodInfo.MakeGenericMethod(typeArg),
                                           methodCall,
                                           HttpContextParameter);
                    }
                    else
                    {
                        // ExecuteTask<T>(action(..), httpContext);
                        if (typeArg == typeof(string))
                        {
                            body = Expression.Call(
                                       ExecuteValueTaskOfStringMethodInfo,
                                       methodCall,
                                       HttpContextParameter);
                        }
                        else
                        {
                            body = Expression.Call(
                                       ExecuteValueTaskOfTMethodInfo.MakeGenericMethod(typeArg),
                                       methodCall,
                                       HttpContextParameter);
                        }
                    }
                }
                else
                {
                    // TODO: Handle custom awaitables
                    throw new NotSupportedException($"Unsupported return type: {methodInfo.ReturnType}");
                }
            }
            else if (typeof(IResult).IsAssignableFrom(methodInfo.ReturnType))
            {
                body = Expression.Call(methodCall, ResultWriteResponseAsync, HttpContextParameter);
            }
            else if (methodInfo.ReturnType == typeof(string))
            {
                body = Expression.Call(StringResultWriteResponseAsync, HttpResponseExpr, methodCall, Expression.Constant(CancellationToken.None));
            }
            else if (methodInfo.ReturnType.IsValueType)
            {
                var box = Expression.TypeAs(methodCall, typeof(object));
                body = Expression.Call(JsonResultWriteResponseAsync, HttpResponseExpr, box, Expression.Constant(CancellationToken.None));
            }
            else
            {
                body = Expression.Call(JsonResultWriteResponseAsync, HttpResponseExpr, methodCall, Expression.Constant(CancellationToken.None));
            }

            Func<object?, HttpContext, Task>? requestDelegate = null;

            if (consumeBodyDirectly)
            {
                // We need to generate the code for reading from the body before calling into the delegate
                var lambda = Expression.Lambda<Func<object?, HttpContext, object?, Task>>(body, TargetArg, HttpContextParameter, DeserializedBodyArg);
                var invoker = lambda.Compile();
                object? defaultBodyValue = null;

                if (allowEmptyBody && bodyType!.IsValueType)
                {
                    defaultBodyValue = Activator.CreateInstance(bodyType);
                }

                requestDelegate = async (target, httpContext) =>
                {
                    object? bodyValue;

                    if (allowEmptyBody && httpContext.Request.ContentLength == 0)
                    {
                        bodyValue = defaultBodyValue;
                    }
                    else
                    {
                        try
                        {
                            bodyValue = await httpContext.Request.ReadFromJsonAsync(bodyType!);
                        }
                        catch (IOException ex)
                        {
                            Log.RequestBodyIOException(GetLogger(httpContext), ex);
                            httpContext.Abort();
                            return;
                        }
                        catch (InvalidDataException ex)
                        {
                            Log.RequestBodyInvalidDataException(GetLogger(httpContext), ex);
                            httpContext.Response.StatusCode = 400;
                            return;
                        }
                    }

                    await invoker(target, httpContext, bodyValue);
                };
            }
            else if (consumeBodyAsForm)
            {
                var lambda = Expression.Lambda<Func<object?, HttpContext, Task>>(body, TargetArg, HttpContextParameter);
                var invoker = lambda.Compile();

                requestDelegate = async (target, httpContext) =>
                {
                    // Generating async code would just be insane so if the method needs the form populate it here
                    // so the within the method it's cached
                    try
                    {
                        await httpContext.Request.ReadFormAsync();
                    }
                    catch (IOException ex)
                    {
                        Log.RequestBodyIOException(GetLogger(httpContext), ex);
                        httpContext.Abort();
                        return;
                    }
                    catch (InvalidDataException ex)
                    {
                        Log.RequestBodyInvalidDataException(GetLogger(httpContext), ex);
                        httpContext.Response.StatusCode = 400;
                        return;
                    }

                    await invoker(target, httpContext);
                };
            }
            else
            {
                var lambda = Expression.Lambda<Func<object?, HttpContext, Task>>(body, TargetArg, HttpContextParameter);
                var invoker = lambda.Compile();

                requestDelegate = invoker;
            }

            return requestDelegate;
        }

        private static ILogger GetLogger(HttpContext httpContext)
        {
            var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            return loggerFactory.CreateLogger("Microsoft.AspNetCore.Routing.MapAction");
        }

        private static Expression BindParamenter(Expression sourceExpression, ParameterInfo parameter, string? name)
        {
            var key = name ?? parameter.Name;
            var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
            var valueArg = Expression.Convert(
                                Expression.MakeIndex(sourceExpression,
                                                     sourceExpression.Type.GetProperty("Item"),
                                                     new[] { Expression.Constant(key) }),
                                typeof(string));

            MethodInfo parseMethod = (from m in type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                      let parameters = m.GetParameters()
                                      where m.Name == "Parse" && parameters.Length == 1 && parameters[0].ParameterType == typeof(string)
                                      select m).FirstOrDefault()!;

            Expression? expr = null;

            if (parseMethod != null)
            {
                expr = Expression.Call(parseMethod, valueArg);
            }
            else if (parameter.ParameterType != valueArg.Type)
            {
                // Convert.ChangeType()
                expr = Expression.Call(ChangeTypeMethodInfo, valueArg, Expression.Constant(type));
            }
            else
            {
                expr = valueArg;
            }

            if (expr.Type != parameter.ParameterType)
            {
                expr = Expression.Convert(expr, parameter.ParameterType);
            }

            Expression defaultExpression;
            if (parameter.HasDefaultValue)
            {
                defaultExpression = Expression.Constant(parameter.DefaultValue);
            }
            else
            {
                defaultExpression = Expression.Default(parameter.ParameterType);
            }

            // property[key] == null ? default : (ParameterType){Type}.Parse(property[key]);
            expr = Expression.Condition(
                Expression.Equal(valueArg, Expression.Constant(null)),
                defaultExpression,
                expr);

            return expr;
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

        private static Task ExecuteTask<T>(Task<T> task, HttpContext httpContext)
        {
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

        private static Task ExecuteTaskOfString(Task<string> task, HttpContext httpContext)
        {
            static async Task ExecuteAwaited(Task<string> task, HttpContext httpContext)
            {
                await httpContext.Response.WriteAsync(await task);
            }

            if (task.IsCompletedSuccessfully)
            {
                return httpContext.Response.WriteAsync(task.GetAwaiter().GetResult());
            }

            return ExecuteAwaited(task, httpContext);
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

        private static Task ExecuteValueTaskOfString(ValueTask<string> task, HttpContext httpContext)
        {
            static async Task ExecuteAwaited(ValueTask<string> task, HttpContext httpContext)
            {
                await httpContext.Response.WriteAsync(await task);
            }

            if (task.IsCompletedSuccessfully)
            {
                return httpContext.Response.WriteAsync(task.GetAwaiter().GetResult());
            }

            return ExecuteAwaited(task, httpContext);
        }

        private static Task ExecuteValueTaskResult<T>(ValueTask<T> task, HttpContext httpContext) where T : IResult
        {
            static async Task ExecuteAwaited(ValueTask<T> task, HttpContext httpContext)
            {
                await (await task).ExecuteAsync(httpContext);
            }

            if (task.IsCompletedSuccessfully)
            {
                return task.GetAwaiter().GetResult().ExecuteAsync(httpContext);
            }

            return ExecuteAwaited(task, httpContext);
        }

        private static async Task ExecuteTaskResult<T>(Task<T> task, HttpContext httpContext) where T : IResult
        {
            await (await task).ExecuteAsync(httpContext);
        }

        [StackTraceHidden]
        private static void ThrowCannotReadBodyDirectlyAndAsForm()
        {
            throw new InvalidOperationException("Action cannot mix FromBody and FromForm on the same method.");
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _requestBodyIOException = LoggerMessage.Define(
                LogLevel.Debug,
                new EventId(1, "RequestBodyIOException"),
                "Reading the request body failed with an IOException.");

            private static readonly Action<ILogger, Exception> _requestBodyInvalidDataException = LoggerMessage.Define(
                LogLevel.Debug,
                new EventId(2, "RequestBodyInvalidDataException"),
                "Reading the request body failed with an InvalidDataException.");

            public static void RequestBodyIOException(ILogger logger, IOException exception)
            {
                _requestBodyIOException(logger, exception);
            }

            public static void RequestBodyInvalidDataException(ILogger logger, InvalidDataException exception)
            {
                _requestBodyInvalidDataException(logger, exception);
            }
        }
    }
}
