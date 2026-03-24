// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Provides a mechanism for an object to perform asynchronous validation of its state.
/// This is the async counterpart to <see cref="IValidatableObject"/>.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on model classes that require asynchronous cross-property
/// validation — for example, checking a combination of field values against a database.
/// </para>
/// <para>
/// The <see cref="ValidateAsync"/> method is called by the <c>Microsoft.Extensions.Validation</c>
/// pipeline after all property-level validation has completed, mirroring how
/// <see cref="IValidatableObject.Validate"/> is called after attribute-level validation.
/// </para>
/// <example>
/// <code>
/// public class Registration : IAsyncValidatableObject
/// {
///     public string Email { get; set; }
///     public string Username { get; set; }
///
///     public async Task&lt;IEnumerable&lt;ValidationResult&gt;&gt; ValidateAsync(
///         ValidationContext validationContext, CancellationToken cancellationToken)
///     {
///         var db = validationContext.GetRequiredService&lt;IUserRepository&gt;();
///         var errors = new List&lt;ValidationResult&gt;();
///
///         if (await db.EmailExistsAsync(Email, cancellationToken))
///             errors.Add(new ValidationResult("Email is taken.", new[] { nameof(Email) }));
///
///         return errors;
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface IAsyncValidatableObject
{
    /// <summary>
    /// Validates the current instance asynchronously and returns a collection of validation results.
    /// </summary>
    /// <param name="validationContext">The context information about the validation operation.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the async validation operation.</param>
    /// <returns>A collection of <see cref="ValidationResult"/> instances; an empty collection indicates success.</returns>
    Task<IEnumerable<ValidationResult>> ValidateAsync(
        ValidationContext validationContext,
        CancellationToken cancellationToken);
}
