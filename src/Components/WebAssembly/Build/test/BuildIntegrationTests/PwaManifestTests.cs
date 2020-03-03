using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Components.WebAssembly.Build
{
    public class PwaManifestTests
    {
        [Fact]
        public async Task Build_ServiceWorkerAssetsManifest_Works()
        {
            // Arrange
            var expectedEntries = (new[] {
                "index.html",
                "css/site.css",
                "_framework/_bin/standalone.dll",
                "_framework/_bin/Microsoft.AspNetCore.Components.dll",
                "_framework/_bin/System.Buffers.dll",
                "_framework/_bin/mscorlib.dll",
                "_framework/_bin/Microsoft.Extensions.Logging.Abstractions.dll",
                "_framework/_bin/netstandard.dll",
                "_framework/_bin/System.Xml.Linq.dll",
                "_framework/_bin/System.Core.dll",
                "_framework/_bin/System.dll",
                "_framework/_bin/WebAssembly.Net.WebSockets.dll",
                "_framework/_bin/System.Memory.dll",
                "_framework/_bin/WebAssembly.Bindings.dll",
                "_framework/_bin/System.Numerics.dll",
                "_framework/_bin/System.Xml.dll",
                "_framework/_bin/Mono.Security.dll",
                "_framework/_bin/System.Transactions.dll",
                "_framework/_bin/System.Runtime.Serialization.dll",
                "_framework/_bin/System.ServiceModel.Internals.dll",
                "_framework/_bin/System.Net.Http.dll",
                "_framework/_bin/WebAssembly.Net.Http.dll",
                "_framework/_bin/System.ComponentModel.Composition.dll",
                "_framework/_bin/System.IO.Compression.FileSystem.dll",
                "_framework/_bin/System.IO.Compression.dll",
                "_framework/_bin/System.Drawing.Common.dll",
                "_framework/_bin/System.Data.DataSetExtensions.dll",
                "_framework/_bin/System.Data.dll",
                "_framework/_bin/Microsoft.Extensions.DependencyInjection.Abstractions.dll",
                "_framework/_bin/standalone.pdb",
                "_framework/wasm/dotnet.wasm",
                "_framework/wasm/dotnet.3.2.0-preview2.js",
                "_framework/blazor.webassembly.js",
                "_framework/blazor.webassembly.js.map",
                "_framework/blazor.boot.json",
            })
            .OrderBy(e => e)
            .ToArray();

            using var project = ProjectDirectory.Create("standalone");
            var result = await MSBuildProcessManager.DotnetMSBuild(project, args: "/p:ServiceWorkerAssetsManifest=service-worker-assets.js");

            Assert.BuildPassed(result);

            var buildOutputDirectory = project.BuildOutputDirectory;

            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.boot.json");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "blazor.webassembly.js");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "wasm", "dotnet.wasm");
            Assert.FileCountEquals(result, 1, Path.Combine(buildOutputDirectory, "wwwroot", "_framework", "wasm"), "dotnet.*.js");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "standalone.dll");
            Assert.FileExists(result, buildOutputDirectory, "wwwroot", "_framework", "_bin", "Microsoft.Extensions.Logging.Abstractions.dll"); // Verify dependencies are part of the output.

            var staticWebAssets = Assert.FileExists(result, buildOutputDirectory, "standalone.StaticWebAssets.xml");
            Assert.FileContains(result, staticWebAssets, Path.Combine("netstandard2.1", "wwwroot"));

            var serviceWorkerAssetsManifest = Assert.FileExists(result, buildOutputDirectory, "wwwroot", "service-worker-assets.js");
            // Trim prefix 'self.assetsManifest = ' and suffix ';'
            var manifestContents = File.ReadAllText(serviceWorkerAssetsManifest)[22..^3];

            var manifestContentsJson = JsonDocument.Parse(manifestContents);
            Assert.True(manifestContentsJson.RootElement.TryGetProperty("assets", out var assets));
            Assert.Equal(JsonValueKind.Array, assets.ValueKind);

            var entries = assets.EnumerateArray().Select(e => e.GetProperty("url").GetString()).OrderBy(e => e).ToArray();
            Assert.Equal(expectedEntries, entries);
        }
    }
}
