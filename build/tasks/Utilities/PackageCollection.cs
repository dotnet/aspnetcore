// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace RepoTasks.Utilities
{
    public class PackageCollection
    {
        private readonly IDictionary<string, PackageCategory> _packages = new Dictionary<string, PackageCategory>(StringComparer.OrdinalIgnoreCase);

        private PackageCollection()
        {
        }

        public bool TryGetCategory(string packageId, out PackageCategory category) => _packages.TryGetValue(packageId, out category);

        public void Remove(string packageId) => _packages.Remove(packageId);

        public int Count => _packages.Count;

        public IEnumerable<string> Keys => _packages.Keys;

        public static PackageCollection FromItemGroup(ITaskItem[] items)
        {
            var list = new PackageCollection();
            if (items == null)
            {
                return list;
            }

            foreach (var item in items)
            {
                PackageCategory category;
                switch (item.GetMetadata("Category")?.ToLowerInvariant())
                {
                    case "ship":
                        category = PackageCategory.Shipping;
                        break;
                    case "noship":
                        category = PackageCategory.NoShip;
                        break;
                    case "shipoob":
                        category = PackageCategory.ShipOob;
                        break;
                    default:
                        category = PackageCategory.Unknown;
                        break;
                }

                if (list._packages.ContainsKey(item.ItemSpec))
                {
                    throw new InvalidDataException($"Duplicate package id detected: {item.ItemSpec}");
                }

                list._packages.Add(item.ItemSpec, category);
            }

            return list;
        }
    }
}
