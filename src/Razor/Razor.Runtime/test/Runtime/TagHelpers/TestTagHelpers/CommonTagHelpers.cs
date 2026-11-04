// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.TagHelpers;

public class Valid_PlainTagHelper : TagHelper
{
}

public class Valid_InheritedTagHelper : Valid_PlainTagHelper
{
}

public class SingleAttributeTagHelper : TagHelper
{
    public int IntAttribute { get; set; }
}

public class MissingAccessorTagHelper : TagHelper
{
    public string ValidAttribute { get; set; }
    public string InvalidNoGetAttribute { set { } }
    public string InvalidNoSetAttribute { get { return string.Empty; } }
}

public class NonPublicAccessorTagHelper : TagHelper
{
    public string ValidAttribute { get; set; }
    public string InvalidPrivateSetAttribute { get; private set; }
    public string InvalidPrivateGetAttribute { private get; set; }
    protected string InvalidProtectedAttribute { get; set; }
    internal string InvalidInternalAttribute { get; set; }
    protected internal string InvalidProtectedInternalAttribute { get; set; }
}

/// <summary>
/// The summary for <see cref="DocumentedTagHelper"/>.
/// </summary>
/// <remarks>
/// Inherits from <see cref="TagHelper"/>.
/// </remarks>
[OutputElementHint("p")]
public class DocumentedTagHelper : TagHelper
{
    /// <summary>
    /// This <see cref="SummaryProperty"/> is of type <see cref="string"/>.
    /// </summary>
    public string SummaryProperty { get; set; }

    /// <remarks>
    /// The <see cref="SummaryProperty"/> may be <c>null</c>.
    /// </remarks>
    public int RemarksProperty { get; set; }

    /// <summary>
    /// This is a complex <see cref="IDictionary{string, bool}"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="SummaryProperty"/><see cref="RemarksProperty"/>
    /// </remarks>
    public IDictionary<string, bool> RemarksAndSummaryProperty { get; set; }

    public bool UndocumentedProperty { get; set; }
}
