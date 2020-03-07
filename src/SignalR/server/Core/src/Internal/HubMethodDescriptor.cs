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
        private static readonly MethodInfo MakeCancelableAsyncEnumerableMethod = typeof(AsyncEnumerableAdapters)
            .GetRuntimeMethods()
            .Single(m => m.Name.Equals(nameof(AsyncEnumerableAdapters.MakeCancelableAsyncEnumerable)) && m.IsGenericMethod);

        private static readonly MethodInfo MakeAsyncEnumerableFromChannelMethod = typeof(AsyncEnumerableAdapters)
            .GetRuntimeMethods()
            .Single(m => m.Name.Equals(nameof(AsyncEnumerableAdapters.MakeAsyncEnumerableFromChannel)) && m.IsGenericMethod);

        private readonly MethodInfo _makeCancelableEnumerableMethodInfo;
        private Func<object, CancellationToken, IAsyncEnumerable<object>> _makeCancelableEnumerable;

        public HubMethodDescriptor(ObjectMethodExecutor methodExecutor, IEnumerable<IAuthorizeData> policies)
        {
            MethodExecutor = methodExecutor;

            NonAsyncReturnType = (MethodExecutor.IsMethodAsync)
                ? MethodExecutor.AsyncResultType
                : MethodExecutor.MethodReturnType;

            foreach (var returnType in NonAsyncReturnType.GetInterfaces().Concat(NonAsyncReturnType.AllBaseTypes()))
            {
                if (!returnType.IsGenericType)
                {
                    continue;
                }

                var openReturnType = returnType.GetGenericTypeDefinition();

                if (openReturnType == typeof(IAsyncEnumerable<>))
                {
                    StreamReturnType = returnType.GetGenericArguments()[0];
                    _makeCancelableEnumerableMethodInfo = MakeCancelableAsyncEnumerableMethod;
                    break;
                }

                if (openReturnType == typeof(ChannelReader<>))
                {
                    StreamReturnType = returnType.GetGenericArguments()[0];
                    _makeCancelableEnumerableMethodInfo = MakeAsyncEnumerableFromChannelMethod;
                    break;
                }
            }

            // Take out synthetic arguments that will be provided by the server, this list will be given to the protocol parsers
            ParameterTypes = methodExecutor.MethodParameters.Where(p =>
            {
                // Only streams can take CancellationTokens currently
                if (IsStreamResponse && p.ParameterType == typeof(CancellationToken))
                {
                    HasSyntheticArguments = true;
                    return false;
                }
                else if (ReflectionHelper.IsStreamingType(p.ParameterType, mustBeDirectType: true))
                {
                    if (StreamingParameters == null)
                    {
                        StreamingParameters = new List<Type>();
                    }

                    StreamingParameters.Add(p.ParameterType.GetGenericArguments()[0]);
                    HasSyntheticArguments = true;
                    return false;
                }
                return true;
            }).Select(p => p.ParameterType).ToArray();

            if (HasSyntheticArguments)
            {
                OriginalParameterTypes = methodExecutor.MethodParameters.Select(p => p.ParameterType).ToArray();
            }

            Policies = policies.ToArray();
        }

        public List<Type> StreamingParameters { get; private set; }

        public ObjectMethodExecutor MethodExecutor { get; }

        public IReadOnlyList<Type> ParameterTypes { get; }

        public IReadOnlyList<Type> OriginalParameterTypes { get; }

        public Type NonAsyncReturnType { get; }

        public bool IsStreamResponse => StreamReturnType != null;

        public Type StreamReturnType { get; }

        public IList<IAuthorizeData> Policies { get; }

        public bool HasSyntheticArguments { get; private set; }

        public IAsyncEnumerable<object> FromReturnedStream(object stream, CancellationToken cancellationToken)
        {
            // there is the potential for compile to be called times but this has no harmful effect other than perf
            if (_makeCancelableEnumerable == null)
            {
                _makeCancelableEnumerable = CompileConvertToEnumerable(_makeCancelableEnumerableMethodInfo, StreamReturnType);
            }

            return _makeCancelableEnumerable.Invoke(stream, cancellationToken);
        }

        private static Func<object, CancellationToken, IAsyncEnumerable<object>> CompileConvertToEnumerable(MethodInfo adapterMethodInfo, Type streamReturnType)
        {
            // This will call one of two adapter methods to wrap the passed in streamable value into an IAsyncEnumerable<object>:
            // - AsyncEnumerableAdapters.MakeCancelableAsyncEnumerable<T>(asyncEnumerable, cancellationToken);
            // - AsyncEnumerableAdapters.MakeCancelableAsyncEnumerableFromChannel<T>(channelReader, cancellationToken);

            var parameters = new[]
            {
                Expression.Parameter(typeof(object)),
                Expression.Parameter(typeof(CancellationToken)),
            };

            var genericMethodInfo = adapterMethodInfo.MakeGenericMethod(streamReturnType);
            var methodParameters = genericMethodInfo.GetParameters();
            var methodArguments = new Expression[]
            {
                Expression.Convert(parameters[0], methodParameters[0].ParameterType),
                parameters[1],
            };

            var methodCall = Expression.Call(null, genericMethodInfo, methodArguments);
            var lambda = Expression.Lambda<Func<object, CancellationToken, IAsyncEnumerable<object>>>(methodCall, parameters);
            return lambda.Compile();
        }
    }
}
