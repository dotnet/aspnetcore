// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection
{
    /// <summary>
    /// A class that represents nullability info.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class NullabilityInfo
    {
        internal NullabilityInfo(Type type, NullabilityState readState, NullabilityState writeState,
            NullabilityInfo? elementType, NullabilityInfo[] typeArguments)
        {
            Type = type;
            ReadState = readState;
            WriteState = writeState;
            ElementType = elementType;
            GenericTypeArguments = typeArguments;
        }

        /// <summary>
        /// The <see cref="System.Type" /> of the member or generic parameter
        /// to which this NullabilityInfo belongs.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The nullability read state of the member.
        /// </summary>
        public NullabilityState ReadState { get; internal set; }

        /// <summary>
        /// The nullability write state of the member.
        /// </summary>
        public NullabilityState WriteState { get; internal set; }

        /// <summary>
        /// If the member type is an array, gives the <see cref="NullabilityInfo" /> of the elements of the array, null otherwise.
        /// </summary>
        public NullabilityInfo? ElementType { get; }

        /// <summary>
        /// If the member type is a generic type, gives the array of <see cref="NullabilityInfo" /> for each type parameter.
        /// </summary>
        public NullabilityInfo[] GenericTypeArguments { get; }
    }

    /// <summary>
    /// An enum that represents nullability state.
    /// </summary>
    internal enum NullabilityState
    {
        /// <summary>
        /// Nullability context not enabled (oblivious)
        /// </summary>
        Unknown,

        /// <summary>
        /// Non nullable value or reference type
        /// </summary>
        NotNull,

        /// <summary>
        /// Nullable value or reference type
        /// </summary>
        Nullable,
    }
}
#endif