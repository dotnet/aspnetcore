// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.DotNet.Openapi.Tools;
using Microsoft.DotNet.Openapi.Tools.Internal;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.DotNet.OpenApi.Commands
{
    internal abstract class BaseCommand : CommandLineApplication
    {
        protected string WorkingDirectory;

        protected readonly IHttpClientWrapper _httpClient;

        public const string OpenApiReference = "OpenApiReference";
        public const string OpenApiProjectReference = "OpenApiProjectReference";
        protected const string SourceUrlAttrName = "SourceUrl";

        public const string ContentDispositionHeaderName = "Content-Disposition";
        private const string CodeGeneratorAttrName = "CodeGenerator";
        private const string DefaultExtension = ".json";

        internal const string PackageVersionUrl = "https://go.microsoft.com/fwlink/?linkid=2099561";

        public BaseCommand(CommandLineApplication parent, string name, IHttpClientWrapper httpClient)
        {
            Parent = parent;
            Name = name;
            Out = parent.Out ?? Out;
            Error = parent.Error ?? Error;
            _httpClient = httpClient;

            ProjectFileOption = Option("-p|--updateProject", "The project file update.", CommandOptionType.SingleValue);

            if (Parent is Application)
            {
                WorkingDirectory = ((Application)Parent).WorkingDirectory;
            }
            else
            {
                WorkingDirectory = ((Application)Parent.Parent).WorkingDirectory;
            }

            OnExecute(ExecuteAsync);
        }

        public CommandOption ProjectFileOption { get; }

        public TextWriter Warning
        {
            get { return Out; }
        }

        protected abstract Task<int> ExecuteCoreAsync();

        protected abstract bool ValidateArguments();

        private async Task<int> ExecuteAsync()
        {
            if (GetApplication().Help.HasValue())
            {
                ShowHelp();
                return 0;
            }

            if (!ValidateArguments())
            {
                ShowHelp();
                return 1;
            }

            return await ExecuteCoreAsync();
        }

        private Application GetApplication()
        {
            var parent = Parent;
            while(!(parent is Application))
            {
                parent = parent.Parent;
            }
            return (Application)parent;
        }

        internal FileInfo ResolveProjectFile(CommandOption projectOption)
        {
            string project;
            if (projectOption.HasValue())
            {
                project = projectOption.Value();
                project = GetFullPath(project);
                if (!File.Exists(project))
                {
                    throw new ArgumentException($"The project '{project}' does not exist.");
                }
            }
            else
            {
                var projects = Directory.GetFiles(WorkingDirectory, "*.csproj", SearchOption.TopDirectoryOnly);
                if (projects.Length == 0)
                {
                    throw new ArgumentException("No project files were found in the current directory. Either move to a new directory or provide the project explicitly");
                }
                if (projects.Length > 1)
                {
                    throw new ArgumentException("More than one project was found in this directory, either remove a duplicate or explicitly provide the project.");
                }

                project = projects[0];
            }

            return new FileInfo(project);
        }

        protected Project LoadProject(FileInfo projectFile)
        {
            var project = ProjectCollection.GlobalProjectCollection.LoadProject(
                projectFile.FullName,
                globalProperties: null,
                toolsVersion: null);
            project.ReevaluateIfNecessary();
            return project;
        }

        internal bool IsProjectFile(string file)
        {
            return File.Exists(Path.GetFullPath(file)) && file.EndsWith(".csproj");
        }

        internal bool IsUrl(string file)
        {
            return Uri.TryCreate(file, UriKind.Absolute, out var _) && file.StartsWith("http");
        }

        internal async Task AddOpenAPIReference(
            string tagName,
            FileInfo projectFile,
            string sourceFile,
            CodeGenerator? codeGenerator,
            string sourceUrl = null)
        {
            // EnsurePackagesInProjectAsync MUST happen before LoadProject, because otherwise the global state set by ProjectCollection doesn't pick up the nuget edits, and we end up losing them.
            await EnsurePackagesInProjectAsync(projectFile, codeGenerator);
            var project = LoadProject(projectFile);
            var items = project.GetItems(tagName);
            var fileItems = items.Where(i => string.Equals(GetFullPath(i.EvaluatedInclude), GetFullPath(sourceFile), StringComparison.Ordinal));

            if (fileItems.Any())
            {
                Warning.Write($"One or more references to {sourceFile} already exist in '{project.FullPath}'. Duplicate references could lead to unexpected behavior.");
                return;
            }

            if (sourceUrl != null)
            {
                if (items.Any(i => string.Equals(i.GetMetadataValue(SourceUrlAttrName), sourceUrl)))
                {
                    Warning.Write($"A reference to '{sourceUrl}' already exists in '{project.FullPath}'.");
                    return;
                }
            }

            var metadata = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(sourceUrl))
            {
                metadata[SourceUrlAttrName] = sourceUrl;
            }

            if (codeGenerator != null)
            {
                metadata[CodeGeneratorAttrName] = codeGenerator.ToString();
            }

            project.AddElementWithAttributes(tagName, sourceFile, metadata);
            project.Save();
        }

        private async Task EnsurePackagesInProjectAsync(FileInfo projectFile, CodeGenerator? codeGenerator)
        {
            var urlPackages = await LoadPackageVersionsFromURLAsync();
            var attributePackages = GetServicePackages(codeGenerator);

            foreach (var kvp in attributePackages)
            {
                var packageId = kvp.Key;
                var version = urlPackages != null && urlPackages.ContainsKey(packageId) ? urlPackages[packageId] : kvp.Value;

                await TryAddPackage(packageId, version, projectFile);
            }
        }

        private async Task TryAddPackage(string packageId, string packageVersion, FileInfo projectFile)
        {
            var args = new[] {
                "add",
                "package",
                packageId,
                "--version",
                packageVersion,
                "--no-restore"
            };

            var muxer = DotNetMuxer.MuxerPathOrDefault();
            if (string.IsNullOrEmpty(muxer))
            {
                throw new ArgumentException($"dotnet was not found on the path.");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = muxer,
                Arguments = string.Join(" ", args),
                WorkingDirectory = projectFile.Directory.FullName,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            using var process = Process.Start(startInfo);

            var timeout = 20;
            if (!process.WaitForExit(timeout * 1000))
            {
                throw new ArgumentException($"Adding package `{packageId}` to `{projectFile.Directory}` took longer than {timeout} seconds.");
            }

            if (process.ExitCode != 0)
            {
                using var csprojStream = projectFile.OpenRead();
                using var csprojReader = new StreamReader(csprojStream);
                var csprojContent = await csprojReader.ReadToEndAsync();
                // We suspect that sometimes dotnet add package is giving a non-zero exit code when it has actually succeeded.
                if (!csprojContent.Contains($"<PackageReference Include=\"{packageId}\" Version=\"{packageVersion}\""))
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await Out.WriteAsync(output);
                    await Error.WriteAsync(error);

                    throw new ArgumentException($"Adding package `{packageId}` to `{projectFile.Directory}` returned ExitCode `{process.ExitCode}` and gave error `{error}` and output `{output}`");
                }
            }
        }

        internal async Task DownloadToFileAsync(string url, string destinationPath, bool overwrite)
        {
            using var response = await RetryRequest(() => _httpClient.GetResponseAsync(url));
            await WriteToFileAsync(await response.Stream, destinationPath, overwrite);
        }

        internal async Task<string> DownloadGivenOption(string url, CommandOption fileOption)
        {
            using var response = await RetryRequest(() => _httpClient.GetResponseAsync(url));

            if (response.IsSuccessCode())
            {
                string destinationPath;
                if (fileOption.HasValue())
                {
                    destinationPath = fileOption.Value();
                }
                else
                {
                    var fileName = GetFileNameFromResponse(response, url);
                    var fullPath = GetFullPath(fileName);
                    var directory = Path.GetDirectoryName(fullPath);
                    destinationPath = GetUniqueFileName(directory, Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName));
                }
                await WriteToFileAsync(await response.Stream, GetFullPath(destinationPath), overwrite: false);

                return destinationPath;
            }
            else
            {
                throw new ArgumentException($"The given url returned '{response.StatusCode}', indicating failure. The url might be wrong, or there might be a networking issue.");
            }
        }

        /// <summary>
        /// Retries every 1 sec for 60 times by default.
        /// </summary>
        /// <param name="retryBlock"></param>
        /// <param name="logger"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="retryCount"></param>
        private static async Task<IHttpResponseMessageWrapper> RetryRequest(
            Func<Task<IHttpResponseMessageWrapper>> retryBlock,
            CancellationToken cancellationToken = default,
            int retryCount = 60)
        {
            for (var retry = 0; retry < retryCount; retry++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Failed to connect, retry canceled.", cancellationToken);
                }

                try
                {
                    var response = await retryBlock().ConfigureAwait(false);

                    if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        // Automatically retry on 503. May be application is still booting.
                        continue;
                    }

                    return response; // Went through successfully
                }
                catch (Exception exception)
                {
                    if (retry == retryCount - 1)
                    {
                        throw;
                    }
                    else
                    {
                        if (exception is HttpRequestException || exception is WebException)
                        {
                            await Task.Delay(1 * 1000); //Wait for a while before retry.
                        }
                    }
                }
            }

            throw new OperationCanceledException("Failed to connect, retry limit exceeded.");
        }

        private string GetUniqueFileName(string directory, string fileName, string extension)
        {
            var uniqueName = fileName;

            var filePath = Path.Combine(directory, fileName + extension);
            var exists = true;
            var count = 0;

            do
            {
                if (!File.Exists(filePath))
                {
                    exists = false;
                }
                else
                {
                    count++;
                    uniqueName = fileName + count;
                    filePath = Path.Combine(directory, uniqueName + extension);
                }
            }
            while (exists);

            return uniqueName + extension;
        }

        private string GetFileNameFromResponse(IHttpResponseMessageWrapper response, string url)
        {
            var contentDisposition = response.ContentDisposition();
            string result;
            if (contentDisposition != null && contentDisposition.FileName != null)
            {
                var fileName = Path.GetFileName(contentDisposition.FileName);
                if (!Path.HasExtension(fileName))
                {
                    fileName += DefaultExtension;
                }

                result = fileName;
            }
            else
            {
                var uri = new Uri(url);
                if (uri.Segments.Any() && uri.Segments.Last() != "/")
                {
                    var lastSegment = uri.Segments.Last();
                    if (!Path.HasExtension(lastSegment))
                    {
                        lastSegment += DefaultExtension;
                    }

                    result = lastSegment;
                }
                else
                {
                    var parts = uri.Host.Split('.');

                    // There's no segment, use the domain name.
                    string domain;
                    switch (parts.Length)
                    {
                        case 1:
                        case 2:
                            // It's localhost if 1, no www if 2
                            domain = parts.First();
                            break;
                        case 3:
                            domain = parts[1];
                            break;
                        default:
                            throw new NotImplementedException("We don't handle the case that the Host has more than three segments");
                    }

                    result = domain + DefaultExtension;
                }
            }

            return result;
        }

        internal CodeGenerator? GetCodeGenerator(CommandOption codeGeneratorOption)
        {
            CodeGenerator? codeGenerator;
            if (codeGeneratorOption.HasValue())
            {
                codeGenerator = Enum.Parse<CodeGenerator>(codeGeneratorOption.Value());
            }
            else
            {
                codeGenerator = null;
            }

            return codeGenerator;
        }

        internal void ValidateCodeGenerator(CommandOption codeGeneratorOption)
        {
            if (codeGeneratorOption.HasValue())
            {
                var value = codeGeneratorOption.Value();
                if (!Enum.TryParse(value, out CodeGenerator _))
                {
                    throw new ArgumentException($"Invalid value '{value}' given as code generator.");
                }
            }
        }

        internal string GetFullPath(string path)
        {
            return Path.IsPathFullyQualified(path)
                ? path
                : Path.GetFullPath(path, WorkingDirectory);
        }

        private async Task<IDictionary<string, string>> LoadPackageVersionsFromURLAsync()
        {
            /* Example Json content
             {
              "Version" : "1.0",
              "Packages"  :  {
                "Microsoft.Azure.SignalR": "1.1.0-preview1-10442",
                "Grpc.AspNetCore.Server": "0.1.22-pre2",
                "Grpc.Net.ClientFactory": "0.1.22-pre2",
                "Google.Protobuf": "3.8.0",
                "Grpc.Tools": "1.22.0",
                "NSwag.ApiDescription.Client": "13.0.3",
                "Microsoft.Extensions.ApiDescription.Client": "0.3.0-preview7.19365.7",
                "Newtonsoft.Json": "12.0.2"
              }
            }*/
            try
            {
                using var packageVersionStream = await (await _httpClient.GetResponseAsync(PackageVersionUrl)).Stream;
                using var packageVersionDocument = await JsonDocument.ParseAsync(packageVersionStream);
                var packageVersionsElement = packageVersionDocument.RootElement.GetProperty("Packages");
                var packageVersionsDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var packageVersion in packageVersionsElement.EnumerateObject())
                {
                    packageVersionsDictionary[packageVersion.Name] = packageVersion.Value.GetString();
                }

                return packageVersionsDictionary;
            }
            catch
            {
                // TODO (johluo): Consider logging a message indicating what went wrong and actions, if any, to be taken to resolve possible issues.
                // Currently not logging anything since the fwlink is not published yet.
                return null;
            }
        }

        private static IDictionary<string, string> GetServicePackages(CodeGenerator? type)
        {
            CodeGenerator generator = type ?? CodeGenerator.NSwagCSharp;
            var name = Enum.GetName(typeof(CodeGenerator), generator);
            var attributes = typeof(Program).Assembly.GetCustomAttributes<OpenApiDependencyAttribute>();

            var packages = attributes.Where(a => a.CodeGenerators.Contains(generator));
            var result = new Dictionary<string, string>();
            if (packages != null)
            {
                foreach (var package in packages)
                {
                    result[package.Name] = package.Version;
                }
            }

            return result;
        }

        private static byte[] GetHash(Stream stream)
        {
            SHA256 algorithm;
            try
            {
                algorithm = SHA256.Create();
            }
            catch (TargetInvocationException)
            {
                // SHA256.Create is documented to throw this exception on FIPS-compliant machines. See
                // https://msdn.microsoft.com/en-us/library/z08hz7ad Fall back to a FIPS-compliant SHA256 algorithm.
                algorithm = new SHA256CryptoServiceProvider();
            }

            using (algorithm)
            {
                return algorithm.ComputeHash(stream);
            }
        }

        private async Task WriteToFileAsync(Stream content, string destinationPath, bool overwrite)
        {
            if (content.CanSeek)
            {
                content.Seek(0, SeekOrigin.Begin);
            }

            destinationPath = GetFullPath(destinationPath);
            var destinationExists = File.Exists(destinationPath);
            if (destinationExists && !overwrite)
            {
                throw new ArgumentException($"File '{destinationPath}' already exists. Aborting to avoid conflicts. Provide the '--output-file' argument with an unused file to resolve.");
            }

            await Out.WriteLineAsync($"Downloading to '{destinationPath}'.");
            var reachedCopy = false;
            try
            {
                if (destinationExists)
                {
                    // Check hashes before using the downloaded information.
                    var downloadHash = GetHash(content);

                    byte[] destinationHash;
                    using (var destinationStream = File.OpenRead(destinationPath))
                    {
                        destinationHash = GetHash(destinationStream);
                    }

                    var sameHashes = downloadHash.Length == destinationHash.Length;
                    for (var i = 0; sameHashes && i < downloadHash.Length; i++)
                    {
                        sameHashes = downloadHash[i] == destinationHash[i];
                    }

                    if (sameHashes)
                    {
                        await Out.WriteLineAsync($"Not overwriting existing and matching file '{destinationPath}'.");
                        return;
                    }
                }
                else
                {
                    // May need to create directory to hold the file.
                    var destinationDirectory = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }
                }

                // Create or overwrite the destination file.
                reachedCopy = true;
                using var fileStream = new FileStream(destinationPath, FileMode.OpenOrCreate, FileAccess.Write);
                fileStream.Seek(0, SeekOrigin.Begin);
                if (content.CanSeek)
                {
                    content.Seek(0, SeekOrigin.Begin);
                }
                await content.CopyToAsync(fileStream);
            }
            catch (Exception ex)
            {
                await Error.WriteLineAsync($"Downloading failed.");
                await Error.WriteLineAsync(ex.ToString());
                if (reachedCopy)
                {
                    File.Delete(destinationPath);
                }
            }
        }
    }
}
