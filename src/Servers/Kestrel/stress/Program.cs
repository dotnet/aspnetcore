// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;
using System.Text;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// Simple HttpClient stress app that launches Kestrel in-proc and runs many concurrent requests of varying types against it.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var cmd = new RootCommand();
        cmd.AddOption(new Option("-n", "Max number of requests to make concurrently.") { Argument = new Argument<int>("numWorkers", 1) });
        cmd.AddOption(new Option("-maxContentLength", "Max content length for request and response bodies.") { Argument = new Argument<int>("numBytes", 1000) });
        cmd.AddOption(new Option("-http", "HTTP version (1.1 or 2.0)") { Argument = new Argument<Version[]>("version", new[] { HttpVersion.Version20 }) });
        cmd.AddOption(new Option("-connectionLifetime", "Max connection lifetime length (milliseconds).") { Argument = new Argument<int?>("connectionLifetime", null)});
        cmd.AddOption(new Option("-ops", "Indices of the operations to use") { Argument = new Argument<int[]>("space-delimited indices", null) });
        cmd.AddOption(new Option("-trace", "Enable Microsoft-System-Net-Http tracing.") { Argument = new Argument<string>("\"console\" or path") });
        cmd.AddOption(new Option("-aspnetlog", "Enable ASP.NET warning and error logging.") { Argument = new Argument<bool>("enable", false) });
        cmd.AddOption(new Option("-listOps", "List available options.") { Argument = new Argument<bool>("enable", false) });
        cmd.AddOption(new Option("-seed", "Seed for generating pseudo-random parameters for a given -n argument.") { Argument = new Argument<int?>("seed", null)});

        ParseResult cmdline = cmd.Parse(args);
        if (cmdline.Errors.Count > 0)
        {
            foreach (ParseError error in cmdline.Errors)
            {
                Console.WriteLine(error);
            }
            Console.WriteLine();
            new HelpBuilder(new SystemConsole()).Write(cmd);
            return;
        }

        Run(concurrentRequests  : cmdline.ValueForOption<int>("-n"),
            maxContentLength    : cmdline.ValueForOption<int>("-maxContentLength"),
            httpVersions        : cmdline.ValueForOption<Version[]>("-http"),
            connectionLifetime  : cmdline.ValueForOption<int?>("-connectionLifetime"),
            opIndices           : cmdline.ValueForOption<int[]>("-ops"),
            logPath             : cmdline.HasOption("-trace") ? cmdline.ValueForOption<string>("-trace") : null,
            aspnetLog           : cmdline.ValueForOption<bool>("-aspnetlog"),
            listOps             : cmdline.ValueForOption<bool>("-listOps"),
            seed                : cmdline.ValueForOption<int?>("-seed") ?? new Random().Next());
    }

    private static void Run(int concurrentRequests, int maxContentLength, Version[] httpVersions, int? connectionLifetime, int[] opIndices, string logPath, bool aspnetLog, bool listOps, int seed)
    {
        // Handle command-line arguments.
        EventListener listener =
            logPath == null ? null :
            new HttpEventListener(logPath != "console" ? new StreamWriter(logPath) { AutoFlush = true } : null);
        // if (listener == null)
        // {
        //     // If no command-line requested logging, enable the user to press 'L' to enable logging to the console
        //     // during execution, so that it can be done just-in-time when something goes awry.
        //     new Thread(() =>
        //     {
        //         while (true)
        //         {
        //             if (Console.ReadKey(intercept: true).Key == ConsoleKey.L)
        //             {
        //                 listener = new HttpEventListener();
        //                 break;
        //             }
        //         }
        //     }) { IsBackground = true }.Start();
        // }

        string contentSource = string.Concat(Enumerable.Repeat("1234567890", maxContentLength / 10));
        const int DisplayIntervalMilliseconds = 10000;
        const int HttpsPort = 5001;
        const string LocalhostName = "localhost";
        string serverUri = $"https://{LocalhostName}:{HttpsPort}";

        // Validation of a response message
        void ValidateResponse(HttpResponseMessage m, Version expectedVersion)
        {
            if (m.Version != expectedVersion)
            {
                throw new Exception($"Expected response version {expectedVersion}, got {m.Version}");
            }
        }

        void ValidateContent(string expectedContent, string actualContent)
        {
            if (actualContent != expectedContent)
            {
                throw new Exception($"Expected response content \"{expectedContent}\", got \"{actualContent}\"");
            }
        }

        // Set of operations that the client can select from to run.  Each item is a tuple of the operation's name
        // and the delegate to invoke for it, provided with the HttpClient instance on which to make the call and
        // returning asynchronously the retrieved response string from the server.  Individual operations can be
        // commented out from here to turn them off, or additional ones can be added.
        var clientOperations = new (string, Func<ClientContext, Task>)[]
        {
            ("GET",
            async ctx =>
            {
                Version httpVersion = ctx.GetRandomVersion(httpVersions);
                using (var req = new HttpRequestMessage(HttpMethod.Get, serverUri) { Version = httpVersion })
                using (HttpResponseMessage m = await ctx.HttpClient.SendAsync(req))
                {
                    ValidateResponse(m, httpVersion);
                    ValidateContent(contentSource, await m.Content.ReadAsStringAsync());
                }
            }),

            // TODO re-enable after HttpClient fixes. https://github.com/dotnet/corefx/issues/39461
            //("GET Partial",
            //async ctx =>
            //{
            //    Version httpVersion = ctx.GetRandomVersion(httpVersions);
            //    using (var req = new HttpRequestMessage(HttpMethod.Get, serverUri + "/slow") { Version = httpVersion })
            //    using (HttpResponseMessage m = await ctx.HttpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead))
            //    {
            //        ValidateResponse(m, httpVersion);
            //        var buffer = new byte[1];
            //        using (Stream s = await m.Content.ReadAsStreamAsync())
            //        {
            //            await s.ReadAsync(buffer); // read single byte from response and throw the rest away
            //        }
            //    }
            //}),

            ("GET Headers",
            async ctx =>
            {
                Version httpVersion = ctx.GetRandomVersion(httpVersions);
                using (var req = new HttpRequestMessage(HttpMethod.Get, serverUri + "/headers") { Version = httpVersion })
                using (HttpResponseMessage m = await ctx.HttpClient.SendAsync(req))
                {
                    ValidateResponse(m, httpVersion);
                    ValidateContent(contentSource, await m.Content.ReadAsStringAsync());
                }
            }),

            ("GET Cancellation",
            async ctx =>
            {
                Version httpVersion = ctx.GetRandomVersion(httpVersions);
                using (var req = new HttpRequestMessage(HttpMethod.Get, serverUri) { Version = httpVersion })
                {
                    var cts = new CancellationTokenSource();
                    Task<HttpResponseMessage> t = ctx.HttpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    await Task.Delay(1);
                    cts.Cancel();
                    try
                    {
                        using (HttpResponseMessage m = await t)
                        {
                            ValidateResponse(m, httpVersion);
                            ValidateContent(contentSource, await m.Content.ReadAsStringAsync());
                        }
                    }
                    catch (OperationCanceledException) { }
                }
            }),

            ("GET Aborted",
            async ctx =>
            {
                Version httpVersion = ctx.GetRandomVersion(httpVersions);
                try
                {
                    using (var req = new HttpRequestMessage(HttpMethod.Get, serverUri + "/abort") { Version = httpVersion })
                    {
                        await ctx.HttpClient.SendAsync(req);
                    }
                    throw new Exception("Completed unexpectedly");
                }
                catch (Exception e)
                {
                    if (e is HttpRequestException hre && hre.InnerException is IOException)
                    {
                        e = hre.InnerException;
                    }

                    if (e is IOException ioe)
                    {
                        if (httpVersion < HttpVersion.Version20)
                        {
                            return;
                        }

                        string name = e.InnerException?.GetType().Name;
                        switch (name)
                        {
                            case "Http2ProtocolException":
                            case "Http2ConnectionException":
                            case "Http2StreamException":
                                if (e.InnerException.Message.Contains("INTERNAL_ERROR") || e.InnerException.Message.Contains("CANCEL"))
                                {
                                    return;
                                }
                                break;
                            case "WinHttpException":
                                return;
                        }
                    }

                    throw;
                }
            }),

            ("POST",
            async ctx =>
            {
                string content = ctx.GetRandomSubstring(contentSource);
                Version httpVersion = ctx.GetRandomVersion(httpVersions);

                using (var req = new HttpRequestMessage(HttpMethod.Post, serverUri) { Version = httpVersion, Content = new StringDuplexContent(content) })
                using (HttpResponseMessage m = await ctx.HttpClient.SendAsync(req))
                {
                    ValidateResponse(m, httpVersion);
                    ValidateContent(content, await m.Content.ReadAsStringAsync());
                }
            }),

            ("POST Duplex",
            async ctx =>
            {
                string content = ctx.GetRandomSubstring(contentSource);
                Version httpVersion = ctx.GetRandomVersion(httpVersions);

                using (var req = new HttpRequestMessage(HttpMethod.Post, serverUri + "/duplex") { Version = httpVersion, Content = new StringDuplexContent(content) })
                using (HttpResponseMessage m = await ctx.HttpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead))
                {
                    ValidateResponse(m, httpVersion);
                    ValidateContent(content, await m.Content.ReadAsStringAsync());
                }
            }),

            ("POST Duplex Slow",
            async ctx =>
            {
                string content = ctx.GetRandomSubstring(contentSource);
                Version httpVersion = ctx.GetRandomVersion(httpVersions);

                using (var req = new HttpRequestMessage(HttpMethod.Post, serverUri + "/duplexSlow") { Version = httpVersion, Content = new ByteAtATimeNoLengthContent(Encoding.ASCII.GetBytes(content)) })
                using (HttpResponseMessage m = await ctx.HttpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead))
                {
                    ValidateResponse(m, httpVersion);
                    ValidateContent(content, await m.Content.ReadAsStringAsync());
                }
            }),

            ("POST ExpectContinue",
            async ctx =>
            {
                string content = ctx.GetRandomSubstring(contentSource);
                Version httpVersion = ctx.GetRandomVersion(httpVersions);

                using (var req = new HttpRequestMessage(HttpMethod.Post, serverUri) { Version = httpVersion, Content = new StringContent(content) })
                {
                    req.Headers.ExpectContinue = true;
                    using (HttpResponseMessage m = await ctx.HttpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead))
                    {
                        ValidateResponse(m, httpVersion);
                        ValidateContent(content, await m.Content.ReadAsStringAsync());
                    }
                }
            }),

            ("POST Cancellation",
            async ctx =>
            {
                string content = ctx.GetRandomSubstring(contentSource);
                Version httpVersion = ctx.GetRandomVersion(httpVersions);

                using (var req = new HttpRequestMessage(HttpMethod.Post, serverUri) { Version = httpVersion, Content = new StringContent(content) })
                {
                    var cts = new CancellationTokenSource();
                    req.Content = new CancelableContent(cts.Token);
                    Task<HttpResponseMessage> t = ctx.HttpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    await Task.Delay(1);
                    cts.Cancel();
                    try
                    {
                        using (HttpResponseMessage m = await t)
                        {
                            ValidateResponse(m, httpVersion);
                            ValidateContent(content, await m.Content.ReadAsStringAsync());
                        }
                    }
                    catch (OperationCanceledException) { }
                }
            }),

            ("HEAD",
            async ctx =>
            {
                Version httpVersion = ctx.GetRandomVersion(httpVersions);
                using (var req = new HttpRequestMessage(HttpMethod.Head, serverUri) { Version = httpVersion })
                using (HttpResponseMessage m = await ctx.HttpClient.SendAsync(req))
                {
                    ValidateResponse(m, httpVersion);
                    if (m.Content.Headers.ContentLength != maxContentLength)
                    {
                        throw new Exception($"Expected {maxContentLength}, got {m.Content.Headers.ContentLength}");
                    }
                    string r = await m.Content.ReadAsStringAsync();
                    if (r.Length > 0) throw new Exception($"Got unexpected response: {r}");
                }
            }),

            ("PUT",
            async ctx =>
            {
                string content = ctx.GetRandomSubstring(contentSource);
                Version httpVersion = ctx.GetRandomVersion(httpVersions);

                using (var req = new HttpRequestMessage(HttpMethod.Put, serverUri) { Version = httpVersion, Content = new StringContent(content) })
                using (HttpResponseMessage m = await ctx.HttpClient.SendAsync(req))
                {
                    ValidateResponse(m, httpVersion);
                    string r = await m.Content.ReadAsStringAsync();
                    if (r != "") throw new Exception($"Got unexpected response: {r}");
                }
            }),
        };

        if (listOps)
        {
            for (int i = 0; i < clientOperations.Length; i++)
            {
                Console.WriteLine($"{i} = {clientOperations[i].Item1}");
            }
            return;
        }

        if (opIndices != null)
        {
            clientOperations = opIndices.Select(i => clientOperations[i]).ToArray();
        }

        Console.WriteLine("     .NET Core: " + Path.GetFileName(Path.GetDirectoryName(typeof(object).Assembly.Location)));
        Console.WriteLine("  ASP.NET Core: " + Path.GetFileName(Path.GetDirectoryName(typeof(WebHost).Assembly.Location)));
        Console.WriteLine("       Tracing: " + (logPath == null ? (object)false : logPath.Length == 0 ? (object)true : logPath));
        Console.WriteLine("   ASP.NET Log: " + aspnetLog);
        Console.WriteLine("   Concurrency: " + concurrentRequests);
        Console.WriteLine("Content Length: " + maxContentLength);
        Console.WriteLine(" HTTP Versions: " + string.Join<Version>(", ", httpVersions));
        Console.WriteLine("      Lifetime: " + (connectionLifetime.HasValue ? $"{connectionLifetime}ms" : "(infinite)"));
        Console.WriteLine("    Operations: " + string.Join(", ", clientOperations.Select(o => o.Item1)));
        Console.WriteLine("   Random Seed: " + seed);
        Console.WriteLine();

        // Start the Kestrel web server in-proc.
        Console.WriteLine("Starting server.");
        WebHost.CreateDefaultBuilder()

            //Use Kestrel, and configure it for HTTPS with a self - signed test certificate.
            .UseKestrel(ko =>
            {
                ko.ListenLocalhost(HttpsPort, listenOptions =>
                {
                    using (RSA rsa = RSA.Create())
                    {
                        var certReq = new CertificateRequest($"CN={LocalhostName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                        certReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                        certReq.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
                        certReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
                        X509Certificate2 cert = certReq.CreateSelfSigned(DateTimeOffset.UtcNow.AddMonths(-1), DateTimeOffset.UtcNow.AddMonths(1));
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            cert = new X509Certificate2(cert.Export(X509ContentType.Pfx));
                        }
                        listenOptions.UseHttps(cert);
                    }
                });
            })

            // Output only warnings and errors from Kestrel
            .ConfigureLogging(log => log.AddFilter("Microsoft.AspNetCore", level => aspnetLog ? level >= LogLevel.Error : false))

            // Set up how each request should be handled by the server.
            .Configure(app =>
            {
                var head = new[] { "HEAD" };
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/", async context =>
                    {
                        // Get requests just send back the requested content.
                        await context.Response.WriteAsync(contentSource);
                    });
                    endpoints.MapGet("/slow", async context =>
                    {
                        // Sends back the content a character at a time.
                        for (int i = 0; i < contentSource.Length; i++)
                        {
                            await context.Response.WriteAsync(contentSource[i].ToString());
                            await context.Response.Body.FlushAsync();
                        }
                    });
                    endpoints.MapGet("/headers", async context =>
                    {
                        // Get request but with a bunch of extra headers
                        for (int i = 0; i < 20; i++)
                        {
                            context.Response.Headers.Add(
                                "CustomHeader" + i,
                                new StringValues(Enumerable.Range(0, i).Select(id => "value" + id).ToArray()));
                        }
                        await context.Response.WriteAsync(contentSource);
                        if (context.Response.SupportsTrailers())
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                context.Response.AppendTrailer(
                                    "CustomTrailer" + i,
                                    new StringValues(Enumerable.Range(0, i).Select(id => "value" + id).ToArray()));
                            }
                        }
                    });
                    endpoints.MapGet("/abort", async context =>
                    {
                        // Server writes some content, then aborts the connection
                        await context.Response.WriteAsync(contentSource.Substring(0, contentSource.Length / 2));
                        context.Abort();
                    });
                    endpoints.MapPost("/", async context =>
                    {
                        // Post echos back the requested content, first buffering it all server-side, then sending it all back.
                        var s = new MemoryStream();
                        await context.Request.Body.CopyToAsync(s);
                        s.Position = 0;
                        await s.CopyToAsync(context.Response.Body);
                    });
                    endpoints.MapPost("/duplex", async context =>
                    {
                        // Echos back the requested content in a full duplex manner.
                        await context.Request.Body.CopyToAsync(context.Response.Body);
                    });
                    endpoints.MapPost("/duplexSlow", async context =>
                    {
                        // Echos back the requested content in a full duplex manner, but one byte at a time.
                        var buffer = new byte[1];
                        while ((await context.Request.Body.ReadAsync(buffer)) != 0)
                        {
                            await context.Response.Body.WriteAsync(buffer);
                        }
                    });
                    endpoints.MapMethods("/", head, context =>
                    {
                        // Just set the max content length on the response.
                        context.Response.Headers.ContentLength = maxContentLength;
                        return Task.CompletedTask;
                    });
                    endpoints.MapPut("/", async context =>
                    {
                        // Read the full request but don't send back a response body.
                        await context.Request.Body.CopyToAsync(Stream.Null);
                    });
                });
            })
            .Build()
            .Start();

        // Start the client.
        Console.WriteLine($"Starting {concurrentRequests} client workers.");
        var handler = new SocketsHttpHandler()
        {
            PooledConnectionLifetime = connectionLifetime.HasValue ? TimeSpan.FromMilliseconds(connectionLifetime.Value) : Timeout.InfiniteTimeSpan,
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = delegate { return true; }
            }
        };
        //var handler = new WinHttpHandler()
        //{
        //    ServerCertificateValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        //};

        using (var client = new HttpClient(handler))
        {
            // Track all successes and failures
            long total = 0;
            long[] success = new long[clientOperations.Length], fail = new long[clientOperations.Length];
            long reuseAddressFailure = 0;

            void Increment(ref long counter)
            {
                Interlocked.Increment(ref counter);
                Interlocked.Increment(ref total);
            }

            // Spin up a thread dedicated to outputting stats for each defined interval
            new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(DisplayIntervalMilliseconds);
                    lock (Console.Out)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("[" + DateTime.Now + "]");
                        Console.ResetColor();
                        Console.WriteLine(" Total: " + total.ToString("N0"));

                        if (reuseAddressFailure > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.WriteLine("~~ Reuse address failures: " + reuseAddressFailure.ToString("N0") + "~~");
                            Console.ResetColor();
                        }

                        for (int i = 0; i < clientOperations.Length; i++)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write("\t" + clientOperations[i].Item1.PadRight(30));
                            Console.ResetColor();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("Success: ");
                            Console.ResetColor();
                            Console.Write(success[i].ToString("N0"));
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.Write("\tFail: ");
                            Console.ResetColor();
                            Console.WriteLine(fail[i].ToString("N0"));
                        }
                        Console.WriteLine();
                    }
                }
            })
            { IsBackground = true }.Start();

            // Start N workers, each of which sits in a loop making requests.
            Task.WaitAll(Enumerable.Range(0, concurrentRequests).Select(taskNum => Task.Run(async () =>
            {
                var clientContext = new ClientContext(client, taskNum: taskNum, seed: seed);
                // TODO make 50000 configurable based on time.
                for (long i = taskNum; i < 500000; i++)
                {
                    long opIndex = i % clientOperations.Length;
                    (string operation, Func<ClientContext, Task> func) = clientOperations[opIndex];
                    try
                    {
                        await func(clientContext);

                        Increment(ref success[opIndex]);
                    }
                    catch (Exception e)
                    {
                        Increment(ref fail[opIndex]);

                        if (e is HttpRequestException hre && hre.InnerException is SocketException se && se.SocketErrorCode == SocketError.AddressAlreadyInUse)
                        {
                            Interlocked.Increment(ref reuseAddressFailure);
                        }
                        else
                        {
                            lock (Console.Out)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"Error from iteration {i} ({operation}) in task {taskNum} with {success.Sum()} successes / {fail.Sum()} fails:");
                                Console.ResetColor();
                                Console.WriteLine(e);
                                Console.WriteLine();
                            }
                        }
                    }
                }
            })).ToArray());

            for (var i = 0; i < fail.Length; i++)
            {
                if (fail[i] > 0)
                {
                    throw new Exception("There was a failure in the stress run. See logs for exact time of failure");
                }
            }
        }

        // Make sure our EventListener doesn't go away.
        GC.KeepAlive(listener);
    }

    /// <summary>Client context containing information pertaining to a single worker.</summary>
    private sealed class ClientContext
    {
        private readonly Random _random;

        public ClientContext(HttpClient httpClient, int taskNum, int seed)
        {
            _random = new Random(Combine(seed, taskNum)); // derived from global seed and worker number
            TaskNum = taskNum;
            HttpClient = httpClient;

            // deterministic hashing copied from System.Runtime.Hashing
            int Combine(int h1, int h2)
            {
                uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
                return ((int)rol5 + h1) ^ h2;
            }
        }
        public int TaskNum { get; }

        public HttpClient HttpClient { get; }

        public string GetRandomSubstring(string input)
        {
            int offset = _random.Next(0, input.Length);
            int length = _random.Next(0, input.Length - offset + 1);
            return input.Substring(offset, length);
        }

        public Version GetRandomVersion(Version[] versions) =>
            versions[_random.Next(0, versions.Length)];
    }

    /// <summary>HttpContent that partially serializes and then waits for cancellation to be requested.</summary>
    private sealed class CancelableContent : HttpContent
    {
        private readonly CancellationToken _cancellationToken;

        public CancelableContent(CancellationToken cancellationToken) => _cancellationToken = cancellationToken;

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            await stream.WriteAsync(new byte[] { 1, 2, 3 });

            var tcs = new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously);
            using (_cancellationToken.Register(() => tcs.SetResult(true)))
            {
                await tcs.Task.ConfigureAwait(false);
            }

            _cancellationToken.ThrowIfCancellationRequested();
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 42;
            return true;
        }
    }

    /// <summary>HttpContent that's similar to StringContent but that can be used with HTTP/2 duplex communication.</summary>
    private sealed class StringDuplexContent : HttpContent
    {
        private readonly byte[] _data;

        public StringDuplexContent(string value) => _data = Encoding.UTF8.GetBytes(value);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) =>
            stream.WriteAsync(_data, 0, _data.Length);

        protected override bool TryComputeLength(out long length)
        {
            length = _data.Length;
            return true;
        }
    }

    /// <summary>HttpContent that trickles out a byte at a time.</summary>
    private sealed class ByteAtATimeNoLengthContent : HttpContent
    {
        private readonly byte[] _buffer;

        public ByteAtATimeNoLengthContent(byte[] buffer) => _buffer = buffer;

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            for (int i = 0; i < _buffer.Length; i++)
            {
                await stream.WriteAsync(_buffer.AsMemory(i, 1));
                await stream.FlushAsync();
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    /// <summary>EventListener that dumps HTTP events out to either the console or a stream writer.</summary>
    private sealed class HttpEventListener : EventListener
    {
        private readonly StreamWriter _writer;

        public HttpEventListener(StreamWriter writer = null) => _writer = writer;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Microsoft-System-Net-Http")
                EnableEvents(eventSource, EventLevel.LogAlways);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            lock (Console.Out)
            {
                if (_writer != null)
                {
                    var sb = new StringBuilder().Append($"[{eventData.EventName}] ");
                    for (int i = 0; i < eventData.Payload.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(eventData.PayloadNames[i]).Append(": ").Append(eventData.Payload[i]);
                    }
                    _writer.WriteLine(sb);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write($"[{eventData.EventName}] ");
                    Console.ResetColor();
                    for (int i = 0; i < eventData.Payload.Count; i++)
                    {
                        if (i > 0) Console.Write(", ");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(eventData.PayloadNames[i] + ": ");
                        Console.ResetColor();
                        Console.Write(eventData.Payload[i]);
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}
