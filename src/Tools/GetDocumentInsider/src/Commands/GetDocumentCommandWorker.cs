// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Tools.Internal;
using Microsoft.OpenApi;
#if NET7_0_OR_GREATER
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Features;
#endif

namespace Microsoft.Extensions.ApiDescription.Tool.Commands;

internal sealed class GetDocumentCommandWorker
{
    private const string DefaultDocumentName = "v1";
    private const string DocumentService = "Microsoft.Extensions.ApiDescriptions.IDocumentProvider";
    private const string DotString = ".";
    private const string InvalidFilenameString = "..";
    private const string JsonExtension = ".json";
    private const string UnderscoreString = "_";
    private static readonly char[] _invalidFilenameCharacters = Path.GetInvalidFileNameChars();
    private static readonly Encoding _utf8EncodingWithoutBOM
        = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private const string GetDocumentsMethodName = "GetDocumentNames";
    private static readonly object[] _getDocumentsArguments = Array.Empty<object>();
    private static readonly Type[] _getDocumentsParameterTypes = Type.EmptyTypes;
    private static readonly Type _getDocumentsReturnType = typeof(IEnumerable<string>);

    private const string GenerateMethodName = "GenerateAsync";
    private static readonly Type[] _generateMethodParameterTypes = [typeof(string), typeof(TextWriter)];
    private static readonly Type[] _generateWithVersionMethodParameterTypes = [typeof(string), typeof(TextWriter), typeof(OpenApiSpecVersion)];
    private static readonly Type _generateMethodReturnType = typeof(Task);

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

#if NET7_0_OR_GREATER
        // Register no-op implementations of IServer and IHostLifetime
        // to prevent the application server from actually launching after build.
        void ConfigureHostBuilder(object hostBuilder)
        {
            ((IHostBuilder)hostBuilder).ConfigureServices((context, services) =>
            {
                services.AddSingleton<IServer, NoopServer>();
                services.AddSingleton<IHostLifetime, NoopHostLifetime>();
            });
        }

        // Register a TCS to be invoked when the entrypoint (aka Program.Main)
        // has finished running. For minimal APIs, this means that all app.X
        // calls about the host has been built have been executed.
        var waitForStartTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnEntryPointExit(Exception exception)
        {
            // If the entry point exited, we'll try to complete the wait
            if (exception != null)
            {
                waitForStartTcs.TrySetException(exception);
            }
            else
            {
                waitForStartTcs.TrySetResult(null);
            }
        }

        // Resolve the host factory, ensuring that we don't stop the
        // application after the host has been built.
        var factory = HostFactoryResolver.ResolveHostFactory(assembly,
            stopApplication: false,
            configureHostBuilder: ConfigureHostBuilder,
            entrypointCompleted: OnEntryPointExit);

        if (factory == null)
        {
            _reporter.WriteError(Resources.FormatMethodsNotFound(
                HostFactoryResolver.BuildWebHost,
                HostFactoryResolver.CreateHostBuilder,
                HostFactoryResolver.CreateWebHostBuilder,
                entryPointType));

            return 8;
        }

