# Getting start with HTTP/3 on Windows and Linux

This is a getting started guide for using HTTP/3 on Windows and Linux.

## Prerequisites

1. Download the latest 5.0-preview build of .NET from <https://dotnet.microsoft.com/download/dotnet/5.0.>
2. Download Edge Canary <https://www.microsoftedgeinsider.com/en-us/download.> This is required to browse websites with HTTP/3.

### Windows

1. Latest [Windows Insider Builds](https://insider.windows.com/en-us/), Insiders Fast build. This is required for Schannel support for QUIC.
2. Enabling TLS 1.3. Add the following registry keys to enable TLS 1.3.

    ```text
    reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.3\Server" /v DisabledByDefault /t REG_DWORD /d 1 /f
    reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.3\Server" /v Enabled /t REG_DWORD /d 1 /f
    reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.3\Client" /v DisabledByDefault /t REG_DWORD /d 1 /f
    reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.3\Client" /v Enabled /t REG_DWORD /d 1 /f
    ```

### Linux

1. [Building OpenSSL](https://github.com/openssl/openssl/pull/8797) with QUIC support.

    Today, OpenSSL doesn't support QUIC, and currently isn't planing to [support QUIC in the 3.0 release](https://www.openssl.org/blog/blog/2020/02/17/QUIC-and-OpenSSL/). .NET relies on OpenSSL for TLS on Linux. There is an outstanding pull request which adds the required APIs to [support QUIC](https://github.com/openssl/openssl/pull/8797).

    ```text
    git clone https://github.com/akamai/openssl
    cd openssl
    ./config
    make
    ```

## Running on Windows Locally

1. Create a new ASP.NET Core WebApplication that targets 5.0.

    You can do on command line by running:

    ```text
    dotnet new web -o Http3App
    ```

2. Generating a new certificate for local development

    ```Powershell
    New-SelfSignedCertificate -DnsName localhost -FriendlyName Http3TestCert -KeyUsageProperty Sign -KeyUsage DigitalSignature -CertStoreLocation cert:\CurrentUser\My -HashAlgorithm SHA256 -Provider "Microsoft Software Key Storage Provider"
    ```

    And trust this certificate by putting it into the trusted root for the current user.

    TODO investigate why specifically the dev cert doesn't work.

3. Adding a package reference to Microsoft.AspNetCore.Server.Kestrel.Transport.Experimental.Quic

    You can do this by adding the following to the Http3App.csproj.

    ```xml
    <ItemGroup>
        <PackageReference Include="Microsoft.AspNetCore.Server.Kestrel.Transport.Experimental.Quic" Version="5.0.0-preview.3.20215.14" />
    </ItemGroup>
    ```

    or you can add it via commandline

    ```text
    dotnet add package Microsoft.AspNetCore.Server.Kestrel.Transport.Experimental.Quic --version "5.0.0-*"
    ```

4. Building msquic for schannel

    5.0 preview3 and preview4 builds support h3-25, which needs to build a specific version of msquic. Checkout and build msquic via running the following.

    ```text
    cd ..
    git clone --branch v0.9-draft-25 https://github.com/microsoft/msquic
    cd msquic
    ```

    Next, build msquic targeting schannel and copy it to your application directory.

    ```text
    # requires Powershell Core to build MsQuic at the moment.
    .\build.ps1 -Tls schannel
    # Copy msquic to the app.
    cp .\artifacts\windows\x64_Debug_schannel\msquic.dll ..\Http3App
    ```

    We are also planning a distribution story for msquic so you don't need to build it yourself.

5. Add the following code to Program.cs

    ```c#
    namespace Http3App
    {
        public class Program
        {
            public static void Main(string[] args)
            {
                CreateHostBuilder(args).Build().Run();
            }

            public static IHostBuilder CreateHostBuilder(string[] args) =>
                Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        var cert = CertificateLoader.LoadFromStoreCert("Http3App", StoreName.My.ToString(), StoreLocation.CurrentUser, false);
                        webBuilder.UseStartup<Startup>().UseQuic(options =>
                        {
                            options.Certificate = cert;
                            options.Alpn = "h3-25";
                        })
                        .ConfigureKestrel((context, options) =>
                        {
                            options.EnableAltSvc = true;

                            options.Listen(System.Net.IPAddress.Any, 5001, listenOptions =>
                            {
                                listenOptions.UseHttps(httpsOptions =>
                                {
                                    httpsOptions.ServerCertificate = cert;
                                });
                                listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                            });
                        });
                    });
        }
    }
    ```

6. Run the app on commandline:

    ```text
    dotnet run
    ```

7. Launch Edge with switches to enable QUIC/

    ```text
    & "C:\Users\<user>\AppData\Local\Microsoft\Edge SxS\Application\msedge.exe" --enable-quic --quic-version=h3-25 --origin-to-force-quic-on=localhost:5001
    ```

8. Browse to <https://localhost:5001/>.

    We'd recommend going over to the network tab and changing edge to display the protocol.
    ![image](https://user-images.githubusercontent.com/8302101/81094192-02c7da00-8eb8-11ea-9d59-670b2cf99665.png)

    You should see Kestrel respond with h3-25 as the protocol.

    ![image](https://user-images.githubusercontent.com/8302101/81095468-e3ca4780-8eb9-11ea-8dea-b23ac66897ea.png)

## Running on Windows Remotely

TODO

## Running on Linux locally

TODO

## Running on Linux remotely

TODO

## Credit

Credit to Diwakar Mantha and the Kaizala team for the MsQuic interop code.
