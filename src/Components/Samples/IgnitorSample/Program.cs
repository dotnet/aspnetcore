using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ignitor;
using Microsoft.AspNetCore.SignalR.Client;

namespace IgnitorSample
{
    /// <summary>
    /// This is a minimal sample that lets you try out Ignitor against a Blazor Server app.
    /// To use this, first launch the server app. Update the code below to point to the host url and run the test.
    /// </summary>
    class Program
    {
        private static readonly string ServerUrl = "https://localhost:5001";
        private static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        static async Task Main(string[] args)
        {
            var client = new BlazorClient();
            await client.ConnectAsync(new Uri(ServerUrl));

            await VerifyNavigationAsync(client);

            Console.WriteLine("Done");
        }

        static async ValueTask VerifyNavigationAsync(BlazorClient client)
        {
            await client.ExpectRenderBatch(() => client.NavigateAsync($"{ServerUrl}/counter"));
            client.Hive.TryFindElementById("counter", out var counter);
            Debug.Assert(counter != null, "We must have navigated to counter.");
        }
    }

    static class BlazorClientExtensions
    {
        public static Task NavigateAsync(this BlazorClient client, string url, CancellationToken cancellationToken = default)
        {
            return client.HubConnection.InvokeAsync("OnLocationChanged", url, false, cancellationToken);
        }
    }
}
