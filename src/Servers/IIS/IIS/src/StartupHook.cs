// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Views;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.StackTrace.Sources;

internal class StartupHook
{
    public static void Initialize()
    {
        // TODO make this unhandled exception
        AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
        {
            var exception = eventArgs.Exception;

            var iisConfigData = NativeMethods.HttpGetApplicationProperties();
            var contentRoot = iisConfigData.pwzFullApplicationPath.TrimEnd(Path.DirectorySeparatorChar);

            var model = new ErrorPageModel
            {
                RuntimeDisplayName = RuntimeInformation.FrameworkDescription
            };
            var systemRuntimeAssembly = typeof(System.ComponentModel.DefaultValueAttribute).GetTypeInfo().Assembly;
            var assemblyVersion = new AssemblyName(systemRuntimeAssembly.FullName).Version.ToString();
            var clrVersion = assemblyVersion;
            model.RuntimeArchitecture = RuntimeInformation.ProcessArchitecture.ToString();
            var currentAssembly = typeof(ErrorPage).GetTypeInfo().Assembly;
            model.CurrentAssemblyVesion = currentAssembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;
            model.ClrVersion = clrVersion;
            model.OperatingSystemDescription = RuntimeInformation.OSDescription;

            var exceptionDetailProvider = new ExceptionDetailsProvider(
                new PhysicalFileProvider(contentRoot),
                sourceCodeLineCount: 6);

            model.ErrorDetails = exceptionDetailProvider.GetDetails(exception);

            var errorPage = new ErrorPage(model);
            var context = new IntermediateHttpContext();
            errorPage.ExecuteAsync(context).GetAwaiter().GetResult();
            context.Response.Body.Position = 0;
            var content = ((MemoryStream)context.Response.Body).ToArray();

            NativeMethods.HttpSetStartupErrorPageContent(content);
        };
    }

    private class IntermediateHttpContext : HttpContext
    {
        public IntermediateHttpContext()
        {
        }

        public override IFeatureCollection Features => throw new NotImplementedException();

        public override HttpRequest Request => null; 

        public override HttpResponse Response { get; } = new IntermediateResponse();

        public override ConnectionInfo Connection => throw new NotImplementedException();

        public override WebSocketManager WebSockets => throw new NotImplementedException();

        public override ClaimsPrincipal User { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override IDictionary<object, object> Items { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override IServiceProvider RequestServices { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override CancellationToken RequestAborted { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override string TraceIdentifier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override ISession Session { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Abort()
        {
            throw new NotImplementedException();
        }

        private class IntermediateResponse : HttpResponse
        {
            public override HttpContext HttpContext => throw new NotImplementedException();

            public override int StatusCode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override IHeaderDictionary Headers => throw new NotImplementedException();

            public override Stream Body { get; set; } = new MemoryStream();
            public override PipeWriter BodyWriter { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override long? ContentLength { get; set; }
            public override string ContentType { get; set; }

            public override IResponseCookies Cookies => throw new NotImplementedException();

            public override bool HasStarted => throw new NotImplementedException();

            public override void OnCompleted(Func<object, Task> callback, object state)
            {
                throw new NotImplementedException();
            }

            public override void OnStarting(Func<object, Task> callback, object state)
            {
                throw new NotImplementedException();
            }

            public override void Redirect(string location, bool permanent)
            {
                throw new NotImplementedException();
            }

            public override Task StartAsync(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
    }
}
