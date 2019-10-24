## Using MsQuic on Windows

### Setup pre-requisites

1. Update machine to the latest Windows Insider build. This is required for TLS 1.3 support.
2. Copy msquic.dll and msquic.pdb to this directory and uncomment the copy task in Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.csproj. This will copy the msquic.dll into any built project.
