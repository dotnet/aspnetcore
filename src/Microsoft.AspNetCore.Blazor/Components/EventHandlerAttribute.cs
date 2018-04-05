// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Blazor.Components
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class EventHandlerAttribute : Attribute
    {
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

        public string AttributeName { get; }

        public Type EventArgsType { get; }
    }
}
