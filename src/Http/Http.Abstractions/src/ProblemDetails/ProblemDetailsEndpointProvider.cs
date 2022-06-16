// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// 
/// </summary>
public sealed class ProblemDetailsWriterProvider
{
    private readonly IProblemDetailsWriter[] _writers;

    public ProblemDetailsWriterProvider(IEnumerable<IProblemDetailsWriter> writers)
    {
        _writers = writers.ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="currentMetadata"></param>
    /// <param name="isRouting"></param>
    /// <returns></returns>
    public IProblemDetailsWriter? GetWriter(
        HttpContext context,
        EndpointMetadataCollection? currentMetadata = null,
        bool isRouting = false)
    {
        currentMetadata ??= context.GetEndpoint()?.Metadata;

        for (var i = 0; i < _writers.Length; i++)
        {
            if (_writers[i].CanWrite(context, currentMetadata, isRouting: isRouting))
            {
                return _writers[i];
            }
        }

        return null;
    }
}
