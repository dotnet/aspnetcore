// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the result of an identity operation.
/// </summary>
public class IdentityResult
{
    private static readonly IdentityResult _success = new IdentityResult { Succeeded = true };
    private readonly List<IdentityError> _errors = new List<IdentityError>();

    /// <summary>
    /// Flag indicating whether if the operation succeeded or not.
    /// </summary>
    /// <value>True if the operation succeeded, otherwise false.</value>
    public bool Succeeded { get; protected set; }

    /// <summary>
    /// An <see cref="IEnumerable{T}"/> of <see cref="IdentityError"/> instances containing errors
    /// that occurred during the identity operation.
    /// </summary>
    /// <value>An <see cref="IEnumerable{T}"/> of <see cref="IdentityError"/> instances.</value>
    public IEnumerable<IdentityError> Errors => _errors;

    /// <summary>
    /// Returns an <see cref="IdentityResult"/> indicating a successful identity operation.
    /// </summary>
    /// <returns>An <see cref="IdentityResult"/> indicating a successful operation.</returns>
    public static IdentityResult Success => _success;

    /// <summary>
    /// Creates an <see cref="IdentityResult"/> indicating a failed identity operation, with a list of <paramref name="errors"/> if applicable.
    /// </summary>
    /// <param name="errors">An optional array of <see cref="IdentityError"/>s which caused the operation to fail.</param>
    /// <returns>An <see cref="IdentityResult"/> indicating a failed identity operation, with a list of <paramref name="errors"/> if applicable.</returns>
    public static IdentityResult Failed(params IdentityError[] errors)
    {
        var result = new IdentityResult { Succeeded = false };
        if (errors != null)
        {
            result._errors.AddRange(errors);
        }
        return result;
    }

    internal static IdentityResult Failed(List<IdentityError>? errors)
    {
        var result = new IdentityResult { Succeeded = false };
        if (errors != null)
        {
            result._errors.AddRange(errors);
        }
        return result;
    }

    /// <summary>
    /// Converts the value of the current <see cref="IdentityResult"/> object to its equivalent string representation.
    /// </summary>
    /// <returns>A string representation of the current <see cref="IdentityResult"/> object.</returns>
    /// <remarks>
    /// If the operation was successful the ToString() will return "Succeeded" otherwise it returned
    /// "Failed : " followed by a comma delimited list of error codes from its <see cref="Errors"/> collection, if any.
    /// </remarks>
    public override string ToString()
    {
        return Succeeded ?
               "Succeeded" :
               string.Format(CultureInfo.InvariantCulture, "{0} : {1}", "Failed", string.Join(",", Errors.Select(x => x.Code).ToList()));
    }
}
