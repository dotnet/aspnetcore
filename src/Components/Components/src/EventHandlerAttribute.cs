// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Associates an event argument type with an event attribute name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class EventHandlerAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of <see cref="EventHandlerAttribute"/>.
        /// </summary>
        /// <param name="attributeName"></param>
        /// <param name="eventArgsType"></param>
        public EventHandlerAttribute(string attributeName, Type eventArgsType)
        {
            if (attributeName == null)
            {
                throw new ArgumentNullException(nameof(attributeName));
            }

            if (eventArgsType == null)
            {
                throw new ArgumentNullException(nameof(eventArgsType));
            }

            AttributeName = attributeName;
            EventArgsType = eventArgsType;
        }

        /// <summary>
        /// Gets the attribute name.
        /// </summary>
        public string AttributeName { get; }

        /// <summary>
        /// Gets the event argument type.
        /// </summary>
        public Type EventArgsType { get; }
    }
}
