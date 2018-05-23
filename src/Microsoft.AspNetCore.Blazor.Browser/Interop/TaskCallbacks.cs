// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Blazor.Browser.Interop
{
    internal static class TaskCallbacks
    {
        private static IDictionary<string, Action<string>> References { get; } =
            new Dictionary<string, Action<string>>();

        public static void Track(string id, Action<string> reference)
        {
            if (References.ContainsKey(id))
            {
                throw new InvalidOperationException($"An element with id '{id}' is already being tracked.");
            }

            References.Add(id, reference);
        }

        public static void Untrack(string id)
        {
            if (!References.ContainsKey(id))
            {
                throw new InvalidOperationException($"An element with id '{id}' is not being tracked.");
            }

            References.Remove(id);
        }

        public static Action<string> Get(string id)
        {
            if (!References.ContainsKey(id))
            {
                throw new InvalidOperationException($"An element with id '{id}' is not being tracked.");
            }

            return References[id];
        }
    }
}
