// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Cors.Infrastructure;

/// <summary>
/// Defines the policy for Cross-Origin requests based on the CORS specifications.
/// </summary>
public class CorsPolicy
{
    private Func<string, bool> _isOriginAllowed;
    private TimeSpan? _preflightMaxAge;

    /// <summary>
    /// Default constructor for a CorsPolicy.
    /// </summary>
    public CorsPolicy()
    {
        _isOriginAllowed = DefaultIsOriginAllowed;
    }

    /// <summary>
    /// Gets a value indicating if all headers are allowed.
    /// </summary>
    public bool AllowAnyHeader
    {
        get
        {
            if (Headers == null || Headers.Count != 1 || Headers[0] != CorsConstants.AnyHeader)
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Gets a value indicating if all methods are allowed.
    /// </summary>
    public bool AllowAnyMethod
    {
        get
        {
            if (Methods == null || Methods.Count != 1 || Methods[0] != CorsConstants.AnyMethod)
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Gets a value indicating if all origins are allowed.
    /// </summary>
    public bool AllowAnyOrigin
    {
        get
        {
            if (Origins == null || Origins.Count != 1 || Origins[0] != CorsConstants.AnyOrigin)
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Gets a value indicating if <see cref="IsOriginAllowed"/> is the default function that is set in the CorsPolicy constructor.
    /// </summary>
    internal bool IsDefaultIsOriginAllowed { get; private set; } = true;

    /// <summary>
    /// Gets or sets a function which evaluates whether an origin is allowed.
    /// </summary>
    public Func<string, bool> IsOriginAllowed
    {
        get
        {
            return _isOriginAllowed;
        }
        set
        {
            _isOriginAllowed = value;
            IsDefaultIsOriginAllowed = false;
        }
    }

    /// <summary>
    /// Gets the headers that the resource might use and can be exposed.
    /// </summary>
    public IList<string> ExposedHeaders { get; } = new List<string>();

    /// <summary>
    /// Gets the headers that are supported by the resource.
    /// </summary>
    public IList<string> Headers { get; } = new List<string>();

    /// <summary>
    /// Gets the methods that are supported by the resource.
    /// </summary>
    public IList<string> Methods { get; } = new List<string>();

    /// <summary>
    /// Gets the origins that are allowed to access the resource.
    /// </summary>
    public IList<string> Origins { get; } = new List<string>();

    /// <summary>
    /// Gets or sets the <see cref="TimeSpan"/> for which the results of a preflight request can be cached.
    /// </summary>
    public TimeSpan? PreflightMaxAge
    {
        get
        {
            return _preflightMaxAge;
        }
        set
        {
            if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), Resources.PreflightMaxAgeOutOfRange);
            }

            _preflightMaxAge = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the resource supports user credentials in the request.
    /// </summary>
    public bool SupportsCredentials { get; set; }

    /// <summary>
    /// Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="System.String" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append("AllowAnyHeader: ");
        builder.Append(AllowAnyHeader);
        builder.Append(", AllowAnyMethod: ");
        builder.Append(AllowAnyMethod);
        builder.Append(", AllowAnyOrigin: ");
        builder.Append(AllowAnyOrigin);
        builder.Append(", PreflightMaxAge: ");
        builder.Append(PreflightMaxAge.HasValue ?
            PreflightMaxAge.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture) : "null");
        builder.Append(", SupportsCredentials: ");
        builder.Append(SupportsCredentials);
        builder.Append(", Origins: {");
        builder.AppendJoin(",", Origins);
        builder.Append('}');
        builder.Append(", Methods: {");
        builder.AppendJoin(",", Methods);
        builder.Append('}');
        builder.Append(", Headers: {");
        builder.AppendJoin(",", Headers);
        builder.Append('}');
        builder.Append(", ExposedHeaders: {");
        builder.AppendJoin(",", ExposedHeaders);
        builder.Append('}');
        return builder.ToString();
    }

    private bool DefaultIsOriginAllowed(string origin)
    {
        return Origins.Contains(origin, StringComparer.Ordinal);
    }
}
