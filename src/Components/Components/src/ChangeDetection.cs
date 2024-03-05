// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

internal sealed class ChangeDetection
{
    public static bool MayHaveChanged<T1, T2>(T1 oldValue, T2 newValue)
    {
        var oldIsNotNull = oldValue != null;
        var newIsNotNull = newValue != null;
        if (oldIsNotNull != newIsNotNull)
        {
            return true; // One's null and the other isn't, so different
        }
        else if (oldIsNotNull) // i.e., both are not null (considering previous check)
        {
            var oldValueType = oldValue!.GetType();
            var newValueType = newValue!.GetType();
            if (oldValueType != newValueType            // Definitely different
                || !IsKnownImmutableType(oldValueType)  // Maybe different
                || !oldValue.Equals(newValue))          // Somebody says they are different
            {
                return true;
            }
        }

        // By now we know either both are null, or they are the same immutable type
        // and ThatType::Equals says the two values are equal.
        return false;
    }

    // The contents of this list need to trade off false negatives against computation
    // time. So we don't want a huge list of types to check (or would have to move to
    // a hashtable lookup, which is differently expensive). It's better not to include
    // uncommon types here even if they are known to be immutable.
    // This logic assumes that no new System.TypeCode enum entries have been declared since 7.0, or at least that any new ones
    // represent immutable types. If System.TypeCode changes, review this logic to ensure it is still correct.
    // Supported immutable types : bool, byte, sbyte, short, ushort, int, uint, long, ulong, char, double,
    //                             string, DateTime, decimal, Guid, Enum, EventCallback, EventCallback<>.
    // For performance reasons, the following immutable types are not supported: IntPtr, UIntPtr, Type.
    private static bool IsKnownImmutableType(Type type)
        => Type.GetTypeCode(type) != TypeCode.Object
        || type == typeof(Guid)
        || typeof(IEventCallback).IsAssignableFrom(type);
}
