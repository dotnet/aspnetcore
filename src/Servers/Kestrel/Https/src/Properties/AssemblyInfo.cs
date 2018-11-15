// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;

[assembly: TypeForwardedTo(typeof(ClientCertificateMode))]
[assembly: TypeForwardedTo(typeof(HttpsConnectionAdapter))]
[assembly: TypeForwardedTo(typeof(HttpsConnectionAdapterOptions))]
[assembly: TypeForwardedTo(typeof(ListenOptionsHttpsExtensions))]