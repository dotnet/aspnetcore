#region Copyright notice and license

// Copyright 2019 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System.IO.Compression;
using System.Linq;
using Grpc.AspNetCore.Server;
using Grpc.Net.Compression;

namespace Grpc.Shared.Server;

/// <summary>
/// Options used to execute a gRPC method.
/// </summary>
internal sealed class MethodOptions
{
    /// <summary>
    /// Gets the list of compression providers used to compress and decompress gRPC messages.
    /// </summary>
    public IReadOnlyDictionary<string, ICompressionProvider> CompressionProviders { get; }

    /// <summary>
    /// Get a collection of interceptors to be executed with every call. Interceptors are executed in order.
    /// </summary>
    public IReadOnlyList<InterceptorRegistration> Interceptors { get; }

    /// <summary>
    /// Gets the maximum message size in bytes that can be sent from the server.
    /// </summary>
    public int? MaxSendMessageSize { get; }

    /// <summary>
    /// Gets the maximum message size in bytes that can be received by the server.
    /// </summary>
    public int? MaxReceiveMessageSize { get; }

    /// <summary>
    /// Gets a value indicating whether detailed error messages are sent to the peer.
    /// Detailed error messages include details from exceptions thrown on the server.
    /// </summary>
    public bool? EnableDetailedErrors { get; }

    /// <summary>
    /// Gets the compression algorithm used to compress messages sent from the server.
    /// The request grpc-accept-encoding header value must contain this algorithm for it to
    /// be used.
    /// </summary>
    public string? ResponseCompressionAlgorithm { get; }

    /// <summary>
    /// Gets the compression level used to compress messages sent from the server.
    /// The compression level will be passed to the compression provider.
    /// </summary>
    public CompressionLevel? ResponseCompressionLevel { get; }

    // Fast check for whether the service has any interceptors
    internal bool HasInterceptors { get; }

    private MethodOptions(
        Dictionary<string, ICompressionProvider> compressionProviders,
        InterceptorCollection interceptors,
        int? maxSendMessageSize,
        int? maxReceiveMessageSize,
        bool? enableDetailedErrors,
        string? responseCompressionAlgorithm,
        CompressionLevel? responseCompressionLevel)
    {
        CompressionProviders = compressionProviders;
        Interceptors = interceptors;
        HasInterceptors = interceptors.Count > 0;
        MaxSendMessageSize = maxSendMessageSize;
        MaxReceiveMessageSize = maxReceiveMessageSize;
        EnableDetailedErrors = enableDetailedErrors;
        ResponseCompressionAlgorithm = responseCompressionAlgorithm;
        ResponseCompressionLevel = responseCompressionLevel;

        if (ResponseCompressionAlgorithm != null)
        {
            if (!CompressionProviders.TryGetValue(ResponseCompressionAlgorithm, out var _))
            {
                throw new InvalidOperationException($"The configured response compression algorithm '{ResponseCompressionAlgorithm}' does not have a matching compression provider.");
            }
        }
    }

    /// <summary>
    /// Creates method options by merging together the settings the specificed <see cref="GrpcServiceOptions"/> collection.
    /// The <see cref="GrpcServiceOptions"/> should be ordered with items arranged in ascending order of precedence.
    /// When interceptors from multiple options are merged together they will be executed in reverse order of precendence.
    /// </summary>
    /// <param name="serviceOptions">A collection of <see cref="GrpcServiceOptions"/> instances, arranged in ascending order of precedence.</param>
    /// <returns>A new <see cref="MethodOptions"/> instanced with settings merged from specifid <see cref="GrpcServiceOptions"/> collection.</returns>
    public static MethodOptions Create(IEnumerable<GrpcServiceOptions> serviceOptions)
    {
        // This is required to get ensure that service methods without any explicit configuration
        // will continue to get the global configuration options
        var resolvedCompressionProviders = new Dictionary<string, ICompressionProvider>(StringComparer.Ordinal);
        var tempInterceptors = new List<InterceptorRegistration>();
        int? maxSendMessageSize = null;
        int? maxReceiveMessageSize = null;
        bool? enableDetailedErrors = null;
        string? responseCompressionAlgorithm = null;
        CompressionLevel? responseCompressionLevel = null;

        foreach (var options in serviceOptions.Reverse())
        {
            AddCompressionProviders(resolvedCompressionProviders, options.CompressionProviders);
            tempInterceptors.InsertRange(0, options.Interceptors);
            maxSendMessageSize ??= options.MaxSendMessageSize;
            maxReceiveMessageSize ??= options.MaxReceiveMessageSize;
            enableDetailedErrors ??= options.EnableDetailedErrors;
            responseCompressionAlgorithm ??= options.ResponseCompressionAlgorithm;
            responseCompressionLevel ??= options.ResponseCompressionLevel;
        }

        var interceptors = new InterceptorCollection();
        foreach (var interceptor in tempInterceptors)
        {
            interceptors.Add(interceptor);
        }

        return new MethodOptions
        (
            compressionProviders: resolvedCompressionProviders,
            interceptors: interceptors,
            maxSendMessageSize: maxSendMessageSize,
            maxReceiveMessageSize: maxReceiveMessageSize,
            enableDetailedErrors: enableDetailedErrors,
            responseCompressionAlgorithm: responseCompressionAlgorithm,
            responseCompressionLevel: responseCompressionLevel
        );
    }

    private static void AddCompressionProviders(Dictionary<string, ICompressionProvider> resolvedProviders, IList<ICompressionProvider>? compressionProviders)
    {
        if (compressionProviders != null)
        {
            foreach (var compressionProvider in compressionProviders)
            {
                if (!resolvedProviders.ContainsKey(compressionProvider.EncodingName))
                {
                    resolvedProviders.Add(compressionProvider.EncodingName, compressionProvider);
                }
            }
        }
    }
}
