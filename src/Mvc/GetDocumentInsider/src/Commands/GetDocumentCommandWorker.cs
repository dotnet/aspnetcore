// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.Extensions.ApiDescription.Tool.Commands
{
    internal class GetDocumentCommandWorker
    {
        private const string DocumentService = "Microsoft.Extensions.ApiDescriptions.IDocumentProvider";
        private static readonly char[] InvalidFilenameCharacters = Path.GetInvalidFileNameChars();
        private static readonly string[] InvalidFilenameStrings = new[] { ".." };

        private const string BuildMethodName = "BuildWebHost";
        private static readonly object[] BuildArguments = new[] { Array.Empty<string>() };
        private static readonly Type[] BuildParameterTypes = new[] { typeof(string[]) };
        private static readonly Type BuildReturnType = typeof(IWebHost);

        private const string CreateMethodName = "CreateWebHostBuilder";
        private static readonly object[] CreateArguments = BuildArguments;
        private static readonly Type[] CreateParameterTypes = BuildParameterTypes;
        private static readonly Type CreateReturnType = typeof(IWebHostBuilder);

        private const string GetDocumentsMethodName = "GetDocumentNames";
        private static readonly object[] GetDocumentsArguments = Array.Empty<object>();
        private static readonly Type[] GetDocumentsParameterTypes = Type.EmptyTypes;
        private static readonly Type GetDocumentsReturnType = typeof(IEnumerable<string>);

        private const string GenerateMethodName = "GenerateAsync";
        private static readonly Type[] GenerateMethodParameterTypes = new[] { typeof(string), typeof(TextWriter) };
        private static readonly Type GenerateMethodReturnType = typeof(Task);

        public static int Process(GetDocumentCommandContext context)
        {
            var assemblyName = new AssemblyName(context.AssemblyName);
            var assembly = Assembly.Load(assemblyName);
            var entryPointType = assembly.EntryPoint?.DeclaringType;
            if (entryPointType == null)
            {
                Reporter.WriteError(Resources.FormatMissingEntryPoint(context.AssemblyPath));
                return 2;
            }

            static string FormatException(Exception exception) => $"{exception.GetType().FullName}: {exception.Message}";

            try
            {
                var services = GetServices(entryPointType);
                if (services == null)
                {
                    return 3;
                }

                var success = GetDocuments(context, services);
                if (!success)
                {
                    return 4;
                }
            }
            catch (AggregateException ex) when (ex.InnerException != null)
            {
                foreach (var innerException in ex.Flatten().InnerExceptions)
                {
                    Reporter.WriteError(FormatException(innerException));
                }

                Reporter.WriteVerbose(ex.StackTrace);
                return 5;
            }
            catch (Exception ex)
            {
                Reporter.WriteError(FormatException(ex));
                Reporter.WriteVerbose(ex.StackTrace);
                return 6;
            }

            return 0;
        }

        private static IServiceProvider GetServices(Type entryPointType)
        {
            // BuildWebHost (old style has highest priority)
            var methodInfo = GetMethod(
                BuildMethodName,
                entryPointType,
                BuildParameterTypes,
                BuildReturnType,
                // Will fall back to find CreateWebHostBuilder method.
                reportErrors: false,
                isStatic: true);
            if (methodInfo != null)
            {
                var webHost = (IWebHost)InvokeMethod(methodInfo, instance: null, arguments: BuildArguments);
                if (webHost == null)
                {
                    return null;
                }

                return webHost.Services;
            }

            // CreateWebHostBuilder
            methodInfo = GetMethod(
                CreateMethodName,
                entryPointType,
                CreateParameterTypes,
                CreateReturnType,
                // Use a different message for this case.
                reportErrors: false,
                isStatic: true);
            if (methodInfo == null)
            {
                Reporter.WriteError(Resources.FormatMethodsNotFound(
                    BuildMethodName,
                    CreateMethodName,
                    entryPointType));

                return null;
            }

            var builder = (IWebHostBuilder)InvokeMethod(methodInfo, instance: null, arguments: CreateArguments);
            if (builder == null)
            {
                return null;
            }

            return builder.Build().Services;
        }

        private static bool GetDocuments(GetDocumentCommandContext context, IServiceProvider services)
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
                Reporter.WriteError(Resources.FormatServiceTypeNotFound(DocumentService));
                return false;
            }

            var getDocumentsMethod = GetMethod(
                GetDocumentsMethodName,
                serviceType,
                GetDocumentsParameterTypes,
                GetDocumentsReturnType,
                reportErrors: true,
                isStatic: false);
            if (getDocumentsMethod == null)
            {
                return false;
            }

            var generateMethod = GetMethod(
                GenerateMethodName,
                serviceType,
                GenerateMethodParameterTypes,
                GenerateMethodReturnType,
                reportErrors: true,
                isStatic: false);
            if (generateMethod == null)
            {
                return false;
            }

            var service = services.GetService(serviceType);
            if (service == null)
            {
                Reporter.WriteError(Resources.FormatServiceNotFound(DocumentService));
                return false;
            }

            var documentNames = (IEnumerable<string>)InvokeMethod(getDocumentsMethod, service, GetDocumentsArguments);
            if (documentNames == null)
            {
                return false;
            }

            // Write out the documents.
            Directory.CreateDirectory(context.OutputDirectory);
            var filePathList = new List<string>();
            foreach (var documentName in documentNames)
            {
                var filePath = GetDocument(
                    documentName,
                    context.ProjectName,
                    context.OutputDirectory,
                    generateMethod,
                    service);
                if (filePath == null)
                {
                    return false;
                }

                filePathList.Add(filePath);
            }

            // Write out the cache file.
            var stream = File.Create(context.FileListPath);
            using var writer = new StreamWriter(stream) { AutoFlush = true };
            writer.WriteLine(string.Join(Environment.NewLine, filePathList));
            stream.Flush();

            return true;
        }

        private static string GetDocument(
            string documentName,
            string projectName,
            string outputDirectory,
            MethodInfo generateMethod,
            object service)
        {
            Reporter.WriteInformation(Resources.FormatRetrievingDocument(documentName));

            var stream = new MemoryStream();
            using var writer = new StreamWriter(stream) { AutoFlush = true };
            var resultTask = (Task)InvokeMethod(generateMethod, service, new object[] { documentName, writer });
            if (resultTask == null)
            {
                return null;
            }

            var finishedIndex = Task.WaitAny(resultTask, Task.Delay(TimeSpan.FromMinutes(1)));
            if (finishedIndex != 0)
            {
                Reporter.WriteError(Resources.FormatMethodTimedOut(GenerateMethodName, DocumentService, 1));
                return null;
            }

            var filePath = GetDocumentPath(documentName, projectName, outputDirectory);
            try
            {
                if (stream.Length == 0L)
                {
                    Reporter.WriteError(Resources.FormatMethodWroteNoContent(
                        GenerateMethodName,
                        DocumentService,
                        documentName));

                    return null;
                }

                stream.Position = 0L;
                Reporter.WriteInformation(Resources.FormatWritingDocument(documentName, filePath));

                // Create the output FileStream last to avoid corrupting an existing file or writing partial data.
                using var outStream = File.Create(filePath);
                stream.CopyTo(outStream);
                outStream.Flush();
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
            if (string.Equals("v1", documentName, StringComparison.Ordinal))
            {
                // Leave default document name out of the filename.
                path = projectName + ".json";
            }
            else
            {
                // Sanitize the document name because it may contain almost any character, including illegal filename
                // characters such as '/' and '?' and the string "..". Do not treat slashes as folder separators.
                var sanitizedDocumentName = string.Join("_", documentName.Split(InvalidFilenameCharacters));
                while (sanitizedDocumentName.Contains(InvalidFilenameStrings[0]))
                {
                    sanitizedDocumentName = string.Join(
                        ".",
                        sanitizedDocumentName.Split(InvalidFilenameStrings, StringSplitOptions.None));
                }

                path = $"{projectName}_{documentName}.json";
            }

            if (!string.IsNullOrEmpty(outputDirectory))
            {
                path = Path.Combine(outputDirectory, path);
            }

            return path;
        }

        private static MethodInfo GetMethod(
            string methodName,
            Type type,
            Type[] parameterTypes,
            Type returnType,
            bool reportErrors,
            bool isStatic)
        {
            static void Report(bool reportErrors, string message)
            {
                if (reportErrors)
                {
                    Reporter.WriteError(message);
                }
                else
                {
                    Reporter.WriteWarning(message);
                }
            }

            var method = type.GetMethod(methodName, parameterTypes);
            if (method == null)
            {
                Report(reportErrors, Resources.FormatMethodNotFound(methodName, type));
                return null;
            }

            if (isStatic != method.IsStatic)
            {
                if (isStatic)
                {
                    Report(reportErrors, Resources.FormatMethodIsNotStatic(methodName, type));
                }
                else
                {
                    Report(reportErrors, Resources.FormatMethodIsStatic(methodName, type));
                }

                return null;
            }

            if (!returnType.IsAssignableFrom(method.ReturnType))
            {
                Report(
                    reportErrors,
                    Resources.FormatMethodReturnTypeUnsupported(methodName, type, method.ReturnType, returnType));

                return null;
            }

            return method;
        }

        private static object InvokeMethod(MethodInfo method, object instance, object[] arguments)
        {
            var result = method.Invoke(instance, arguments);
            if (result == null)
            {
                Reporter.WriteError(Resources.FormatMethodReturnedNull(
                    method.Name,
                    method.DeclaringType,
                    method.ReturnType));
            }

            return result;
        }
    }
}
