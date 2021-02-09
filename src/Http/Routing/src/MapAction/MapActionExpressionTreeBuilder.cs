// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Routing.MapAction
{
    internal class MapActionExpressionTreeBuilder
    {
        private static readonly MethodInfo ChangeTypeMethodInfo = GetMethodInfo<Func<object, Type, object>>((value, type) => Convert.ChangeType(value, type, CultureInfo.InvariantCulture));
        private static readonly MethodInfo ExecuteTaskOfTMethodInfo = typeof(MapActionExpressionTreeBuilder).GetMethod(nameof(ExecuteTask), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteValueTaskOfTMethodInfo = typeof(MapActionExpressionTreeBuilder).GetMethod(nameof(ExecuteValueTask), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteTaskResultOfTMethodInfo = typeof(MapActionExpressionTreeBuilder).GetMethod(nameof(ExecuteTaskResult), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo ExecuteValueResultTaskOfTMethodInfo = typeof(MapActionExpressionTreeBuilder).GetMethod(nameof(ExecuteValueTaskResult), BindingFlags.NonPublic | BindingFlags.Static)!;
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

        public static RequestDelegate BuildRequestDelegate(Delegate action)
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

            var method = action.Method;

            var needForm = false;
            var needBody = false;
            Type? bodyType = null;

            // This argument represents the deserialized body returned from IHttpRequestReader
            // when the method has a FromBody attribute declared

            var args = new List<Expression>();

            foreach (var parameter in method.GetParameters())
            {
                Expression paramterExpression = Expression.Default(parameter.ParameterType);

                //if (parameter.FromQuery != null)
                //{
                //    var queryProperty = Expression.Property(httpRequestExpr, nameof(HttpRequest.Query));
                //    paramterExpression = BindParamenter(queryProperty, parameter, parameter.FromQuery);
                //}
                //else if (parameter.FromHeader != null)
                //{
                //    var headersProperty = Expression.Property(httpRequestExpr, nameof(HttpRequest.Headers));
                //    paramterExpression = BindParamenter(headersProperty, parameter, parameter.FromHeader);
                //}
                //else if (parameter.FromRoute != null)
                //{
                //    var routeValuesProperty = Expression.Property(httpRequestExpr, nameof(HttpRequest.RouteValues));
                //    paramterExpression = BindParamenter(routeValuesProperty, parameter, parameter.FromRoute);
                //}
                //else if (parameter.FromCookie != null)
                //{
                //    var cookiesProperty = Expression.Property(httpRequestExpr, nameof(HttpRequest.Cookies));
                //    paramterExpression = BindParamenter(cookiesProperty, parameter, parameter.FromCookie);
                //}
                //else if (parameter.FromServices)
                //{
                //    paramterExpression = Expression.Call(GetRequiredServiceMethodInfo.MakeGenericMethod(parameter.ParameterType), requestServicesExpr);
                //}
                //else if (parameter.FromForm != null)
                //{
                //    needForm = true;

                //    var formProperty = Expression.Property(httpRequestExpr, nameof(HttpRequest.Form));
                //    paramterExpression = BindParamenter(formProperty, parameter, parameter.FromForm);
                //}
                //else if (parameter.FromBody)
                if (parameter.CustomAttributes.Any(a => typeof(IFromBodyMetadata).IsAssignableFrom(a.AttributeType)))
                {
                    if (needBody)
                    {
                        throw new InvalidOperationException("Action cannot have more than one FromBody attribute.");
                    }

                    if (needForm)
                    {
                        throw new InvalidOperationException("Action cannot mix FromBody and FromForm on the same method.");
                    }

                    needBody = true;
                    bodyType = parameter.ParameterType;
                    paramterExpression = Expression.Convert(DeserializedBodyArg, bodyType);
                }
                else
                {
                    if (parameter.ParameterType == typeof(IFormCollection))
                    {
                        needForm = true;

                        paramterExpression = Expression.Property(HttpRequestExpr, nameof(HttpRequest.Form));
                    }
                    else if (parameter.ParameterType == typeof(HttpContext))
                    {
                        paramterExpression = HttpContextParameter;
                    }
                }

                args.Add(paramterExpression);
            }

            Expression? body = null;

            MethodCallExpression methodCall;

            if (action.Target is null)
            {
                methodCall = Expression.Call(method, args);
            }
            else
            {
                var castedTarget = Expression.Convert(TargetArg, action.Target.GetType());
                methodCall = Expression.Call(castedTarget, method, args);
            }

            // Exact request delegate match
            if (method.ReturnType == typeof(void))
            {
                var bodyExpressions = new List<Expression>
                {
                    methodCall,
                    Expression.Property(null, (PropertyInfo)CompletedTaskMemberInfo)
                };

                body = Expression.Block(bodyExpressions);
            }
            else if (AwaitableInfo.IsTypeAwaitable(method.ReturnType, out var info))
            {
                if (method.ReturnType == typeof(Task))
                {
                    body = methodCall;
                }
                else if (method.ReturnType.IsGenericType &&
                         method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var typeArg = method.ReturnType.GetGenericArguments()[0];

                    if (typeof(IResult).IsAssignableFrom(typeArg))
                    {
                        body = Expression.Call(
                                           ExecuteTaskResultOfTMethodInfo.MakeGenericMethod(typeArg),
                                           methodCall,
                                           TargetArg,
                                           HttpContextParameter);
                    }
                    else
                    {
                        // ExecuteTask<T>(action(..), httpContext);
                        body = Expression.Call(
                                           ExecuteTaskOfTMethodInfo.MakeGenericMethod(typeArg),
                                           methodCall,
                                           TargetArg,
                                           HttpContextParameter);
                    }
                }
                else if (method.ReturnType.IsGenericType &&
                         method.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                {
                    var typeArg = method.ReturnType.GetGenericArguments()[0];

                    if (typeof(IResult).IsAssignableFrom(typeArg))
                    {
                        body = Expression.Call(
                                           ExecuteValueResultTaskOfTMethodInfo.MakeGenericMethod(typeArg),
                                           methodCall,
                                           TargetArg,
                                           HttpContextParameter);
                    }
                    else
                    {
                        // ExecuteTask<T>(action(..), httpContext);
                        body = Expression.Call(
                                       ExecuteValueTaskOfTMethodInfo.MakeGenericMethod(typeArg),
                                       methodCall,
                                       TargetArg,
                                       HttpContextParameter);
                    }
                }
                else
                {
                    // TODO: Handle custom awaitables
                    throw new NotSupportedException($"Unsupported return type: {method.ReturnType}");
                }
            }
            else if (typeof(IResult).IsAssignableFrom(method.ReturnType))
            {
                body = Expression.Call(methodCall, ResultWriteResponseAsync, HttpContextParameter);
            }
            else if (method.ReturnType == typeof(string))
            {
                body = Expression.Call(StringResultWriteResponseAsync, HttpResponseExpr, methodCall, Expression.Constant(CancellationToken.None));
            }
            else
            {
                body = Expression.Call(JsonResultWriteResponseAsync, HttpResponseExpr, methodCall, Expression.Constant(CancellationToken.None));
            }

            Func<object?, HttpContext, Task>? requestDelegate = null;

            if (needBody)
            {
                // We need to generate the code for reading from the body before calling into the 
                // delegate
                var lambda = Expression.Lambda<Func<object?, HttpContext, object?, Task>>(body, TargetArg, HttpContextParameter, DeserializedBodyArg);
                var invoker = lambda.Compile();

                requestDelegate = async (target, httpContext) =>
                {
                    var bodyValue = await httpContext.Request.ReadFromJsonAsync(bodyType!);

                    await invoker(target, httpContext, bodyValue);
                };
            }
            else if (needForm)
            {
                var lambda = Expression.Lambda<Func<object?, HttpContext, Task>>(body, TargetArg, HttpContextParameter);
                var invoker = lambda.Compile();

                requestDelegate = async (target, httpContext) =>
                {
                    // Generating async code would just be insane so if the method needs the form populate it here
                    // so the within the method it's cached
                    await httpContext.Request.ReadFormAsync();

                    await invoker(target, httpContext);
                };
            }
            else
            {
                var lambda = Expression.Lambda<Func<object?, HttpContext, Task>>(body, TargetArg, HttpContextParameter);
                var invoker = lambda.Compile();

                requestDelegate = invoker;
            }

            return httpContext =>
            {
                return requestDelegate(action.Target, httpContext);
            };
        }

        private static Expression BindParamenter(Expression sourceExpression, ParameterInfo parameter, string name)
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

            // property[key] == null ? default : (ParameterType){Type}.Parse(property[key]);
            expr = Expression.Condition(
                Expression.Equal(valueArg, Expression.Constant(null)),
                Expression.Default(parameter.ParameterType),
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

        private static async ValueTask ExecuteTask<T>(Task<T> task, HttpContext httpContext)
        {
            await new JsonResult(await task).ExecuteAsync(httpContext);
        }

        private static Task ExecuteValueTask<T>(ValueTask<T> task, HttpContext httpContext)
        {
            static async Task ExecuteAwaited(ValueTask<T> task, HttpContext httpContext)
            {
                await new JsonResult(await task).ExecuteAsync(httpContext);
            }

            if (task.IsCompletedSuccessfully)
            {
                return new JsonResult(task.GetAwaiter().GetResult()).ExecuteAsync(httpContext);
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

        private static async ValueTask ExecuteTaskResult<T>(Task<T> task, HttpContext httpContext) where T : IResult
        {
            await (await task).ExecuteAsync(httpContext);
        }

        /// <summary>
        /// Equivalent to the IResult part of Microsoft.AspNetCore.Mvc.JsonResult
        /// </summary>
        private class JsonResult : IResult
        {
            public object? Value { get; }

            public JsonResult(object? value)
            {
                Value = value;
            }

            public Task ExecuteAsync(HttpContext httpContext)
            {
                return httpContext.Response.WriteAsJsonAsync(Value);
            }
        }
    }
}
