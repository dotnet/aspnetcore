// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal class HubMethodDescriptor
    {
        private static readonly MethodInfo GetAsyncEnumeratorMethod = typeof(AsyncEnumeratorAdapters)
            .GetRuntimeMethods()
            .Single(m => m.Name.Equals(nameof(AsyncEnumeratorAdapters.GetAsyncEnumerator)) && m.IsGenericMethod);

        public HubMethodDescriptor(ObjectMethodExecutor methodExecutor, IEnumerable<IAuthorizeData> policies)
        {
            MethodExecutor = methodExecutor;
            ParameterTypes = methodExecutor.MethodParameters.Select(p => p.ParameterType).ToArray();
            Policies = policies.ToArray();

            NonAsyncReturnType = (MethodExecutor.IsMethodAsync)
                ? MethodExecutor.AsyncResultType
                : MethodExecutor.MethodReturnType;

            if (IsChannelType(NonAsyncReturnType, out var channelItemType))
            {
                IsChannel = true;
                StreamReturnType = channelItemType;
            }
        }

        private Func<object, CancellationToken, IAsyncEnumerator<object>> _convertToEnumerator;

        public ObjectMethodExecutor MethodExecutor { get; }

        public IReadOnlyList<Type> ParameterTypes { get; }

        public Type NonAsyncReturnType { get; }

        public bool IsChannel { get; }

        public bool IsStreamable => IsChannel;

        public Type StreamReturnType { get; }

        public IList<IAuthorizeData> Policies { get; }

        private static bool IsChannelType(Type type, out Type payloadType)
        {
            var channelType = type.AllBaseTypes().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ChannelReader<>));
            if (channelType == null)
            {
                payloadType = null;
                return false;
            }

            payloadType = channelType.GetGenericArguments()[0];
            return true;
        }

        public IAsyncEnumerator<object> FromChannel(object channel, CancellationToken cancellationToken)
        {
            // there is the potential for compile to be called times but this has no harmful effect other than perf
            if (_convertToEnumerator == null)
            {
                _convertToEnumerator = CompileConvertToEnumerator(GetAsyncEnumeratorMethod, StreamReturnType);
            }

            return _convertToEnumerator.Invoke(channel, cancellationToken);
        }

        private static Func<object, CancellationToken, IAsyncEnumerator<object>> CompileConvertToEnumerator(MethodInfo adapterMethodInfo, Type streamReturnType)
        {
            // This will call one of two adapter methods to wrap the passed in streamable value
            // and cancellation token to an IAsyncEnumerator<object>
            // ChannelReader<T>
            // AsyncEnumeratorAdapters.GetAsyncEnumerator<T>(channelReader, cancellationToken);

            var genericMethodInfo = adapterMethodInfo.MakeGenericMethod(streamReturnType);

            var methodParameters = genericMethodInfo.GetParameters();

            // arg1 and arg2 are the parameter names on Func<T1, T2, TReturn>
            // we reference these values and then use them to call adaptor method
            var targetParameter = Expression.Parameter(typeof(object), "arg1");
            var parametersParameter = Expression.Parameter(typeof(CancellationToken), "arg2");

            var parameters = new List<Expression>
            {
                Expression.Convert(targetParameter, methodParameters[0].ParameterType),
                parametersParameter
            };

            var methodCall = Expression.Call(null, genericMethodInfo, parameters);

            var castMethodCall = Expression.Convert(methodCall, typeof(IAsyncEnumerator<object>));

            var lambda = Expression.Lambda<Func<object, CancellationToken, IAsyncEnumerator<object>>>(castMethodCall, targetParameter, parametersParameter);
            return lambda.Compile();
        }
    }
}