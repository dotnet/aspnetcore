// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace FSharpWebSite

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection


type Startup () =

    member this.ConfigureServices(services: IServiceCollection) =
        services.AddMvc() |> ignore

    member this.Configure(app: IApplicationBuilder) =
        app.UseDeveloperExceptionPage() |> ignore
        app.UseStaticFiles() |> ignore
        app.UseMvcWithDefaultRoute() |> ignore
