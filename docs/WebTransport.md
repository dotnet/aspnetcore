# Using WebTransport in Kestrel

Kestrel currently implements most of the WebTransport [draft-02](https://ietf-wg-webtrans.github.io/draft-ietf-webtrans-http3/draft-ietf-webtrans-http3.html) specification, except for datagrams. Datagrams will be implemented at a later date. This document outlines how to use the functionality.

# Running the sample app
To help applications get started on implementing WebTransport, there is the `WebTransportSampleApp` project. You can find it in `src\Servers\Kestrel\samples\WebTransportSampleApp`. To use it simply run it from VS. This will launch the server and a terminal which will show logs that Kestrel prints. Now you should be able to connect to it from any client that implements the standard WebTransport draft02 specification.

**Note:** Once you run the WebTransportSampleApp, it will print the certificate hash that it is using for the SSL connection. You will need to copy and paste it into your client to make sure that both the server and the client use the same one.

## Using Edge or Chrome DevTools as a client
The Chromium project has implemented a WebTransport client and can be accessed via their JS API from the Chrome or Edge DevTools console. A good sample app demoing how to use that API can be found [here](https://github.com/myjimmy/google-webtransport-sample/blob/ee13bde656c4d421d1f2a8e88fd71f572272c163/client.js).

# Overview of the Kestrel WebTransport API
## Setting up a connection
To setup a WebTransport connection, you will first need to configure a host upon which you open a port. A very minimal example is shown below:
```C#
public class Program
{
    public static void Main(string[] args)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseKestrel()
                .ConfigureKestrel((context, options) =>
                {
                    options.Listen([SOME IP ADDRESS], [SOME PORT], listenOptions =>
                    {
                        listenOptions.UseHttps([YOUR CERTIFICATE]);
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                    });
                })
                .UseStartup<Startup>();
            });
        var host = hostBuilder.Build();
        host.Run();
    }
}
```
**Note:** As WebTransport uses HTTP/3, you must make sure to select the `listenOptions.UseHttps` setting as well as set the `listenOptions.UseHttps` to include HTTP/3.

Next, we defined the `Startup` file. This file will setup the application layer logic that the server will use to accept and manage WebTransport sessions and streams.
```C#
public class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services) { }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var feature = context.Features.GetRequiredFeature<IHttpWebTransportFeature>();
            if (feature.IsWebTransportRequest)
            {
                var session = await feature.AcceptAsync(CancellationToken.None);

                // Do WebTransport stuff
            }
            else
            {
                await next(context);
            }
        });
    }
}
```
The `Configure` method is the main entry-point of your application logic. The `app.Use` block is triggered every time there is a connection request. Once the request is a WebTransport request (which is defined by getting the `IHttpWebTransportFeature` feature and then checking the `IsWebTransportRequest` property), you will be able to accept WebTransport sessions and interact with the client.

## Available WebTransport Features in Kestrel
This section highlights some of the most significant features of WebTransport that Kestrel implements. This is not an exhaustive list.

- Accept a WebTransport Session
```C#
var session = await feature.AcceptAsync(CancellationToken token);
```
This will await for the next incoming WebTransport session and return an instance of `IWebTransportSession` when a connection is completed. A session must be created prior to any streams being created or any data is sent. Note that only clients can initiate a session, thus the server passively waits until one is received and cannot initiate its own session. The cancellation token can be used to stop the operation.

- Accepting a WebTransport stream
```C#
var stream = await session.AcceptStreamAsync(CancellationToken token);
```
This will await for the next incoming WebTransport stream and return an instance of `WebTransportStream`. Note that streams are buffered in order and so this call pops from the front of the queue of pending streams. However, if no streams are pending, it will block until it receives one. You can use the cancellation token to stop the operation.

**Note:** This method will return both bidirectional and unidirectional streams. They can be distinguished based on the `stream.CanRead` and `stream.CanWrite` properties.

- Opening a new WebTransport stream from the server
```C#
var stream = await session.OpenUnidirectionalStreamAsync(CancellationToken token);
```
This will attempt to open a new unidirectional stream from the server to the client and return an instance of `WebTransportStream`. You can use the cancellation token to stop the operation.

- Sending data over a WebTransport stream
```C#
await stream.WriteAsync(ReadOnlyMemory<byte> bytes);
await stream.FlushAsync();
```
`stream.WriteAsync` will write data to the stream but it will not automatically flush (i.e. send it to the client). Therefore, after the `stream.WriteAsync`, you will need to call `stream.FlushAsync`.

**Note:** You can only send data on streams that have `stream.CanWrite` set as `true`. Sending data on non-writable streams will throw an `NotSupportedException` exception.

- Reading data from a WebTransport stream
```C#
var length = await stream.ReadAsync(Memory<byte> memory);
```
`stream.ReadAsync` will read data from the stream and copy it into the provided `memory` parameter. It will then return the number of bytes read.

**Note:** You can only read data from streams that have `stream.CanRead` set as `true`. Reading data on non-readable streams will throw an `NotSupportedException` exception.

- Aborting a WebTransport session
```C#
session.Abort(int errorCode = 256);
```
Aborting a WebTransport session will result in severing the connection with the client and aborting all the streams. You can optionally specify an error code that will be passed down into the logs. The default value (256) represents no error.

- Aborting a WebTransport stream
```C#
stream.Abort(int errorCode = 256);
```
Aborting a WebTransport stream will result in abruptly stopping all data transmission and prevent further communication over this stream. The default value (256) represents no error.

## Examples

### Example 1
This example waits for a bidirectional stream. Once it receives one, it will read the data from it, reverse it and then write it back to the stream.
```C#
public void Configure(IApplicationBuilder app)
    {
        var memory = new Memory<byte>(new byte[4096]);

        app.Use(async (context, next) =>
        {
            var feature = context.Features.GetRequiredFeature<IHttpWebTransportFeature>();
            if (feature.IsWebTransportRequest)
            {
                // accept a new session
                var session = await feature.AcceptAsync(CancellationToken.None);

                WebTransportStream stream;
                do
                {
                    // wait until we get a bidirectional stream
                    stream = await session.AcceptStreamAsync(CancellationToken.None);
                } while (stream.CanRead && stream.CanWrite);

                // read some data from the stream
                var length = await stream.ReadAsync(memory, CancellationToken.None);

                // do some operations on the contents of the data
                memory.Span.Reverse();

                // write back the data to the stream
                await stream.WriteAsync(memory, CancellationToken.None);
                await stream.FlushAsync(CancellationToken.None);
            }
            else
            {
                await next(context);
            }
        });
    }
```

### Example 2
This example opens a new stream from the server side and then sends data.
```C#
public void Configure(IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var feature = context.Features.GetRequiredFeature<IHttpWebTransportFeature>();
            if (feature.IsWebTransportRequest)
            {
                // accept a new session
                var session = await feature.AcceptAsync(CancellationToken.None);

                // open a new stream from the server to the client
                var stream = await session.OpenUnidirectionalStreamAsync(CancellationToken.None);

                // write data to the stream
                await stream.WriteAsync(new Memory<byte>(new byte[] { 65, 66, 67, 68, 69 }), CancellationToken.None);
                await stream.FlushAsync(CancellationToken.None);
            }
            else
            {
                await next(context);
            }
        });
    }
```
