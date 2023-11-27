// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Components;

internal struct ComponentMarker
{
    public const string ServerMarkerType = "server";
    public const string WebAssemblyMarkerType = "webassembly";
    public const string AutoMarkerType = "auto";

    #region Common marker data

    // The marker type. Can be "server", "webassembly", or "auto".
    public string? Type { get; set; }

    // A string to allow the clients to differentiate between prerendered
    // and non prerendered components and to uniquely identify start and end
    // markers in prererendered components.
    // The value will be null if this marker represents a non-prerendered component.
    public string? PrerenderId { get; set; }

    // A key that the browser can use when comparing markers to determine
    // whether they represent different component instances.
    public ComponentMarkerKey? Key { get; set; }

    #endregion

    #region Server marker data

    // The order in which this component was rendered/produced
    // on the server. It matches the number on the descriptor
    // and is used to prevent an infinite amount of components
    // from being rendered from the client-side.
    public int? Sequence { get; set; }

    // A data-protected payload that allows the server to validate the legitimacy
    // of the invocation.
    // The value will be null for end markers.
    public string? Descriptor { get; set; }

    #endregion

    #region WebAssembly marker data

    // The assembly containing the component type.
    public string? Assembly { get; set; }

    // The full name of the component type.
    public string? TypeName { get; set; }

    // Serialized definitions of the component's parameters.
    public string? ParameterDefinitions { get; set; }

    // Serialized values of the component's parameters.
    public string? ParameterValues { get; set; }

    #endregion

    public static ComponentMarker Create(string type, bool prerendered, ComponentMarkerKey? key)
    {
        return new()
        {
            Type = type,
            PrerenderId = prerendered ? GeneratePrerenderId() : null,
            Key = key,
        };
    }

    public void WriteServerData(int sequence, string descriptor)
    {
        Sequence = sequence;
        Descriptor = descriptor;
    }

    public void WriteWebAssemblyData(string assembly, string typeName, string parameterDefinitions, string parameterValues)
    {
        Assembly = assembly;
        TypeName = typeName;
        ParameterDefinitions = parameterDefinitions;
        ParameterValues = parameterValues;
    }

    public ComponentEndMarker? ToEndMarker()
        => PrerenderId is null ? default : new() { PrerenderId = PrerenderId };

    private static string GeneratePrerenderId()
        => Guid.NewGuid().ToString("N");
}

internal struct ComponentEndMarker
{
    public string? PrerenderId { get; set; }
}

internal struct ComponentMarkerKey : IEquatable<ComponentMarkerKey>
{
    public ComponentMarkerKey(string locationHash, string? formattedComponentKey)
        => (LocationHash, FormattedComponentKey) = (locationHash, formattedComponentKey);

    // A hash that distinguishes this component from other components in the render tree.
    // The output should be deterministic between endpoint invocations so that the client
    // can match up component instances between renders.
    // The current implementation uses the hashed component type name and its render tree
    // sequence number.
    public string LocationHash { get; set; }

    // The formatted component key (@key), if any. This helps the developer further distinguish
    // between component instances if they have the same type and sequence number (e.g., components
    // rendered in a list).
    // In addition, specifying a @key lets interactive components receive parameter updates dynamically.
    public string? FormattedComponentKey { get; set; }

    public static bool operator ==(ComponentMarkerKey left, ComponentMarkerKey right)
        => left.Equals(right);

    public static bool operator !=(ComponentMarkerKey left, ComponentMarkerKey right)
        => !(left == right);

    public readonly bool Equals(ComponentMarkerKey other)
        => string.Equals(LocationHash, other.LocationHash, StringComparison.Ordinal)
        && string.Equals(FormattedComponentKey, other.FormattedComponentKey, StringComparison.Ordinal);

    public override readonly bool Equals(object? obj)
        => obj is ComponentMarkerKey other && Equals(other);

    public override readonly int GetHashCode()
        => HashCode.Combine(LocationHash, FormattedComponentKey);
}
