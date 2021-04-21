open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting

[<EntryPoint>]
let main args =
    let app = WebApplication.Create(args);

    if app.Environment.IsDevelopment() then
        app.UseDeveloperExceptionPage() |> ignore

    app.MapGet("/", Func<string>(fun () -> "Hello World!")) |> ignore

    app.MapGet("/plaintext", Func<string>(fun () -> "Hello, World!")) |> ignore
    app.MapGet("/json", Func<obj>(fun () -> {| message = "Hello, World!" |} :> obj)) |> ignore
    app.MapGet("/hello/{name}", Func<string, string>(fun name -> $"Hello {name}")) |> ignore

    app.Run()

    0 // Exit code
