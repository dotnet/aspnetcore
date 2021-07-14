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
    internal static class AttributeComparerForDiffing
    {
        private static readonly ConcurrentDictionary<Type, Func<object, object, bool>> ClosureComparerCache = new();
        private static readonly Func<object, object, bool> AlwaysFalseDelegate = (objA, objB) => false;

        internal static bool IsEquivalentForDiffing(object? oldValue, object? newValue)
        {
            // If the values are delegate types, we'll inspect them to see if they point to closures.
            if (oldValue is EventCallback leftEventCallback && newValue is EventCallback rightEventCallBack)
            {
                return EventCallback.IsEquivalentForDiffing(ref leftEventCallback, ref rightEventCallBack);
            }
            else if (oldValue is MulticastDelegate leftDelegate && newValue is MulticastDelegate rightDelegate)
            {
                return IsEquivalentForDiffing(leftDelegate, rightDelegate);
            }
            else
            {
                return Equals(oldValue, newValue);
            }
        }

        internal static bool IsEquivalentForDiffing(MulticastDelegate left, MulticastDelegate right)
        {
            if (left.Equals(right))
            {
                return true;
            }

            // If any of the delegates or their targets are null (static delegates) then normal
            // equality is sufficient. No additional testing required.
            if (left.Target == null || right.Target == null)
            {
                return false;
            }

            var oldTargetType = left.Target!.GetType();
            var newTargetType = right.Target!.GetType();

            // if the types are not the same, or the methods are not the same, these possible closures are
            // not the same anyway. Types are implicitly compared as they are part of the MethodInfo comparison. 
            if (left.Method != right.Method)
            {
                return false;
            }

            // for speed we cache the comparison functions per closure type.
            var comparison = ClosureComparerCache.GetOrAdd(newTargetType, GetClosureComparerForType);
            return comparison(left.Target, right.Target);
        }

        private static Func<object, object, bool> GetClosureComparerForType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type type)
        {
            // for compiler-generated types, we're willing to assume that it's a closure, and for closures where all the fields are equal,
            // we consider the object reference identity to be inconsequential. Whereas for non-compiler-generated types, we can't make
            // the assumption that the object reference identity doesn't matter, so the preceding tests about object reference identity
            // are all we care about in that case.
            if (!type.CustomAttributes.Any(x => x.AttributeType == typeof(CompilerGeneratedAttribute)))
            {
                return AlwaysFalseDelegate;
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
                    if (!IsEquivalentForDiffing(leftValue, rightValue))
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
