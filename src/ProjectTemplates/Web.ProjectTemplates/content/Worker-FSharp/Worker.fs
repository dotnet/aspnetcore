namespace Company.Application1

open System
open System.Collections.Generic
open System.Linq
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

type Worker(logger: ILogger<Worker>) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) =
        async {
            while not ct.IsCancellationRequested do
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now)
                do! Async.Sleep(1000)
        }
        |> Async.StartAsTask
        :> Task // need to convert into the parameter-less task