// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// The validation state of a <see cref="ModelStateEntry"/> instance.
/// <para>
/// <see cref="ModelValidationState"/> of <see cref="ModelStateDictionary.Root"/> is used to determine the validity
/// of <see cref="ModelStateDictionary"/>. <see cref="ModelStateDictionary.IsValid"/> is <see langword="true" />, when
/// the aggregate validity (<see cref="ModelStateDictionary.GetFieldValidationState(string)"/>)
/// of the root node is <see cref="Valid"/>.
/// </para>
/// </summary>
public enum ModelValidationState
{
    /// <summary>
    /// Validation has not been performed on the <see cref="ModelStateEntry"/>.
    /// <para>
    /// For aggregate validity, the validation of a <see cref="ModelStateEntry"/> is <see cref="Unvalidated"/>
    /// if either the entry or one of thedescendants is <see cref="Unvalidated"/>.
    /// </para>
    /// </summary>
    Unvalidated,

    /// <summary>
    /// Validation was performed on the <see cref="ModelStateEntry"/> and was found to be invalid.
    /// <para>
    /// For aggregate validity, the validation of a <see cref="ModelStateEntry"/> is <see cref="Invalid"/>
    /// if either the entry or one of the descendants is <see cref="Invalid"/> and none are <see cref="Unvalidated"/>.
    /// </para>
    /// </summary>
    Invalid,

    /// <summary>
    /// Validation was performed on the <see cref="ModelStateEntry"/>
    /// <para>
    /// For aggregate validity, the validation of a <see cref="ModelStateEntry"/> is <see cref="Valid"/>
    /// if the validity of the entry and all descendants is either <see cref="Valid"/> or <see cref="Skipped"/>.
    /// </para>
    /// </summary>
    Valid,

    /// <summary>
    /// Validation was skipped for the <see cref="ModelStateEntry"/>.
    /// <para>
    /// The aggregate validity of an entry is never <see cref="Skipped"/>.
    /// </para>
    /// </summary>
    Skipped
}
