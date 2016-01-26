// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public static class IConfigurationExtensions
    {
        // Temporary until Configuration/issues/370 is implemented.
        public static IEnumerable<KeyValuePair<string, string>> GetFlattenedSettings(this IConfiguration configuration)
        {
            var stack = new Stack<IConfiguration>();
            stack.Push(configuration);

            while (stack.Count > 0)
            {
                var config = stack.Pop();
                var section = config as IConfigurationSection;

                if (section != null)
                {
                    yield return new KeyValuePair<string, string>(section.Path, section.Value);
                }

                foreach (var child in config.GetChildren())
                {
                    stack.Push(child);
                }
            }
        }
    }
}