// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.Internal
{
    /// <summary>
    /// Helper related to generic interface definitions and implementing classes.
    /// </summary>
    internal static class ClosedGenericMatcher
    {
        /// <summary>
        /// Determine whether <paramref name="queryType"/> is or implements a closed generic <see cref="Type"/>
        /// created from <paramref name="interfaceType"/>.
        /// </summary>
        /// <param name="queryType">The <see cref="Type"/> of interest.</param>
        /// <param name="interfaceType">The open generic <see cref="Type"/> to match. Usually an interface.</param>
        /// <returns>
        /// The closed generic <see cref="Type"/> created from <paramref name="interfaceType"/> that
        /// <paramref name="queryType"/> is or implements. <c>null</c> if the two <see cref="Type"/>s have no such
        /// relationship.
        /// </returns>
        /// <remarks>
        /// This method will return <paramref name="queryType"/> if <paramref name="interfaceType"/> is
        /// <c>typeof(KeyValuePair{,})</c>, and <paramref name="queryType"/> is
        /// <c>typeof(KeyValuePair{string, object})</c>.
        /// </remarks>
        public static Type ExtractGenericInterface(Type queryType, Type interfaceType)
        {
            if (queryType == null)
            {
                throw new ArgumentNullException(nameof(queryType));
            }

            if (interfaceType == null)
            {
                throw new ArgumentNullException(nameof(interfaceType));
            }

            Func<Type, bool> matchesInterface =
                type => type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == interfaceType;
            if (matchesInterface(queryType))
            {
                // Checked type matches (i.e. is a closed generic type created from) the open generic type.
                return queryType;
            }

            // Otherwise check all interfaces the type implements for a match.
            return queryType.GetTypeInfo().ImplementedInterfaces.FirstOrDefault(matchesInterface);
        }
    }
}