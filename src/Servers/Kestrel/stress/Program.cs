// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
using System.Web;
using System.Collections.Specialized;

/// <summary>
/// Simple HttpClient stress app that launches Kestrel in-proc and runs many concurrent requests of varying types against it.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var cmd = new RootCommand();
        cmd.AddOption(new Option("-n", "Max number of requests to make concurrently.") { Argument = new Argument<int>("numWorkers", Environment.ProcessorCount) });
        cmd.AddOption(new Option("-maxContentLength", "Max content length for request and response bodies.") { Argument = new Argument<int>("numBytes", 1000) });
        cmd.AddOption(new Option("-http", "HTTP version (1.1 or 2.0)") { Argument = new Argument<Version[]>("version", new[] { HttpVersion.Version20 }) });
        cmd.AddOption(new Option("-connectionLifetime", "Max connection lifetime length (milliseconds).") { Argument = new Argument<int?>("connectionLifetime", null)});
        cmd.AddOption(new Option("-ops", "Indices of the operations to use") { Argument = new Argument<int[]>("space-delimited indices", null) });
        cmd.AddOption(new Option("-trace", "Enable Microsoft-System-Net-Http tracing.") { Argument = new Argument<string>("\"console\" or path") });
        cmd.AddOption(new Option("-aspnetlog", "Enable ASP.NET warning and error logging.") { Argument = new Argument<bool>("enable", false) });
        cmd.AddOption(new Option("-listOps", "List available options.") { Argument = new Argument<bool>("enable", false) });
        cmd.AddOption(new Option("-seed", "Seed for generating pseudo-random parameters for a given -n argument.") { Argument = new Argument<int?>("seed", null)});
        cmd.AddOption(new Option("-numParameters", "Max number of query parameters or form fields for a request.") { Argument = new Argument<int>("queryParameters", 1) });
        cmd.AddOption(new Option("-cancelRate", "Number between 0 and 1 indicating rate of client-side request cancellation attempts. Defaults to 0.1.") { Argument = new Argument<double>("probability", 0.1) });
        cmd.AddOption(new Option("-httpSys", "Use http.sys instead of Kestrel.") { Argument = new Argument<bool>("enable", false) });

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

        Run(httpSys                 : cmdline.ValueForOption<bool>("-httpSys"),
            concurrentRequests      : cmdline.ValueForOption<int>("-n"),
            maxContentLength        : cmdline.ValueForOption<int>("-maxContentLength"),
            httpVersions            : cmdline.ValueForOption<Version[]>("-http"),
            connectionLifetime      : cmdline.ValueForOption<int?>("-connectionLifetime"),
            opIndices               : cmdline.ValueForOption<int[]>("-ops"),
            logPath                 : cmdline.HasOption("-trace") ? cmdline.ValueForOption<string>("-trace") : null,
            aspnetLog               : cmdline.ValueForOption<bool>("-aspnetlog"),
            listOps                 : cmdline.ValueForOption<bool>("-listOps"),
            seed                    : cmdline.ValueForOption<int?>("-seed") ?? new Random().Next(),
            numParameters           : cmdline.ValueForOption<int>("-numParameters"),
            cancellationProbability : Math.Max(0, Math.Min(1, cmdline.ValueForOption<double>("-cancelRate"))));
    }

    private static void Run(bool httpSys, int concurrentRequests, int maxContentLength, Version[] httpVersions, int? connectionLifetime, int[] opIndices, string logPath, bool aspnetLog, bool listOps, int seed, int numParameters, double cancellationProbability)
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
        const int DisplayIntervalMilliseconds = 1000;
        const int HttpsPort = 5001;
        const string LocalhostName = "localhost";
        string serverUri = $"https://{LocalhostName}:{HttpsPort}";
        int maxRequestLineSize = -1;

        // Validation of a response message
        void ValidateResponse(HttpResponseMessage m, Version expectedVersion)
        {
            if (m.Version != expectedVersion)
            {
                throw new Exception($"Expected response version {expectedVersion}, got {m.Version}");
            }
        }

        void ValidateContent(string expectedContent, string actualContent, string details = null)
        {
            if (actualContent != expectedContent)
            {
                throw new Exception($"Expected response content \"{expectedContent}\", got \"{actualContent}\". {details}");
            }
        }

        // Set of operations that the client can select from to run.  Each item is a tuple of the operation's name
        // and the delegate to invoke for it, provided with the HttpClient instance on which to make the call and
        // returning asynchronously the retrieved response string from the server.  Individual operations can be
        // commented out from here to turn them off, or additional ones can be added.
        var clientOperations = new (string, Func<RequestContext, Task>)[]
        {
            ("GET",
            async ctx =>
            {
                Version httpVersion = ctx.GetRandomVersion(httpVersions);
                using (var req = new HttpRequestMessage(HttpMethod.Get, serverUri) { Version = httpVersion })
                using (HttpResponseMessage m = await ctx.SendAsync(req))
                {
                    ValidateResponse(m, httpVersion);
                    ValidateContent(contentSource, await m.Content.ReadAsStringAsync());
                }
            }),

            // ("GET Partial",
            // async ctx =>
            // {
            //     Version httpVersion = ctx.GetRandomVersion(httpVersions);
            //     using (var req = new HttpRequestMessage(HttpMethod.Get, serverUri + "/slow") { Version = httpVersion })
            //     using (HttpResponseMessage m = await ctx.SendAsync(req, HttpCompletionOption.ResponseHeadersRead))
            //     {
            //         ValidateResponse(m, httpVersion);
            //         using (Stream s = await m.Content.ReadAsStreamAsync())
            //         {
            //             s.ReadByte(); // read single byte from response and throw the rest away
            //         }
            //     }
            // }),

            ("GET Headers",
            async ctx =>
            {
                Version httpVersion = ctx.GetRandomVersion(httpVersions);
                using (var req = new HttpRequestMessage(HttpMethod.Get, serverUri + "/headers") { Version = httpVersion })
                using (HttpResponseMessage m = await ctx.SendAsync(req))
                {
                    ValidateResponse(m, httpVersion);
                    ValidateContent(contentSource, await m.Content.ReadAsStringAsync());
                }
            }),

            ("GET Parameters",
            async ctx =>
            {
                Version httpVersion = ctx.GetRandomVersion(httpVersions);
                string uri = serverUri + "/variables";
                string expectedResponse = GetGetQueryParameters(ref uri, maxRequestLineSize, contentSource, ctx, numParameters);
                using (var req = new HttpRequestMessage(HttpMethod.Get, uri) { Version = httpVersion })
                using (HttpResponseMessage m = await ctx.SendAsync(req))
                {
                    ValidateResponse(m, httpVersion);
                    ValidateContent(expectedResponse, await m.Content.ReadAsStringAsync(), $"Uri: {uri}");
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
                        await ctx.SendAsync(req);
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
                                if (e.InnerException.Message.Contains("INTERNAL_ERROR") || // UseKestrel (https://github.com/aspnet/AspNetCore/issues/12256)
                                    e.InnerException.Message.Contains("CANCEL")) // UseHttpSys
                                {
                                    return;
                                }
                                break;
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
                using (HttpResponseMessage m = await ctx.SendAsync(req))
                {
                    ValidateResponse(m, httpVersion);
                    ValidateContent(content, await m.Content.ReadAsStringAsync());;
                }
            }),

            ("POST Multipart Data",
            async ctx =>
            {
                (string expected, MultipartContent formDataContent) formData = GetMultipartContent(contentSource, ctx, numParameters);
                Version httpVersion = ctx.GetRandomVersion(httpVersions);

                using (var req = new HttpRequestMessage(HttpMethod.Post, serverUri) { Version = httpVersion, Content = formData.formDataContent })
                using (HttpResponseMessage m = await ctx.SendAsync(req))
                {
                    ValidateResponse(m, httpVersion);
                    ValidateContent($"{formData.expected}", await m.Content.ReadAsStringAsync());;
                }
            }),

            ("POST Duplex",
            async ctx =>
            {
                string content = ctx.GetRandomSubstring(contentSource);
                Version httpVersion = ctx.GetRandomVersion(httpVersions);

                using (var req = new HttpRequestMessage(HttpMethod.Post, serverUri + "/duplex") { Version = httpVersion, Content = new StringDuplexContent(content) })
                using (HttpResponseMessage m = await ctx.SendAsync(req, HttpCompletionOption.ResponseHeadersRead))
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
                using (HttpResponseMessage m = await ctx.SendAsync(req, HttpCompletionOption.ResponseHeadersRead))
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
                    using (HttpResponseMessage m = await ctx.SendAsync(req, HttpCompletionOption.ResponseHeadersRead))
                    {
                        ValidateResponse(m, httpVersion);
                        ValidateContent(content, await m.Content.ReadAsStringAsync());
                    }
                }
            }),

            ("HEAD",
            async ctx =>
            {
                Version httpVersion = ctx.GetRandomVersion(httpVersions);
                using (var req = new HttpRequestMessage(HttpMethod.Head, serverUri) { Version = httpVersion })
                using (HttpResponseMessage m = await ctx.SendAsync(req))
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
                using (HttpResponseMessage m = await ctx.SendAsync(req))
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

        Console.WriteLine("       .NET Core: " + Path.GetFileName(Path.GetDirectoryName(typeof(object).Assembly.Location)));
        Console.WriteLine("    ASP.NET Core: " + Path.GetFileName(Path.GetDirectoryName(typeof(WebHost).Assembly.Location)));
        Console.WriteLine("          Server: " + (httpSys ? "http.sys" : "Kestrel"));
        Console.WriteLine("      Server URL: " + serverUri);
        Console.WriteLine("         Tracing: " + (logPath == null ? (object)false : logPath.Length == 0 ? (object)true : logPath));
        Console.WriteLine("     ASP.NET Log: " + aspnetLog);
        Console.WriteLine("     Concurrency: " + concurrentRequests);
        Console.WriteLine("  Content Length: " + maxContentLength);
        Console.WriteLine("   HTTP Versions: " + string.Join<Version>(", ", httpVersions));
        Console.WriteLine("        Lifetime: " + (connectionLifetime.HasValue ? $"{connectionLifetime}ms" : "(infinite)"));
        Console.WriteLine("      Operations: " + string.Join(", ", clientOperations.Select(o => o.Item1)));
        Console.WriteLine("     Random Seed: " + seed);
        Console.WriteLine("    Cancellation: " + 100 * cancellationProbability + "%");
        Console.WriteLine("Query Parameters: " + numParameters);
        Console.WriteLine();

        // Start the Kestrel web server in-proc.
        Console.WriteLine($"Starting {(httpSys ? "http.sys" : "Kestrel")} server.");
        IWebHostBuilder host = WebHost.CreateDefaultBuilder();
        if (httpSys)
        {
            // Use http.sys.  This requires additional manual configuration ahead of time;
            // see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys?view=aspnetcore-2.2#configure-windows-server.
            // In particular, you need to:
            // 1. Create a self-signed cert and install it into your local personal store, e.g. New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "cert:\LocalMachine\My"
            // 2. Pre-register the URL prefix, e.g. netsh http add urlacl url=https://localhost:5001/ user=Users
            // 3. Register the cert, e.g. netsh http add sslcert ipport=[::1]:5001 certhash=THUMBPRINTFROMABOVE appid="{some-guid}"
            host = host.UseHttpSys(hso =>
            {
                maxRequestLineSize = 8192;
                hso.UrlPrefixes.Add(serverUri);
                hso.Authentication.Schemes = Microsoft.AspNetCore.Server.HttpSys.AuthenticationSchemes.None;
                hso.Authentication.AllowAnonymous = true;
                hso.MaxConnections = null;
                hso.MaxRequestBodySize = null;
            });
        }
        else
        {
            // Use Kestrel, and configure it for HTTPS with a self-signed test certificate.
            host = host.UseKestrel(ko =>
            {
                maxRequestLineSize = ko.Limits.MaxRequestLineSize;
                ko.ListenLocalhost(HttpsPort, listenOptions =>
                {
                    // Create self-signed cert for server.
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
            });
        };

        // Output only warnings and errors from Kestrel
        host = host.ConfigureLogging(log => log.AddFilter("Microsoft.AspNetCore", level => aspnetLog ? level >= LogLevel.Warning : false))

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
                    endpoints.MapGet("/variables", async context =>
                    {
                        string queryString = context.Request.QueryString.Value;
                        NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(queryString);

                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < nameValueCollection.Count; i++)
                        {
                            sb.Append(nameValueCollection[$"Var{i}"]);
                        }

                        await context.Response.WriteAsync(sb.ToString());
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
            });

        host.Build()
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
        using (var client = new HttpClient(handler))
        {
            // Track all successes and failures
            long total = 0;
            long[] success = new long[clientOperations.Length], cancel = new long[clientOperations.Length], fail = new long[clientOperations.Length];
            long reuseAddressFailure = 0;

            void Increment(ref long counter)
            {
                Interlocked.Increment(ref counter);
                Interlocked.Increment(ref total);
            }

            // Spin up a thread dedicated to outputting stats for each defined interval
            new Thread(() =>
            {
                long lastTotal = 0;
                while (true)
                {
                    Thread.Sleep(DisplayIntervalMilliseconds);
                    lock (Console.Out)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("[" + DateTime.Now + "]");
                        Console.ResetColor();

                        if (lastTotal == total)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                        }
                        lastTotal = total;
                        Console.WriteLine(" Total: " + total.ToString("N0"));
                        Console.ResetColor();

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
                            Console.Write(success[i].ToString("N0"));
                            Console.ResetColor();
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write("\tCanceled: ");
                            Console.Write(cancel[i].ToString("N0"));
                            Console.ResetColor();
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
                // Creates a System.Random instance that is specific to the current client job
                // Generated using the global seed and the task index
                Random CreateRandomInstance()
                {
                    // deterministic hashing copied from System.Runtime.Hashing
                    int Combine(int h1, int h2)
                    {
                        uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
                        return ((int)rol5 + h1) ^ h2;
                    }

                    return new Random(Seed: Combine(taskNum, seed));
                }

                Random random = CreateRandomInstance();

                for (long i = taskNum; i < 500000; i++)
                {
                    long opIndex = i % clientOperations.Length;
                    (string operation, Func<RequestContext, Task> func) = clientOperations[opIndex];
                    var requestContext = new RequestContext(client, random, taskNum, cancellationProbability);
                    try
                    {
                        await func(requestContext);

                        Increment(ref success[opIndex]);
                    }
                    catch (OperationCanceledException) when (requestContext.IsCancellationRequested)
                    {
                        Increment(ref cancel[opIndex]);
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
                    throw new Exception("There was a failure in the stress run. See logs for exact time of failure.");
                }
            }
        }

        // Make sure our EventListener doesn't go away.
        GC.KeepAlive(listener);
    }

    private static string GetGetQueryParameters(ref string uri, int maxRequestLineSize, string contentSource, RequestContext clientContext, int numParameters)
    {
        if (maxRequestLineSize < uri.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRequestLineSize));
        }
        if (numParameters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numParameters));
        }

        var expectedString = new StringBuilder();
        var uriSb = new StringBuilder(uri);
        maxRequestLineSize -= uri.Length;

        int appxMaxValueLength = Math.Max(maxRequestLineSize / numParameters, 1);

        int num = clientContext.GetRandomInt32(1, numParameters + 1);
        for (int i = 0; i < num; i++)
        {
            string key = $"{(i == 0 ? "?" : "&")}Var{i}=";

            int remainingLength = maxRequestLineSize - uriSb.Length - key.Length;
            if (remainingLength <= 0)
            {
                break;
            }

            uriSb.Append(key);

            string value = clientContext.GetRandomString(Math.Min(appxMaxValueLength, remainingLength));
            expectedString.Append(value);
            uriSb.Append(value);
        }

        uri = uriSb.ToString();
        return expectedString.ToString();
    }

    private static (string, MultipartContent) GetMultipartContent(string contentSource, RequestContext clientContext, int numFormFields)
    {
        var multipartContent = new MultipartContent("prefix" + clientContext.GetRandomSubstring(contentSource), "test_boundary");
        StringBuilder sb = new StringBuilder();

        int num = clientContext.GetRandomInt32(1, numFormFields + 1);

        for (int i = 0; i < num; i++)
        {
            sb.Append("--test_boundary\r\nContent-Type: text/plain; charset=utf-8\r\n\r\n");
            string content = clientContext.GetRandomSubstring(contentSource);
            sb.Append(content);
            sb.Append("\r\n");
            multipartContent.Add(new StringContent(content));
        }

        sb.Append("--test_boundary--\r\n");
        return (sb.ToString(), multipartContent);
    }

    /// <summary>Client context containing information pertaining to a single request.</summary>
    private sealed class RequestContext
    {
        private readonly Random _random;
        private readonly HttpClient _client;
        private readonly double _cancellationProbability;

        public RequestContext(HttpClient httpClient, Random random, int taskNum, double cancellationProbability)
        {
            _random = random;
            _client = httpClient;
            _cancellationProbability = cancellationProbability;

            TaskNum = taskNum;
            IsCancellationRequested = false;
        }

        public int TaskNum { get; }
        public bool IsCancellationRequested { get; set; }

        // HttpClient.SendAsync() wrapper that wires randomized cancellation
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption httpCompletion = HttpCompletionOption.ResponseContentRead, CancellationToken? token = null)
        {
            if (token != null)
            {
                // user-supplied cancellation token overrides random cancellation
                return await _client.SendAsync(request, httpCompletion, token.Value);
            }
            else if (GetRandomBoolean(_cancellationProbability))
            {
                // trigger a random cancellation
                using (var cts = new CancellationTokenSource())
                {
                    int delayMs = _random.Next(0, 2);
                    Task<HttpResponseMessage> task = _client.SendAsync(request, httpCompletion, cts.Token);
                    if (delayMs > 0)
                        await Task.Delay(delayMs);

                    cts.Cancel();
                    IsCancellationRequested = true;
                    return await task;
                }
            }
            else
            {
                // no cancellation
                return await _client.SendAsync(request, httpCompletion);
            }
        }

        public string GetRandomString(int maxLength)
        {
            int length = _random.Next(0, maxLength);
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append((char)(_random.Next(0, 26) + 'a'));
            }
            return sb.ToString();
        }

        public string GetRandomSubstring(string input)
        {
            int offset = _random.Next(0, input.Length);
            int length = _random.Next(0, input.Length - offset + 1);
            return input.Substring(offset, length);
        }

        public bool GetRandomBoolean(double probability = 0.5)
        {
            if (probability < 0 || probability > 1)
                throw new ArgumentOutOfRangeException(nameof(probability));

            return _random.NextDouble() < probability;
        }

        public int GetRandomInt32(int minValueInclusive, int maxValueExclusive) => _random.Next(minValueInclusive, maxValueExclusive);

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