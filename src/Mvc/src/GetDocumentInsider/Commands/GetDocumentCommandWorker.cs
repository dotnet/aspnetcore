// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.Extensions.ApiDescription.Tool.Commands
{
    internal class GetDocumentCommandWorker
    {
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

            var services = GetServices(entryPointType, context.AssemblyPath, context.AssemblyName);
            if (services == null)
            {
                return 3;
            }

            var success = TryProcess(context, services);
            if (!success)
            {
                // As part of the aspnet/Mvc#8425 fix, return 4 here.
                return 0;
            }

            return 0;
        }

        public static bool TryProcess(GetDocumentCommandContext context, IServiceProvider services)
        {
            var documentName = string.IsNullOrEmpty(context.DocumentName) ?
                GetDocumentCommand.FallbackDocumentName :
                context.DocumentName;
            var methodName = string.IsNullOrEmpty(context.Method) ?
                GetDocumentCommand.FallbackMethod :
                context.Method;
            var serviceName = string.IsNullOrEmpty(context.Service) ?
                GetDocumentCommand.FallbackService :
                context.Service;

            Reporter.WriteInformation(Resources.FormatUsingDocument(documentName));
            Reporter.WriteInformation(Resources.FormatUsingMethod(methodName));
            Reporter.WriteInformation(Resources.FormatUsingService(serviceName));

            try
            {
                Type serviceType = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    serviceType = assembly.GetType(serviceName, throwOnError: false);
                    if (serviceType != null)
                    {
                        break;
                    }
                }

                // As part of the aspnet/Mvc#8425 fix, make all warnings in this method errors unless the file already
                // exists.
                if (serviceType == null)
                {
                    Reporter.WriteWarning(Resources.FormatServiceTypeNotFound(serviceName));
                    return false;
                }

                var method = serviceType.GetMethod(methodName, new[] { typeof(string), typeof(TextWriter) });
                if (method == null)
                {
                    Reporter.WriteWarning(Resources.FormatMethodNotFound(methodName, serviceName));
                    return false;
                }
                else if (!typeof(Task).IsAssignableFrom(method.ReturnType))
                {
                    Reporter.WriteWarning(Resources.FormatMethodReturnTypeUnsupported(
                        methodName,
                        serviceName,
                        method.ReturnType,
                        typeof(Task)));
                    return false;
                }

                var service = services.GetService(serviceType);
                if (service == null)
                {
                    Reporter.WriteWarning(Resources.FormatServiceNotFound(serviceName));
                    return false;
                }

                // Create the output FileStream last to avoid corrupting an existing file or writing partial data.
                var stream = new MemoryStream();
                using (var writer = new StreamWriter(stream))
                {
                    var resultTask = (Task)method.Invoke(service, new object[] { documentName, writer });
                    if (resultTask == null)
                    {
                        Reporter.WriteWarning(
                            Resources.FormatMethodReturnedNull(methodName, serviceName, nameof(Task)));
                        return false;
                    }

                    var finishedIndex = Task.WaitAny(resultTask, Task.Delay(TimeSpan.FromMinutes(1)));
                    if (finishedIndex != 0)
                    {
                        Reporter.WriteWarning(Resources.FormatMethodTimedOut(methodName, serviceName, 1));
                        return false;
                    }

                    writer.Flush();
                    stream.Position = 0L;
                    using (var outStream = File.Create(context.OutputPath))
                    {
                        stream.CopyTo(outStream);

                        outStream.Flush();
                    }
                }

                return true;
            }
            catch (AggregateException ex) when (ex.InnerException != null)
            {
                foreach (var innerException in ex.Flatten().InnerExceptions)
                {
                    Reporter.WriteWarning(FormatException(innerException));
                }
            }
            catch (Exception ex)
            {
                Reporter.WriteWarning(FormatException(ex));
            }

            File.Delete(context.OutputPath);

            return false;
        }

        // TODO: Use Microsoft.AspNetCore.Hosting.WebHostBuilderFactory.Sources once we have dev feed available.
        private static IServiceProvider GetServices(Type entryPointType, string assemblyPath, string assemblyName)
        {
            var args = new[] { Array.Empty<string>() };
            var methodInfo = entryPointType.GetMethod("BuildWebHost");
            if (methodInfo != null)
            {
                // BuildWebHost (old style has highest priority)
                var parameters = methodInfo.GetParameters();
                if (!methodInfo.IsStatic ||
                    parameters.Length != 1 ||
                    typeof(string[]) != parameters[0].ParameterType ||
                    typeof(IWebHost) != methodInfo.ReturnType)
                {
                    Reporter.WriteError(
                        "BuildWebHost method found in {assemblyPath} does not have expected signature.");

                    return null;
                }

                try
                {
                    var webHost = (IWebHost)methodInfo.Invoke(obj: null, parameters: args);

                    return webHost.Services;
                }
                catch (Exception ex)
                {
                    Reporter.WriteError($"BuildWebHost method threw: {FormatException(ex)}");

                    return null;
                }
            }

            if ((methodInfo = entryPointType.GetMethod("CreateWebHostBuilder")) != null)
            {
                // CreateWebHostBuilder
                var parameters = methodInfo.GetParameters();
                if (!methodInfo.IsStatic ||
                    parameters.Length != 1 ||
                    typeof(string[]) != parameters[0].ParameterType ||
                    typeof(IWebHostBuilder) != methodInfo.ReturnType)
                {
                    Reporter.WriteError(
                        "CreateWebHostBuilder method found in {assemblyPath} does not have expected signature.");

                    return null;
                }

                try
                {
                    var builder = (IWebHostBuilder)methodInfo.Invoke(obj: null, parameters: args);

                    return builder.Build().Services;
                }
                catch (Exception ex)
                {
                    Reporter.WriteError($"CreateWebHostBuilder method threw: {FormatException(ex)}");

                    return null;
                }
            }

            return null;
        }

        private static string FormatException(Exception exception)
        {
            return $"{exception.GetType().FullName}: {exception.Message}";
        }
    }
}
