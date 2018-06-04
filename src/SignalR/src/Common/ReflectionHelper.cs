// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace Microsoft.AspNetCore.SignalR
{
    internal static class ReflectionHelper
    {
        public static bool IsStreamingType(Type type)
        {
            // IMPORTANT !!
            // All valid types must be generic
            // because HubConnectionContext gets the generic argument and uses it to determine the expected item type of the stream
            // The long-term solution is making a (streaming type => expected item type) method.

            if (!type.IsGenericType)
            {
                return false;
            }

            // walk up inheritance chain, until parent is either null or a ChannelReader<T>
            // TODO #2594 - add Streams here, to make sending files easy
            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ChannelReader<>))
                {
                    return true;
                }

                type = type.BaseType;
            }
            return false;
        }
    }
}
