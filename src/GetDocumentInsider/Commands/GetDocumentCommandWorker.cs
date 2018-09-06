using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.ApiDescription.Client.Properties;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.ApiDescription.Client.Commands
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
                Reporter.WriteError(Resources.MissingEntryPoint(context.AssemblyPath));
                return 2;
            }

            var services = GetServices(entryPointType, context.AssemblyPath, context.AssemblyName);
            if (services == null)
            {
                return 3;
            }

            var success = TryProcess(context, services);
            if (!success && string.IsNullOrEmpty(context.Uri))
            {
                return 4;
            }

            var builder = GetBuilder(entryPointType, context.AssemblyPath, context.AssemblyName);
            if (builder == null)
            {
                return 5;
            }

            // Mute the HttpsRedirectionMiddleware warning about HTTPS configuration.
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.AddFilter(
                "Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware",
                LogLevel.Error));

            using (var server = new TestServer(builder))
            {
                ProcessAsync(context, server).Wait();
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

            Reporter.WriteInformation(Resources.UsingDocument(documentName));
            Reporter.WriteInformation(Resources.UsingMethod(methodName));
            Reporter.WriteInformation(Resources.UsingService(serviceName));

            try
            {
                var serviceType = Type.GetType(serviceName, throwOnError: true);
                var method = serviceType.GetMethod(methodName, new[] { typeof(TextWriter), typeof(string) });
                var service = services.GetRequiredService(serviceType);

                var success = true;
                using (var writer = File.CreateText(context.Output))
                {
                    if (method.ReturnType == typeof(bool))
                    {
                        success = (bool)method.Invoke(service, new object[] { writer, documentName });
                    }
                    else
                    {
                        method.Invoke(service, new object[] { writer, documentName });
                    }
                }

                if (!success)
                {
                    var message = Resources.MethodInvocationFailed(methodName, serviceName, documentName);
                    if (string.IsNullOrEmpty(context.Uri) && !File.Exists(context.Output))
                    {
                        Reporter.WriteError(message);
                    }
                    else
                    {
                        Reporter.WriteWarning(message);
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                var message = FormatException(ex);
                if (string.IsNullOrEmpty(context.Uri) && !File.Exists(context.Output))
                {
                    Reporter.WriteError(message);
                }
                else
                {
                    Reporter.WriteWarning(message);
                }

                return false;
            }
        }

        public static async Task ProcessAsync(GetDocumentCommandContext context, TestServer server)
        {

            Debug.Assert(!string.IsNullOrEmpty(context.Uri));
            Reporter.WriteInformation(Resources.UsingUri(context.Uri));

            var httpClient = server.CreateClient();
            await DownloadFileCore.DownloadAsync(
                context.Uri,
                context.Output,
                httpClient,
                new LogWrapper(),
                CancellationToken.None,
                timeoutSeconds: 60);
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

            // Startup
            return new WebHostBuilder().UseStartup(assemblyName).Build().Services;
        }

        // TODO: Use Microsoft.AspNetCore.Hosting.WebHostBuilderFactory.Sources once we have dev feed available.
        private static IWebHostBuilder GetBuilder(Type entryPointType, string assemblyPath, string assemblyName)
        {
            var methodInfo = entryPointType.GetMethod("BuildWebHost");
            if (methodInfo != null)
            {
                // BuildWebHost cannot be used. Fall through, most likely to Startup fallback.
                Reporter.WriteWarning(
                    "BuildWebHost method cannot be used. Falling back to minimal Startup configuration.");
            }

            methodInfo = entryPointType.GetMethod("CreateWebHostBuilder");
            if (methodInfo != null)
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
                    var args = new[] { Array.Empty<string>() };
                    var builder = (IWebHostBuilder)methodInfo.Invoke(obj: null, parameters: args);

                    return builder;
                }
                catch (Exception ex)
                {
                    Reporter.WriteError($"CreateWebHostBuilder method threw: {FormatException(ex)}");

                    return null;
                }
            }

            // Startup
            return new WebHostBuilder().UseStartup(assemblyName);
        }

        private static string FormatException(Exception exception)
        {
            return $"{exception.GetType().FullName}: {exception.Message}";
        }
    }
}
