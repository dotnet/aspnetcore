// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.ApplicationModelDetection
{
    public class AppModelDetector
    {
        // We use Hosting package to detect AspNetCore version
        // it contains light-up implemenation that we care about and
        // would have to be used by aspnet core web apps
        private const string AspNetCoreAssembly = "Microsoft.AspNetCore.Hosting";

        /// <summary>
        /// Reads the following sources
        ///     - web.config to detect dotnet framework kind
        ///     - *.runtimeconfig.json to detect target framework version
        ///     - *.deps.json to detect Asp.Net Core version
        ///     - Microsoft.AspNetCore.Hosting.dll to detect Asp.Net Core version
        /// </summary>
        /// <param name="directory">The application directory</param>
        /// <returns>The <see cref="AppModelDetectionResult"/> instance containing information about application</returns>
        public AppModelDetectionResult Detect(DirectoryInfo directory)
        {
            string entryPoint = null;

            // Try reading web.config and resolving framework and app path
            var webConfig = directory.GetFiles("web.config").FirstOrDefault();

            bool webConfigExists = webConfig != null;
            bool? usesDotnetExe = null;

            if (webConfigExists &&
                TryParseWebConfig(webConfig, out var dotnetExe, out entryPoint))
            {
                usesDotnetExe = dotnetExe;
            }

            // If we found entry point let's look for .deps.json
            // in some cases it exists in desktop too
            FileInfo depsJson = null;
            FileInfo runtimeConfig = null;

            if (!string.IsNullOrWhiteSpace(entryPoint))
            {
                depsJson = new FileInfo(Path.ChangeExtension(entryPoint, ".deps.json"));
                runtimeConfig = new FileInfo(Path.ChangeExtension(entryPoint, ".runtimeconfig.json"));
            }

            if (depsJson == null || !depsJson.Exists)
            {
                depsJson = directory.GetFiles("*.deps.json").FirstOrDefault();
            }

            if (runtimeConfig == null || !runtimeConfig.Exists)
            {
                runtimeConfig = directory.GetFiles("*.runtimeconfig.json").FirstOrDefault();
            }

            string aspNetCoreVersionFromDeps = null;
            string aspNetCoreVersionFromDll = null;


            // Try to detect ASP.NET Core version from .deps.json
            if (depsJson != null &&
                depsJson.Exists  &&
                TryParseDependencies(depsJson, out var aspNetCoreVersion))
            {
                aspNetCoreVersionFromDeps = aspNetCoreVersion;
            }

            // Try to detect ASP.NET Core version from .deps.json
            var aspNetCoreDll = directory.GetFiles(AspNetCoreAssembly + ".dll").FirstOrDefault();
            if (aspNetCoreDll != null &&
                TryParseAssembly(aspNetCoreDll, out aspNetCoreVersion))
            {
                aspNetCoreVersionFromDll = aspNetCoreVersion;
            }

            // Try to detect dotnet core runtime version from runtimeconfig.json
            string runtimeVersionFromRuntimeConfig = null;
            if (runtimeConfig != null &&
                runtimeConfig.Exists)
            {
                TryParseRuntimeConfig(runtimeConfig, out runtimeVersionFromRuntimeConfig);
            }

            var result = new AppModelDetectionResult();
            if (usesDotnetExe == true)
            {
                result.Framework = RuntimeFramework.DotNetCore;
                result.FrameworkVersion = runtimeVersionFromRuntimeConfig;
            }
            else
            {
                if (depsJson?.Exists == true &&
                    runtimeConfig?.Exists == true)
                {
                    result.Framework = RuntimeFramework.DotNetCoreStandalone;
                }
                else
                {
                    result.Framework = RuntimeFramework.DotNetFramework;
                }
            }

            result.AspNetCoreVersion = aspNetCoreVersionFromDeps ?? aspNetCoreVersionFromDll;

            return result;
        }

        private bool TryParseAssembly(FileInfo aspNetCoreDll, out string aspNetCoreVersion)
        {
            aspNetCoreVersion = null;
            try
            {
                using (var stream = aspNetCoreDll.OpenRead())
                using (var peReader = new PEReader(stream))
                {
                    var metadataReader = peReader.GetMetadataReader();
                    var assemblyDefinition = metadataReader.GetAssemblyDefinition();
                    aspNetCoreVersion = assemblyDefinition.Version.ToString();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Search for Microsoft.AspNetCore.Hosting entry in deps.json and get it's version number
        /// </summary>
        private bool TryParseDependencies(FileInfo depsJson, out string aspnetCoreVersion)
        {
            aspnetCoreVersion = null;
            try
            {
                using (var streamReader = depsJson.OpenText())
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    var json = JObject.Load(jsonReader);

                    var libraryPrefix = AspNetCoreAssembly+ "/";

                    var library = json.Descendants().OfType<JProperty>().FirstOrDefault(property => property.Name.StartsWith(libraryPrefix));
                    if (library != null)
                    {
                        aspnetCoreVersion = library.Name.Substring(libraryPrefix.Length);
                        return true;
                    }
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        private bool TryParseRuntimeConfig(FileInfo runtimeConfig, out string frameworkVersion)
        {
            frameworkVersion = null;
            try
            {
                using (var streamReader = runtimeConfig.OpenText())
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    var json = JObject.Load(jsonReader);
                    frameworkVersion = (string)json?["runtimeOptions"]
                        ?["framework"]
                        ?["version"];

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool TryParseWebConfig(FileInfo webConfig, out bool usesDotnetExe, out string entryPoint)
        {
            usesDotnetExe = false;
            entryPoint = null;

            try
            {
                var xdocument = XDocument.Load(webConfig.FullName);
                var aspNetCoreHandler = xdocument.Root?
                    .Element("system.webServer")
                    .Element("aspNetCore");

                if (aspNetCoreHandler == null)
                {
                    return false;
                }

                var processPath = (string) aspNetCoreHandler.Attribute("processPath");
                var arguments = (string) aspNetCoreHandler.Attribute("arguments");

                if (processPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase) ||
                    processPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(arguments))
                {
                    usesDotnetExe = true;
                    var entryPointPart = arguments.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(entryPointPart))
                    {
                        try
                        {
                            entryPoint = Path.GetFullPath(Path.Combine(webConfig.DirectoryName, entryPointPart));
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                else
                {
                    usesDotnetExe = false;

                    try
                    {
                        entryPoint = Path.GetFullPath(Path.Combine(webConfig.DirectoryName, processPath));
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}