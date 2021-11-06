// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class CascadingGenericTypeParameter
{
    /// <summary>
    /// Gets or sets the type parameter names covered by the value type, e.g., TKey and TItem
    /// </summary>
    public IReadOnlyCollection<string> GenericTypeNames { get; set; }

    /// <summary>
    /// Gets or sets a <see cref="ComponentAttributeIntermediateNode"/> that supplies content for
    /// <see cref="ValueExpression"/>. In the case of explicitly-specified generic parameters, this
    /// will be null.
    /// </summary>
    internal ComponentAttributeIntermediateNode ValueSourceNode { get; set; }

    /// <summary>
    /// Gets or sets the type of <see cref="ValueExpression"/>, e.g., Dictionary[TKey, TItem].
    /// </summary>
    internal string ValueType { get; set; }

    /// <summary>
    /// Gets or sets an expression defining the type of the generic parameter. In the case of inferred
    /// generic parameters, this will only be populated once a variable is emitted corresponding to
    /// <see cref="ValueSourceNode"/>.
    /// </summary>
    internal string ValueExpression { get; set; }
}