        try
        {
            // Retrieve the service provider from the target host.
            var services = ((IHost)factory([$"--{HostDefaults.ApplicationKey}={assemblyName}"])).Services;
            if (services == null)
            {
                _reporter.WriteError(Resources.FormatServiceProviderNotFound(
                    typeof(IServiceProvider),
                    HostFactoryResolver.BuildWebHost,
                    HostFactoryResolver.CreateHostBuilder,
                    HostFactoryResolver.CreateWebHostBuilder,
                    entryPointType));

                return 9;
            }

            // Wait for the application to start to ensure that all configurations
            // on the WebApplicationBuilder have been processed.
            var applicationLifetime = services.GetRequiredService<IHostApplicationLifetime>();
            using (var registration = applicationLifetime.ApplicationStarted.Register(() => waitForStartTcs.TrySetResult(null)))
            {
                waitForStartTcs.Task.Wait();
                var success = GetDocuments(services);
                if (!success)
                {
                    return 10;
                }
            }
        }
        catch (Exception ex)
        {
            _reporter.WriteError(ex.ToString());
            return 11;
        }
#else
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
#endif

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
            _getDocumentsParameterTypes,
            _getDocumentsReturnType);
        if (getDocumentsMethod == null)
        {
            return false;
        }

        var generateWithVersionMethod = serviceType.GetMethod(
            GenerateMethodName,
            _generateWithVersionMethodParameterTypes);

        if (generateWithVersionMethod is not null)
        {
            if (generateWithVersionMethod.IsStatic)
            {
                _reporter.WriteWarning(Resources.FormatMethodIsStatic(GenerateMethodName, serviceType));
                generateWithVersionMethod = null;
            }

            if (!_generateMethodReturnType.IsAssignableFrom(generateWithVersionMethod.ReturnType))
            {
                _reporter.WriteWarning(
                    Resources.FormatMethodReturnTypeUnsupported(GenerateMethodName, serviceType, generateWithVersionMethod.ReturnType, _generateMethodReturnType));
                generateWithVersionMethod = null;

            }
        }

        var generateMethod = GetMethod(
            GenerateMethodName,
            serviceType,
            _generateMethodParameterTypes,
            _generateMethodReturnType);
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

        // Get document names
        var documentNames = (IEnumerable<string>)InvokeMethod(getDocumentsMethod, service, _getDocumentsArguments);
        if (documentNames == null)
        {
            return false;
        }

        // If an explicit document name is provided, then generate only that document.
        if (!string.IsNullOrEmpty(_context.DocumentName))
        {
            if (!documentNames.Contains(_context.DocumentName))
            {
                _reporter.WriteError(Resources.FormatDocumentNotFound(_context.DocumentName));
                return false;
            }

            documentNames = [_context.DocumentName];
        }

        if (!string.IsNullOrWhiteSpace(_context.FileName) && !Regex.IsMatch(_context.FileName, "^([A-Za-z0-9-_]+)$"))
        {
            _reporter.WriteError(Resources.FileNameFormatInvalid);
            return false;
        }

        // Write out the documents.
        var found = false;
        Directory.CreateDirectory(_context.OutputDirectory);
        var filePathList = new List<string>();
        var targetDocumentNames = string.IsNullOrEmpty(_context.DocumentName)
            ? documentNames
            : [_context.DocumentName];
        foreach (var documentName in targetDocumentNames)
        {
            var filePath = GetDocument(
                documentName,
                _context.ProjectName,
                _context.OutputDirectory,
                generateMethod,
                service,
                generateWithVersionMethod,
                _context.FileName);
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
        object service,
        MethodInfo? generateWithVersionMethod,
        string fileName)
    {
        _reporter.WriteInformation(Resources.FormatGeneratingDocument(documentName));

        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, _utf8EncodingWithoutBOM, bufferSize: 1024, leaveOpen: true))
        {
            var targetMethod = generateWithVersionMethod ?? generateMethod;
            object[] arguments = [documentName, writer];
            if (generateWithVersionMethod != null)
            {
                _reporter.WriteInformation(Resources.VersionedGenerateMethod);
                if (Enum.TryParse<OpenApiSpecVersion>(_context.OpenApiVersion, out var version))
                {
                    arguments = [documentName, writer, version];
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(_context.OpenApiVersion))
                    {
                        _reporter.WriteWarning(Resources.FormatInvalidOpenApiVersion(_context.OpenApiVersion));
                    }
                    arguments = [documentName, writer, OpenApiSpecVersion.OpenApi3_1];
                }
            }
            using var resultTask = (Task)InvokeMethod(targetMethod, service, arguments);
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

        fileName = !string.IsNullOrWhiteSpace(fileName) ? fileName : projectName;

        var filePath = GetDocumentPath(documentName, fileName, outputDirectory);
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

    private static string GetDocumentPath(string documentName, string fileName, string outputDirectory)
    {
        string path;

        if (string.Equals(DefaultDocumentName, documentName, StringComparison.Ordinal))
        {
            // Leave default document name out of the filename.
            path = fileName + JsonExtension;
        }
        else
        {
            // Sanitize the document name because it may contain almost any character, including illegal filename
            // characters such as '/' and '?' and the string "..". Do not treat slashes as folder separators.
            var sanitizedDocumentName = string.Join(
                UnderscoreString,
                documentName.Split(_invalidFilenameCharacters));

            while (sanitizedDocumentName.Contains(InvalidFilenameString))
            {
                sanitizedDocumentName = sanitizedDocumentName.Replace(InvalidFilenameString, DotString);
            }

            path = $"{fileName}_{documentName}{JsonExtension}";
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

#if NET7_0_OR_GREATER
    private sealed class NoopHostLifetime : IHostLifetime
    {
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoopServer : IServer
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();
        public void Dispose() { }
        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    }
#endif
}
