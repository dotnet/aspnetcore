namespace SimpleMvcFSharp

module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =
        let t = typeof<Microsoft.AspNetCore.Mvc.IActionResult>
        System.Console.WriteLine(t.FullName)

        exitCode
