// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.ApiDescription.Tool.Commands
{
    internal class GetDocumentCommandWorker
    {
        private const string DefaultDocumentName = "v1";
        private const string DocumentService = "Microsoft.Extensions.ApiDescriptions.IDocumentProvider";
        private const string DotString = ".";
        private const string InvalidFilenameString = "..";
        private const string JsonExtension = ".json";
        private const string UnderscoreString = "_";
        private static readonly char[] InvalidFilenameCharacters = Path.GetInvalidFileNameChars();
        private static readonly Encoding UTF8EncodingWithoutBOM
            = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        private const string GetDocumentsMethodName = "GetDocumentNames";
        private static readonly object[] GetDocumentsArguments = Array.Empty<object>();
        private static readonly Type[] GetDocumentsParameterTypes = Type.EmptyTypes;
        private static readonly Type GetDocumentsReturnType = typeof(IEnumerable<string>);

        private const string GenerateMethodName = "GenerateAsync";
        private static readonly Type[] GenerateMethodParameterTypes = new[] { typeof(string), typeof(TextWriter) };
        private static readonly Type GenerateMethodReturnType = typeof(Task);

        private readonly GetDocumentCommandContext _context;
        private readonly IReporter _reporter;

        public GetDocumentCommandWorker(GetDocumentCommandContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _reporter = context.Reporter;
        }

        public int Process()
        {
            var assemblyName = new AssemblyName(_context.AssemblyName);
            var assembly = Assembly.Load(assemblyName);
            var entryPointType = assembly.EntryPoint?.DeclaringType;
            if (entryPointType == null)
            {
                _reporter.WriteError(Resources.FormatMissingEntryPoint(_context.AssemblyPath));
                return 3;
            }

            try
            {
                var serviceFactory = HostFactoryResolver.ResolveServiceProviderFactory(assembly);
                if (serviceFactory == null)
                {
                    _reporter.WriteError(Resources.FormatMethodsNotFound(
                        HostFactoryResolver.BuildWebHost,
                        HostFactoryResolver.CreateHostBuilder,
                        HostFactoryResolver.CreateWebHostBuilder,
                        entryPointType));

                    return 4;
                }

                var services = serviceFactory(Array.Empty<string>());
                if (services == null)
                {
                    _reporter.WriteError(Resources.FormatServiceProviderNotFound(
                        typeof(IServiceProvider),
                        HostFactoryResolver.BuildWebHost,
                        HostFactoryResolver.CreateHostBuilder,
                        HostFactoryResolver.CreateWebHostBuilder,
                        entryPointType));

                    return 5;
                }

                var success = GetDocuments(services);
                if (!success)
                {
                    return 6;
                }
            }
            catch (Exception ex)
            {
                _reporter.WriteError(ex.ToString());
                return 7;
            }

            return 0;
        }

        private bool GetDocuments(IServiceProvider services)
        {
            Type serviceType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                serviceType = assembly.GetType(DocumentService, throwOnError: false);
                if (serviceType != null)
                {
                    break;
                }
            }

            if (serviceType == null)
            {
                _reporter.WriteError(Resources.FormatServiceTypeNotFound(DocumentService));
                return false;
            }

            var getDocumentsMethod = GetMethod(
                GetDocumentsMethodName,
                serviceType,
                GetDocumentsParameterTypes,
                GetDocumentsReturnType);
            if (getDocumentsMethod == null)
            {
                return false;
            }

            var generateMethod = GetMethod(
                GenerateMethodName,
                serviceType,
                GenerateMethodParameterTypes,
                GenerateMethodReturnType);
            if (generateMethod == null)
            {
                return false;
            }

            var service = services.GetService(serviceType);
            if (service == null)
            {
                _reporter.WriteError(Resources.FormatServiceNotFound(DocumentService));
                return false;
            }

            var documentNames = (IEnumerable<string>)InvokeMethod(getDocumentsMethod, service, GetDocumentsArguments);
            if (documentNames == null)
            {
                return false;
            }

            // Write out the documents.
            var found = false;
            Directory.CreateDirectory(_context.OutputDirectory);
            var filePathList = new List<string>();
            foreach (var documentName in documentNames)
            {
                var filePath = GetDocument(
                    documentName,
                    _context.ProjectName,
                    _context.OutputDirectory,
                    generateMethod,
                    service);
                if (filePath == null)
                {
                    return false;
                }

                filePathList.Add(filePath);
                found = true;
            }

            // Write out the cache file.
            var stream = File.Create(_context.FileListPath);
            using var writer = new StreamWriter(stream);
            writer.WriteLine(string.Join(Environment.NewLine, filePathList));

            if (!found)
            {
                _reporter.WriteError(Resources.DocumentsNotFound);
            }

            return found;
        }

        private string GetDocument(
            string documentName,
            string projectName,
            string outputDirectory,
            MethodInfo generateMethod,
            object service)
        {
            _reporter.WriteInformation(Resources.FormatGeneratingDocument(documentName));

            using var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, UTF8EncodingWithoutBOM, bufferSize: 1024, leaveOpen: true))
            {
                var arguments = new object[] { documentName, writer };
                using var resultTask = (Task)InvokeMethod(generateMethod, service, arguments);
                if (resultTask == null)
                {
                    return null;
                }

                var finished = resultTask.Wait(TimeSpan.FromMinutes(1));
                if (!finished)
                {
                    _reporter.WriteError(Resources.FormatMethodTimedOut(GenerateMethodName, DocumentService, 1));
                    return null;
                }
            }

            if (stream.Length == 0L)
            {
                _reporter.WriteError(
                    Resources.FormatMethodWroteNoContent(GenerateMethodName, DocumentService, documentName));

                return null;
            }

            var filePath = GetDocumentPath(documentName, projectName, outputDirectory);
            _reporter.WriteInformation(Resources.FormatWritingDocument(documentName, filePath));
            try
            {
                stream.Position = 0L;

                // Create the output FileStream last to avoid corrupting an existing file or writing partial data.
                using var outStream = File.Create(filePath);
                stream.CopyTo(outStream);
            }
            catch
            {
                File.Delete(filePath);
                throw;
            }

            return filePath;
        }

        private static string GetDocumentPath(string documentName, string projectName, string outputDirectory)
        {
            string path;
            if (string.Equals(DefaultDocumentName, documentName, StringComparison.Ordinal))
            {
                // Leave default document name out of the filename.
                path = projectName + JsonExtension;
            }
            else
            {
                // Sanitize the document name because it may contain almost any character, including illegal filename
                // characters such as '/' and '?' and the string "..". Do not treat slashes as folder separators.
                var sanitizedDocumentName = string.Join(
                    UnderscoreString,
                    documentName.Split(InvalidFilenameCharacters));

                while (sanitizedDocumentName.Contains(InvalidFilenameString))
                {
                    sanitizedDocumentName = sanitizedDocumentName.Replace(InvalidFilenameString, DotString);
                }

                path = $"{projectName}_{documentName}{JsonExtension}";
            }

            if (!string.IsNullOrEmpty(outputDirectory))
            {
                path = Path.Combine(outputDirectory, path);
            }

            return path;
        }

        private MethodInfo GetMethod(string methodName, Type type, Type[] parameterTypes, Type returnType)
        {
            var method = type.GetMethod(methodName, parameterTypes);
            if (method == null)
            {
                _reporter.WriteError(Resources.FormatMethodNotFound(methodName, type));
                return null;
            }

            if (method.IsStatic)
            {
                _reporter.WriteError(Resources.FormatMethodIsStatic(methodName, type));
                return null;
            }

            if (!returnType.IsAssignableFrom(method.ReturnType))
            {
                _reporter.WriteError(
                    Resources.FormatMethodReturnTypeUnsupported(methodName, type, method.ReturnType, returnType));

                return null;
            }

            return method;
        }

        private object InvokeMethod(MethodInfo method, object instance, object[] arguments)
        {
            var result = method.Invoke(instance, arguments);
            if (result == null)
            {
                _reporter.WriteError(
                    Resources.FormatMethodReturnedNull(method.Name, method.DeclaringType, method.ReturnType));
            }

            return result;
        }
    }
}
