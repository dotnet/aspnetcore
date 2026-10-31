// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// Read / write <see cref="ModelBindingMessageProvider"/> implementation.
/// </summary>
public class DefaultModelBindingMessageProvider : ModelBindingMessageProvider
{
    private Func<string, string> _missingBindRequiredValueAccessor;
    private Func<string> _missingKeyOrValueAccessor;
    private Func<string> _missingRequestBodyRequiredValueAccessor;
    private Func<string, string> _valueMustNotBeNullAccessor;
    private Func<string, string, string> _attemptedValueIsInvalidAccessor;
    private Func<string, string> _nonPropertyAttemptedValueIsInvalidAccessor;
    private Func<string, string> _unknownValueIsInvalidAccessor;
    private Func<string> _nonPropertyUnknownValueIsInvalidAccessor;
    private Func<string, string> _valueIsInvalidAccessor;
    private Func<string, string> _valueMustBeANumberAccessor;
    private Func<string> _nonPropertyValueMustBeANumberAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultModelBindingMessageProvider"/> class.
    /// </summary>
    public DefaultModelBindingMessageProvider()
    {
        SetMissingBindRequiredValueAccessor(Resources.FormatModelBinding_MissingBindRequiredMember);
        SetMissingKeyOrValueAccessor(() => Resources.KeyValuePair_BothKeyAndValueMustBePresent);
        SetMissingRequestBodyRequiredValueAccessor(() => Resources.ModelBinding_MissingRequestBodyRequiredMember);
        SetValueMustNotBeNullAccessor(Resources.FormatModelBinding_NullValueNotValid);
        SetAttemptedValueIsInvalidAccessor(Resources.FormatModelState_AttemptedValueIsInvalid);
        SetNonPropertyAttemptedValueIsInvalidAccessor(Resources.FormatModelState_NonPropertyAttemptedValueIsInvalid);
        SetUnknownValueIsInvalidAccessor(Resources.FormatModelState_UnknownValueIsInvalid);
        SetNonPropertyUnknownValueIsInvalidAccessor(() => Resources.ModelState_NonPropertyUnknownValueIsInvalid);
        SetValueIsInvalidAccessor(Resources.FormatHtmlGeneration_ValueIsInvalid);
        SetValueMustBeANumberAccessor(Resources.FormatHtmlGeneration_ValueMustBeNumber);
        SetNonPropertyValueMustBeANumberAccessor(() => Resources.HtmlGeneration_NonPropertyValueMustBeNumber);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultModelBindingMessageProvider"/> class based on
    /// <paramref name="originalProvider"/>.
    /// </summary>
    /// <param name="originalProvider">The <see cref="DefaultModelBindingMessageProvider"/> to duplicate.</param>
    public DefaultModelBindingMessageProvider(DefaultModelBindingMessageProvider originalProvider)
    {
        ArgumentNullException.ThrowIfNull(originalProvider);

        SetMissingBindRequiredValueAccessor(originalProvider.MissingBindRequiredValueAccessor);
        SetMissingKeyOrValueAccessor(originalProvider.MissingKeyOrValueAccessor);
        SetMissingRequestBodyRequiredValueAccessor(originalProvider.MissingRequestBodyRequiredValueAccessor);
        SetValueMustNotBeNullAccessor(originalProvider.ValueMustNotBeNullAccessor);
        SetAttemptedValueIsInvalidAccessor(originalProvider.AttemptedValueIsInvalidAccessor);
        SetNonPropertyAttemptedValueIsInvalidAccessor(originalProvider.NonPropertyAttemptedValueIsInvalidAccessor);
        SetUnknownValueIsInvalidAccessor(originalProvider.UnknownValueIsInvalidAccessor);
        SetNonPropertyUnknownValueIsInvalidAccessor(originalProvider.NonPropertyUnknownValueIsInvalidAccessor);
        SetValueIsInvalidAccessor(originalProvider.ValueIsInvalidAccessor);
        SetValueMustBeANumberAccessor(originalProvider.ValueMustBeANumberAccessor);
        SetNonPropertyValueMustBeANumberAccessor(originalProvider.NonPropertyValueMustBeANumberAccessor);
    }

    /// <inheritdoc/>
    public override Func<string, string> MissingBindRequiredValueAccessor => _missingBindRequiredValueAccessor;

    /// <summary>
    /// Sets the <see cref="MissingBindRequiredValueAccessor"/> property.
    /// </summary>
    /// <param name="missingBindRequiredValueAccessor">The value to set.</param>
    [MemberNotNull(nameof(_missingBindRequiredValueAccessor))]
    public void SetMissingBindRequiredValueAccessor(Func<string, string> missingBindRequiredValueAccessor)
    {
        ArgumentNullException.ThrowIfNull(missingBindRequiredValueAccessor);

        _missingBindRequiredValueAccessor = missingBindRequiredValueAccessor;
    }

    /// <inheritdoc/>
    public override Func<string> MissingKeyOrValueAccessor => _missingKeyOrValueAccessor;

    /// <summary>
    /// Sets the <see cref="MissingKeyOrValueAccessor"/> property.
    /// </summary>
    /// <param name="missingKeyOrValueAccessor">The value to set.</param>
    [MemberNotNull(nameof(_missingKeyOrValueAccessor))]
    public void SetMissingKeyOrValueAccessor(Func<string> missingKeyOrValueAccessor)
    {
        ArgumentNullException.ThrowIfNull(missingKeyOrValueAccessor);

        _missingKeyOrValueAccessor = missingKeyOrValueAccessor;
    }

    /// <inheritdoc/>
    public override Func<string> MissingRequestBodyRequiredValueAccessor => _missingRequestBodyRequiredValueAccessor;

    /// <summary>
    /// Sets the <see cref="MissingRequestBodyRequiredValueAccessor"/> property.
    /// </summary>
    /// <param name="missingRequestBodyRequiredValueAccessor">The value to set.</param>
    [MemberNotNull(nameof(_missingRequestBodyRequiredValueAccessor))]
    public void SetMissingRequestBodyRequiredValueAccessor(Func<string> missingRequestBodyRequiredValueAccessor)
    {
        ArgumentNullException.ThrowIfNull(missingRequestBodyRequiredValueAccessor);

        _missingRequestBodyRequiredValueAccessor = missingRequestBodyRequiredValueAccessor;
    }

    /// <inheritdoc/>
    public override Func<string, string> ValueMustNotBeNullAccessor => _valueMustNotBeNullAccessor;

    /// <summary>
    /// Sets the <see cref="ValueMustNotBeNullAccessor"/> property.
    /// </summary>
    /// <param name="valueMustNotBeNullAccessor">The value to set.</param>
    [MemberNotNull(nameof(_valueMustNotBeNullAccessor))]
    public void SetValueMustNotBeNullAccessor(Func<string, string> valueMustNotBeNullAccessor)
    {
        ArgumentNullException.ThrowIfNull(valueMustNotBeNullAccessor);

        _valueMustNotBeNullAccessor = valueMustNotBeNullAccessor;
    }

    /// <inheritdoc/>
    public override Func<string, string, string> AttemptedValueIsInvalidAccessor => _attemptedValueIsInvalidAccessor;

    /// <summary>
    /// Sets the <see cref="AttemptedValueIsInvalidAccessor"/> property.
    /// </summary>
    /// <param name="attemptedValueIsInvalidAccessor">The value to set.</param>
    [MemberNotNull(nameof(_attemptedValueIsInvalidAccessor))]
    public void SetAttemptedValueIsInvalidAccessor(Func<string, string, string> attemptedValueIsInvalidAccessor)
    {
        ArgumentNullException.ThrowIfNull(attemptedValueIsInvalidAccessor);

        _attemptedValueIsInvalidAccessor = attemptedValueIsInvalidAccessor;
    }

    /// <inheritdoc/>
    public override Func<string, string> NonPropertyAttemptedValueIsInvalidAccessor => _nonPropertyAttemptedValueIsInvalidAccessor;

    /// <summary>
    /// Sets the <see cref="NonPropertyAttemptedValueIsInvalidAccessor"/> property.
    /// </summary>
    /// <param name="nonPropertyAttemptedValueIsInvalidAccessor">The value to set.</param>
    [MemberNotNull(nameof(_nonPropertyAttemptedValueIsInvalidAccessor))]
    public void SetNonPropertyAttemptedValueIsInvalidAccessor(
        Func<string, string> nonPropertyAttemptedValueIsInvalidAccessor)
    {
        ArgumentNullException.ThrowIfNull(nonPropertyAttemptedValueIsInvalidAccessor);

        _nonPropertyAttemptedValueIsInvalidAccessor = nonPropertyAttemptedValueIsInvalidAccessor;
    }

    /// <inheritdoc/>
    public override Func<string, string> UnknownValueIsInvalidAccessor => _unknownValueIsInvalidAccessor;

    /// <summary>
    /// Sets the <see cref="UnknownValueIsInvalidAccessor"/> property.
    /// </summary>
    /// <param name="unknownValueIsInvalidAccessor">The value to set.</param>
    [MemberNotNull(nameof(_unknownValueIsInvalidAccessor))]
    public void SetUnknownValueIsInvalidAccessor(Func<string, string> unknownValueIsInvalidAccessor)
    {
        ArgumentNullException.ThrowIfNull(unknownValueIsInvalidAccessor);

        _unknownValueIsInvalidAccessor = unknownValueIsInvalidAccessor;
    }

    /// <inheritdoc/>
    public override Func<string> NonPropertyUnknownValueIsInvalidAccessor => _nonPropertyUnknownValueIsInvalidAccessor;

    /// <summary>
    /// Sets the <see cref="NonPropertyUnknownValueIsInvalidAccessor"/> property.
    /// </summary>
    /// <param name="nonPropertyUnknownValueIsInvalidAccessor">The value to set.</param>
    [MemberNotNull(nameof(_nonPropertyUnknownValueIsInvalidAccessor))]
    public void SetNonPropertyUnknownValueIsInvalidAccessor(Func<string> nonPropertyUnknownValueIsInvalidAccessor)
    {
        ArgumentNullException.ThrowIfNull(nonPropertyUnknownValueIsInvalidAccessor);

        _nonPropertyUnknownValueIsInvalidAccessor = nonPropertyUnknownValueIsInvalidAccessor;
    }

    /// <inheritdoc/>
    public override Func<string, string> ValueIsInvalidAccessor => _valueIsInvalidAccessor;

    /// <summary>
    /// Sets the <see cref="ValueIsInvalidAccessor"/> property.
    /// </summary>
    /// <param name="valueIsInvalidAccessor">The value to set.</param>
    [MemberNotNull(nameof(_valueIsInvalidAccessor))]
    public void SetValueIsInvalidAccessor(Func<string, string> valueIsInvalidAccessor)
    {
        ArgumentNullException.ThrowIfNull(valueIsInvalidAccessor);

        _valueIsInvalidAccessor = valueIsInvalidAccessor;
    }

    /// <inheritdoc/>
    public override Func<string, string> ValueMustBeANumberAccessor => _valueMustBeANumberAccessor;

    /// <summary>
    /// Sets the <see cref="ValueMustBeANumberAccessor"/> property.
    /// </summary>
    /// <param name="valueMustBeANumberAccessor">The value to set.</param>
    [MemberNotNull(nameof(_valueMustBeANumberAccessor))]
    public void SetValueMustBeANumberAccessor(Func<string, string> valueMustBeANumberAccessor)
    {
        ArgumentNullException.ThrowIfNull(valueMustBeANumberAccessor);

        _valueMustBeANumberAccessor = valueMustBeANumberAccessor;
    }

    /// <inheritdoc/>
    public override Func<string> NonPropertyValueMustBeANumberAccessor => _nonPropertyValueMustBeANumberAccessor;

    /// <summary>
    /// Sets the <see cref="NonPropertyValueMustBeANumberAccessor"/> property.
    /// </summary>
    /// <param name="nonPropertyValueMustBeANumberAccessor">The value to set.</param>
    [MemberNotNull(nameof(_nonPropertyValueMustBeANumberAccessor))]
    public void SetNonPropertyValueMustBeANumberAccessor(Func<string> nonPropertyValueMustBeANumberAccessor)
    {
        ArgumentNullException.ThrowIfNull(nonPropertyValueMustBeANumberAccessor);

        _nonPropertyValueMustBeANumberAccessor = nonPropertyValueMustBeANumberAccessor;
    }
}
