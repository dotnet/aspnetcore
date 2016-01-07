// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNet.Hosting.Internal
{
    // TODO: Remove this once https://github.com/aspnet/Configuration/pull/349 gets merged
    internal class IncludedConfigurationProvider : ConfigurationProvider
    {
        public IncludedConfigurationProvider(IConfiguration source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            int pathStart = 0;
            var section = source as IConfigurationSection;
            if (section != null)
            {
                pathStart = section.Path.Length + 1;
            }
            foreach (var child in source.GetChildren())
            {
                AddSection(child, pathStart);
            }
        }

        private void AddSection(IConfigurationSection section, int pathStart)
        {
            Data.Add(section.Path.Substring(pathStart), section.Value);
            foreach (var child in section.GetChildren())
            {
                AddSection(child, pathStart);
            }
        }
    }
}
