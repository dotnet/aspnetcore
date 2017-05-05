// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace FSharpWebSite

open System.IO
open Microsoft.AspNetCore.Hosting


module Program =

    [<EntryPoint>]
    let main args =
        let host = 
            WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseKestrel()
                .UseIISIntegration()
                .Build()

        host.Run()
        
        0
