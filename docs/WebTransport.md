# Using WebTransport in Kestrel

Kestrel currently implements most of the WebTransport [draft-02](https://ietf-wg-webtrans.github.io/draft-ietf-webtrans-http3/draft-ietf-webtrans-http3.html) specification, except for datagrams. Datagrams will be implemented at a later date. This document outlines how to use the already implemented functionality.

## Running the sample apps

To help applications get started on implementing WebTransport, there are two sample apps.

- ### `WebTransportSampleApp` project located at `src\Servers\Kestrel\samples\WebTransportSampleApp`
To use it, simply run from VS. This will launch the server and a terminal which will show logs from Kestrel as it interacts with the client. Now you should be able to connect to the sample from any client that implements the standard WebTransport draft02 specification.

**Note:** Once you run the `WebTransportSampleApp`, it will print the certificate hash that it is using for the SSL connection. You will need to copy it into your client to make sure that both the server and the client use the same one.

- ### `WebTransportInteractiveSampleApp` project located at `src\Middleware\WebTransport\samples\WebTransportInteractiveSampleApp`
To use it, simply run from VS. This will launch the server and terminal. Now you can open any browser that supports WebTransport and navigate to `https://localhost:5001`. You will see an interactive WebTransport test page where you can interact with the API and most of its main functionalities.

**Note:** this sample automatically injects the certificate into the client-side code. Therefore, you do not need to handle it manually.

## Using Edge or Chrome DevTools as a client

The Chromium project has implemented a WebTransport client and can be accessed via their JS API from the Chrome or Edge DevTools console. A good sample app demonstrating how to use that API can be found [here](https://github.com/myjimmy/google-webtransport-sample/blob/ee13bde656c4d421d1f2a8e88fd71f572272c163/client.js).

## Note about preview features

WebTransport is a preview feature. Therefore, you must manually enable it via the `EnablePreviewFeatures` property and toggle the `Microsoft.AspNetCore.Server.Kestrel.Experimental.WebTransportAndH3Datagrams` `RuntimeHostConfigurationOption`. This can be done by adding the following `ItemGroup` to your csproj file:
```xml
<ItemGroup>
    <RuntimeHostConfigurationOption Include="Microsoft.AspNetCore.Server.Kestrel.Experimental.WebTransportAndH3Datagrams" Value="true" />
</ItemGroup>
```

## Obtaining a test certificate

The current Kestrel default testing certificate cannot be used for WebTransport connections as it does not meet the requirements needed for WebTransport over HTTP/3. You can generate a new certificate for testing via the following C# (this function will also automatically handle cert rotation every time one expires):
```C#
static X509Certificate2 GenerateManualCertificate()
{
    X509Certificate2 cert = null;
    var store = new X509Store("KestrelWebTransportCertificates", StoreLocation.CurrentUser);
    store.Open(OpenFlags.ReadWrite);
    if (store.Certificates.Count > 0)
    {
        cert = store.Certificates[^1];

        // rotate key after it expires
        if (DateTime.Parse(cert.GetExpirationDateString(), null) < DateTimeOffset.UtcNow)
        {
            cert = null;
        }
    }
    if (cert == null)
    {
        // generate a new cert
        var now = DateTimeOffset.UtcNow;
        SubjectAlternativeNameBuilder sanBuilder = new();
        sanBuilder.AddDnsName("localhost");
        using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        CertificateRequest req = new("CN=localhost", ec, HashAlgorithmName.SHA256);
        // Adds purpose
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection
        {
            new("1.3.6.1.5.5.7.3.1") // serverAuth
        }, false));
        // Adds usage
        req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
        // Adds subject alternate names
        req.CertificateExtensions.Add(sanBuilder.Build());
        // Sign
        using var crt = req.CreateSelfSigned(now, now.AddDays(14)); // 14 days is the max duration of a certificate for this
        cert = new(crt.Export(X509ContentType.Pfx));

        // Save
        store.Add(cert);
    }
    store.Close();

    var hash = SHA256.HashData(cert.RawData);
    var certStr = Convert.ToBase64String(hash);
    Console.WriteLine($"\n\n\n\n\nCertificate: {certStr}\n\n\n\n"); // <-- you will need to put this output into the JS API call to allow the connection
    return cert;
}
// Adapted from: https://github.com/wegylexy/webtransport
```

## Overview of the Kestrel WebTransport API

### Setting up a connection

To setup a WebTransport connection, you will first need to configure a host upon which you open a port. A very minimal example is shown below:
```C#
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel((context, options) =>
{
    // Port configured for WebTransport
    options.Listen([SOME IP ADDRESS], [SOME PORT], listenOptions =>
    {
        listenOptions.UseHttps(GenerateManualCertificate());
        listenOptions.UseConnectionLogging();
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
    });
});
var host = builder.Build();
```
**Note:** As WebTransport uses HTTP/3, you must make sure to select the `listenOptions.UseHttps` setting as well as set the `listenOptions.Protocols` to include HTTP/3.

**Note:** The default Kestrel certificate cannot be used for WebTransport connections. For local testing you can use the workaround described in the [Obtaining a test certificate section](#obtaining-a-test-certificate).

Next, we defined the code that will run when Kestrel receives a connection.
```C#
host.Run(async (context) =>
{
    var feature = context.Features.GetRequiredFeature<IHttpWebTransportFeature>();
    if (!feature.IsWebTransportRequest)
    {
        return;
    }
    var session = await feature.AcceptAsync(CancellationToken.None);

    // Use WebTransport via the newly established session.
});

await host.RunAsync();
```
The `Run` method is the main entry-point of your application logic. It is triggered every time there is a connection request. Once the request is a WebTransport request (which is defined by getting the `IHttpWebTransportFeature` feature and then checking the `IsWebTransportRequest` property), you will be able to accept WebTransport sessions and interact with the client. The last line (`await host.RunAsync();`) will start the server and start accepting connections.

### Available WebTransport Features in Kestrel

This section highlights some of the most significant features of WebTransport that Kestrel implements. However, this is not an exhaustive list.

- Accept a WebTransport Session
```C#
var session = await feature.AcceptAsync(CancellationToken token);
```
This will wait for the next incoming WebTransport session and return an instance of `IWebTransportSession` when a connection is completed. A session must be created prior to any streams being created or any data is sent. Note that only clients can initiate a session, thus the server passively waits until one is received and cannot initiate its own session. The cancellation token can be used to stop the operation.

- Accepting a WebTransport stream
```C#
var connectionContext = await session.AcceptStreamAsync(CancellationToken token);
```
This will wait for the next incoming WebTransport stream and return an instance of `ConnectionContext`. Note that streams are buffered in order. So, this call will return the next least recently received stream by popping from the front of the queue of pending streams. If no streams are pending, it will block until it receives one. You can use the cancellation token to stop the operation.

**Note:** This method will return both bidirectional and unidirectional streams. They can be distinguished based on the `IStreamDirectionFeature.CanRead` and `IStreamDirectionFeature.CanWrite` properties.

- Opening a new WebTransport stream from the server
```C#
var connectionContext = await session.OpenUnidirectionalStreamAsync(CancellationToken token);
```
This will attempt to open a new unidirectional stream from the server to the client and return an instance of `ConnectionContext`. You can use the cancellation token to stop the operation.

- Sending data over a WebTransport stream
```C#
var stream = connectionContext.Transport.Output;
await stream.WriteAsync(ReadOnlyMemory<byte> bytes);
```
`stream.WriteAsync` will write data to the stream and then automatically flush (i.e. send it to the client).

**Note:** You can only send data on streams that have `IStreamDirectionFeature.CanWrite` set as `true`. Sending data on non-writable streams will throw an `NotSupportedException` exception.

- Reading data from a WebTransport stream
```C#
var stream = connectionContext.Transport.Input.AsStream();
var length = await stream.ReadAsync(Memory<byte> memory);
```
`stream.ReadAsync` will read data from the stream and copy it into the provided `memory` parameter. It will then return the number of bytes read.

**Note:** You can only read data from streams that have `IStreamDirectionFeature.CanRead` set as `true`. Reading data on non-readable streams will throw an `NotSupportedException` exception.

- Aborting a WebTransport session
```C#
session.Abort(int errorCode);
```
Aborting a WebTransport session will result in severing the connection with the client and aborting all the streams. You can optionally specify an error code that will be passed down into the logs. The default value (256) represents no error.

**Note:** valid error codes are defined [here](https://www.rfc-editor.org/rfc/rfc9114.html#name-http-3-error-codes).

- Aborting a WebTransport stream
```C#
stream.Abort(ConnectionAbortedException exception);
```
Aborting a WebTransport stream will result in abruptly ending all data transmission over the stream. You can optionally specify an aborted exception that will be passed down into the logs. A default message is used if no message is provided.

- Soft closing a WebTransport stream
```C#
stream.DisposeAsync();
```
Disposing a WebTransport stream will result in ending data transmission and closing the stream gracefully.

### Examples

#### Example 1

This example waits for a bidirectional stream. Once it receives one, it will read the data from it, reverse it and then write it back to the stream.
```C#
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel((context, options) =>
{
    // Port configured for WebTransport
    options.Listen(IPAddress.Any, 5007, listenOptions =>
    {
        listenOptions.UseHttps(GenerateManualCertificate());
        listenOptions.UseConnectionLogging();
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
    });
});
var host = builder.Build();

host.Run(async (context) =>
{
    var feature = context.Features.GetRequiredFeature<IHttpWebTransportFeature>();
    if (!feature.IsWebTransportRequest)
    {
        return;
    }
    var session = await feature.AcceptAsync(CancellationToken.None);

    ConnectionContext? stream = null;
    IStreamDirectionFeature? direction = null;
    while (true)
    {
        // wait until we get a stream
        stream = await session.AcceptStreamAsync(CancellationToken.None);
        if (stream is not null)
        {

            // check that the stream is bidirectional. If yes, keep going, otherwise
            // dispose its resources and keep waiting.
            direction = stream.Features.GetRequiredFeature<IStreamDirectionFeature>();
            if (direction.CanRead && direction.CanWrite)
            {
                break;
            }
            else
            {
                await stream.DisposeAsync();
            }
        }
        else
        {
            // if a stream is null, this means that the session failed to get the next one.
            // Thus, the session has ended or some other issue has occurred. We end the
            // connection in this case.
            return;
        }
    }

    var inputPipe = stream!.Transport.Input;
    var outputPipe = stream!.Transport.Output;

    // read some data from the stream into the memory
    var length = await inputPipe.AsStream().ReadAsync(memory);

    // slice to only keep the relevant parts of the memory
    var outputMemory = memory[..length];

    // do some operations on the contents of the data
    outputMemory.Span.Reverse();

    // write back the data to the stream
    await outputPipe.WriteAsync(outputMemory);
});

await host.RunAsync();
```

#### Example 2

This example opens a new stream from the server side and then sends data.
```C#
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel((context, options) =>
{
    // Port configured for WebTransport
    options.Listen(IPAddress.Any, 5007, listenOptions =>
    {
        listenOptions.UseHttps(GenerateManualCertificate());
        listenOptions.UseConnectionLogging();
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
    });
});
var host = builder.Build();

host.Run(async (context) =>
{
    var feature = context.Features.GetRequiredFeature<IHttpWebTransportFeature>();
    if (!feature.IsWebTransportRequest)
    {
        return;
    }
    var session = await feature.AcceptAsync(CancellationToken.None);
    // open a new stream from the server to the client
    var stream = await session.OpenUnidirectionalStreamAsync(CancellationToken.None);

    // write data to the stream
    var outputPipe = stream.Transport.Output;
    await outputPipe.WriteAsync(new Memory<byte>(new byte[] { 65, 66, 67, 68, 69 }), CancellationToken.None);
    await outputPipe.FlushAsync(CancellationToken.None);
});

await host.RunAsync();
```
