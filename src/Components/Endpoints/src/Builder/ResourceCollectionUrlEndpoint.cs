// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class ResourceCollectionUrlEndpoint
{
    internal static (ImportMapDefinition, string) MapResourceCollectionEndpoints(
        List<Endpoint> endpoints,
        string resourceCollectionUrlFormat,
        ResourceAssetCollection resourceCollection)
    {
        // We map an additional endpoint to serve the resources so webassembly can fetch the list of
        // resources and use fingerprinted resources when running interactively.
        // We expose the same endpoint in four different urls _framework/resource-collection(.<fingerprint>)?.js(.gz)? and
        // with appropriate caching headers in both cases.
        // The fingerprinted URL allows us to cache the resource collection forever and avoid an additional request
        // to fetch the resource collection on subsequent visits.
        var fingerprintSuffix = ComputeFingerprintSuffix(resourceCollection)[0..6];
        // $"/_framework/resource-collection.{fingerprint}.js";
        var fingerprintedResourceCollectionUrl = string.Format(CultureInfo.InvariantCulture, resourceCollectionUrlFormat, fingerprintSuffix, "");
        // $"/_framework/resource-collection.{fingerprintSuffix}.js.gz";
        var fingerprintedGzResourceCollectionUrl = string.Format(CultureInfo.InvariantCulture, resourceCollectionUrlFormat, fingerprintSuffix, ".gz");
        // $"/_framework/resource-collection.js"
        var resourceCollectionUrl = string.Format(CultureInfo.InvariantCulture, resourceCollectionUrlFormat, "", "");
        // $"/_framework/resource-collection.js.gz"
        var gzResourceCollectionUrl = string.Format(CultureInfo.InvariantCulture, resourceCollectionUrlFormat, "", ".gz");

        var bytes = CreateContent(resourceCollection);
        var gzipBytes = CreateGzipBytes(bytes);
        var integrity = ComputeIntegrity(bytes);

        var resourceCollectionEndpoints = new ResourceCollectionEndpointsBuilder(bytes, gzipBytes);
        var builders = resourceCollectionEndpoints.CreateEndpoints(
            fingerprintedResourceCollectionUrl,
            fingerprintedGzResourceCollectionUrl,
            resourceCollectionUrl,
            gzResourceCollectionUrl);

        foreach (var builder in builders)
        {
            var endpoint = builder.Build();
            endpoints.Add(endpoint);
        }

        var importMapDefinition = new ImportMapDefinition(
            new Dictionary<string, string>
            {
                [resourceCollectionUrl] = $"./{fingerprintedResourceCollectionUrl}",
                [gzResourceCollectionUrl] = $"./{fingerprintedGzResourceCollectionUrl}",
            },
            scopes: null,
            new Dictionary<string, string>
            {
                [$"./{fingerprintedResourceCollectionUrl}"] = integrity,
                [$"./{fingerprintedGzResourceCollectionUrl}"] = integrity,
            });

        return (importMapDefinition, $"./{fingerprintedResourceCollectionUrl}");
    }

    private static string ComputeIntegrity(byte[] bytes)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(bytes, hash);
        return $"sha256-{Convert.ToBase64String(hash)}";
    }

    private static byte[] CreateGzipBytes(byte[] bytes)
    {
        using var gzipContent = new MemoryStream();
        using var gzipStream = new GZipStream(gzipContent, CompressionLevel.Optimal, leaveOpen: true);
        gzipStream.Write(bytes);
        gzipStream.Flush();
        return gzipContent.ToArray();
    }

    private static byte[] CreateContent(ResourceAssetCollection resourceCollection)
    {
        var content = new MemoryStream();
        var preamble = """
            export function get() {
                return
            """u8;
        content.Write(preamble);
        var utf8Writer = new Utf8JsonWriter(content);
        JsonSerializer.Serialize<IReadOnlyList<ResourceAsset>>(utf8Writer, resourceCollection, ResourceCollectionSerializerContext.Default.Options);
        var epilogue = """
            ;
            }
            """u8;
        content.Write(epilogue);
        content.Flush();
        return content.ToArray();
    }

    private static string ComputeFingerprintSuffix(ResourceAssetCollection resourceCollection)
    {
        var resources = (IReadOnlyList<ResourceAsset>)resourceCollection;
        var incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> buffer = stackalloc byte[1024];
        byte[]? rented = null;
        Span<byte> result = stackalloc byte[incrementalHash.HashLengthInBytes];
        foreach (var resource in resources)
        {
            var url = resource.Url;
            AppendToHash(incrementalHash, buffer, ref rented, url);
        }
        incrementalHash.GetCurrentHash(result);
        // Base64 encoding at most increases size by (4 * byteSize / 3 + 2),
        // add an extra byte for the initial dot.
        Span<char> fingerprintSpan = stackalloc char[(incrementalHash.HashLengthInBytes * 4 / 3) + 3];
        var length = WebUtilities.WebEncoders.Base64UrlEncode(result, fingerprintSpan[1..]);
        fingerprintSpan[0] = '.';
        return fingerprintSpan[..(length + 1)].ToString();
    }

    private static void AppendToHash(IncrementalHash incrementalHash, Span<byte> buffer, ref byte[]? rented, string value)
    {
        if (Encoding.UTF8.TryGetBytes(value, buffer, out var written))
        {
            incrementalHash.AppendData(buffer[..written]);
        }
        else
        {
            var length = Encoding.UTF8.GetByteCount(value);
            if (rented == null || rented.Length < length)
            {
                if (rented != null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
                rented = ArrayPool<byte>.Shared.Rent(length);
                var bytesWritten = Encoding.UTF8.GetBytes(value, rented);
                incrementalHash.AppendData(rented.AsSpan(0, bytesWritten));
            }
        }
    }

    [JsonSerializable(typeof(ResourceAssetCollection))]
    [JsonSerializable(typeof(IReadOnlyList<ResourceAsset>))]
    [JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault, WriteIndented = false)]
    private partial class ResourceCollectionSerializerContext : JsonSerializerContext
    {
    }

    private class ResourceCollectionEndpointsBuilder
    {
        private readonly byte[] _content;
        private readonly string _contentETag;
        private readonly byte[] _gzipContent;
        private readonly string[] _gzipContentETags;

        public ResourceCollectionEndpointsBuilder(byte[] content, byte[] gzipContent)
        {
            _content = content;
            _contentETag = ComputeETagTag(content);
            _gzipContent = gzipContent;
            _gzipContentETags = [$"W/ {_contentETag}", ComputeETagTag(gzipContent)];
        }

        private string ComputeETagTag(byte[] content)
        {
            Span<byte> data = stackalloc byte[32];
            SHA256.HashData(content, data);
            return $"\"{Convert.ToBase64String(data)}\"";
        }

        public async Task FingerprintedGzipContent(HttpContext context)
        {
            WriteCommonHeaders(context, _gzipContent);
            WriteEncodingHeaders(context);
            WriteFingerprintHeaders(context);
            await context.Response.Body.WriteAsync(_gzipContent);
        }

        public async Task FingerprintedContent(HttpContext context)
        {
            WriteCommonHeaders(context, _content);
            WriteFingerprintHeaders(context);
            await context.Response.Body.WriteAsync(_content);
        }

        public async Task Content(HttpContext context)
        {
            WriteCommonHeaders(context, _content);
            WriteNonFingerprintedHeaders(context);
            await context.Response.Body.WriteAsync(_content);
        }

        public async Task GzipContent(HttpContext context)
        {
            WriteCommonHeaders(context, _gzipContent);
            WriteEncodingHeaders(context);
            WriteNonFingerprintedHeaders(context);
            await context.Response.Body.WriteAsync(_gzipContent);
        }

        private void WriteEncodingHeaders(HttpContext context)
        {
            context.Response.Headers[HeaderNames.ContentEncoding] = "gzip";
            context.Response.Headers[HeaderNames.Vary] = HeaderNames.AcceptEncoding;
            context.Response.Headers.ETag = new StringValues(_gzipContentETags);
        }

        private void WriteNoEncodingHeaders(HttpContext context)
        {
            context.Response.Headers.ETag = new StringValues(_contentETag);
        }

        private static void WriteFingerprintHeaders(HttpContext context)
        {
            context.Response.Headers[HeaderNames.CacheControl] = "max-age=31536000, immutable";
        }

        private static void WriteNonFingerprintedHeaders(HttpContext context)
        {
            context.Response.Headers[HeaderNames.CacheControl] = "no-cache, must-revalidate";
        }

        private static void WriteCommonHeaders(HttpContext context, byte[] contents)
        {
            context.Response.ContentType = "application/javascript";
            context.Response.ContentLength = contents.Length;
        }

        internal IEnumerable<RouteEndpointBuilder> CreateEndpoints(
            string fingerprintedResourceCollectionUrl,
            string fingerprintedGzResourceCollectionUrl,
            string resourceCollectionUrl,
            string gzResourceCollectionUrl)
        {
            var quality = 1 / (1 + _gzipContent.Length);
            var encodingMetadata = new ContentEncodingMetadata("gzip", quality);

            var fingerprintedGzBuilder = new RouteEndpointBuilder(
                FingerprintedGzipContent,
                RoutePatternFactory.Parse(fingerprintedGzResourceCollectionUrl),
                -100);

            var fingerprintedBuilder = new RouteEndpointBuilder(
                FingerprintedContent,
                RoutePatternFactory.Parse(fingerprintedResourceCollectionUrl),
                -100);

            var fingerprintedBuilderConeg = new RouteEndpointBuilder(
                FingerprintedGzipContent,
                RoutePatternFactory.Parse(fingerprintedResourceCollectionUrl),
                -100);

            fingerprintedBuilderConeg.Metadata.Add(encodingMetadata);

            var gzBuilder = new RouteEndpointBuilder(
                GzipContent,
                RoutePatternFactory.Parse(gzResourceCollectionUrl),
                -100);

            var builder = new RouteEndpointBuilder(
                Content,
                RoutePatternFactory.Parse(resourceCollectionUrl),
                -100);

            var builderConeg = new RouteEndpointBuilder(
                Content,
                RoutePatternFactory.Parse(resourceCollectionUrl),
                -100);

            builderConeg.Metadata.Add(encodingMetadata);

            return [
                fingerprintedGzBuilder,
                fingerprintedBuilder,
                fingerprintedBuilderConeg,
                gzBuilder,
                builderConeg,
                builder
            ];
        }
    }
}
