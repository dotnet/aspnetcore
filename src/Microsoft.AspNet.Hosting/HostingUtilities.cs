// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.IO;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingUtilities
    {
        internal static Tuple<string, string> SplitTypeName(string identifier)
        {
            string typeName = null;
            string assemblyName = identifier.Trim();
            var parts = identifier.Split(new[] { ',' }, 2);
            if (parts.Length == 2)
            {
                typeName = parts[0].Trim();
                assemblyName = parts[1].Trim();
            }
            return new Tuple<string, string>(typeName, assemblyName);
        }

        public static string GetWebRoot(string applicationBasePath)
        {
            try
            {
                var config = new Configuration();
                config.AddJsonFile(Path.Combine(applicationBasePath, "project.json"));
                var webroot = config.Get("webroot") ?? string.Empty;
                return Path.GetFullPath(Path.Combine(applicationBasePath, webroot));
            }
            catch (Exception)
            {
                return applicationBasePath;
            }
        }
    }
}
