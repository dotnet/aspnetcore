// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    internal static class EventHandlerDelegateWithClosuresComparer
    {
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075",
            Justification = "We expect application code is configured to ensure that methods invokable from Javascript are retained.")]
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

            // if the types are not the same, or the targets are not compiler generated,
            // this is not a closure and we can assume that the delegates are different.
            if (oldTargetType != newTargetType ||
                left.Method != right.Method ||
                !oldTargetType.CustomAttributes.Any(x => x.AttributeType == typeof(CompilerGeneratedAttribute))
                )
            {
                return false;
            }

            // We have two instances of the same compiler generated class. Let's compare all public fields.
            foreach (var field in oldTargetType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                var leftValue = field.GetValue(left.Target);
                var rightValue = field.GetValue(right.Target);

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
        }
    }
}
