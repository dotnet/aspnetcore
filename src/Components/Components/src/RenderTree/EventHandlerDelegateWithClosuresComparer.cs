// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Reflection;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    internal static class EventHandlerDelegateWithClosuresComparer
    {
        private static readonly ConcurrentDictionary<Type, Func<object, object, bool>> ClosureComparerCache = new();

        internal static bool EventCallBackEquals(ref EventCallback left, ref EventCallback right)
        {
            if (Equals(left, right))
            {
                return true;
            }

            // Whenever the receivers are different and they are explicit we assume that
            // the EventCallbacks are not equal.
            if (left.Receiver != right.Receiver &&
                (left.RequiresExplicitReceiver || right.RequiresExplicitReceiver))
            {
                return false;
            }

            return DelegateEquals(left.Delegate, right.Delegate);
        }

        internal static bool DelegateEquals(MulticastDelegate? left, MulticastDelegate? right)
        {
            if (Equals(left, right))
            {
                return true;
            }

            // If any of the delegates or their targets are null (static delegates) then normal
            // equality is sufficient. No additional testing required.
            if (left == null || right == null || left.Target == null || right.Target == null)
            {
                return false;
            }

            var oldTargetType = left.Target!.GetType();
            var newTargetType = right.Target!.GetType();

            // if the types are not the same, or the methods are not the same, these possible closures are
            // not the same anyway.
            if (oldTargetType != newTargetType || left.Method != right.Method)
            {
                return false;
            }

            // for speed we cache the comparison functions per closure type.
            var comparison = ClosureComparerCache.GetOrAdd(newTargetType, GetClosureComparerForType);
            return comparison(left.Target, right.Target);
        }

        private static Func<object, object, bool> GetClosureComparerForType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type type)
        {
            // if this type is not compiler generated, it is not a closure, and the previous tests where
            // enough to conclude that these are not equal. We just cache a delegate to a function that returns false.
            if (!type.CustomAttributes.Any(x => x.AttributeType == typeof(CompilerGeneratedAttribute)))
            {
                return (objA, objB) => false;
            }

            // build an array of lambdas that will get each public field value on the closure.
            var fieldGetters = type.GetFields(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly).Select(fieldInfo =>
            {
                var sourceParam = Expression.Parameter(typeof(object));
                Expression returnExpression = Expression.Field(Expression.Convert(sourceParam, fieldInfo.DeclaringType!), fieldInfo);
                if (fieldInfo.FieldType.IsValueType)
                {
                    // box if neccessary.
                    returnExpression = Expression.Convert(returnExpression, typeof(object));
                }
                var lambda = Expression.Lambda(returnExpression, sourceParam);
                return (Func<object?, object?>)lambda.Compile();
            }).ToArray();

            // the actual comparison function. It re-uses the array of public field 'getters' that we have built up
            // before.
            return (objA, objB) =>
            {
                // We have two instances of the same compiler generated class. Let's compare all public fields.
                foreach (var fieldGetter in fieldGetters)
                {
                    var leftValue = fieldGetter(objA);
                    var rightValue = fieldGetter(objB);

                    // We might have recursive callbacks or delegates.
                    // For example EventCallbackFactoryBinderExtensions.CreateBinderCore<T> wraps a delegate in a delegate
                    // so this is not hypothetical.
                    if (leftValue is EventCallback leftEventCallback && rightValue is EventCallback rightEventCallBack)
                    {
                        if (!EventCallBackEquals(ref leftEventCallback, ref rightEventCallBack))
                        {
                            return false;
                        }
                    }
                    else if (leftValue is MulticastDelegate leftDelegate && rightValue is MulticastDelegate rightDelegate)
                    {
                        if (!DelegateEquals(leftDelegate, rightDelegate))
                        {
                            return false;
                        }
                    }
                    else if (!Equals(leftValue, rightValue))
                    {
                        return false;
                    }
                }

                // All public fields are equal and the compiler generated types are the same. For our purpose
                // these delegates are the same, even though the instances are different.
                return true;
            };
        }
    }
}
