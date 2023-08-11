// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal class FormDataMappingException : Exception
{
    public FormDataMappingException(FormDataMappingError error) : this(error, null) { }

    public FormDataMappingException(FormDataMappingError error, Exception? innerException)
        : base(FormDataResources.MappingExceptionMessage, innerException)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
    }

    public FormDataMappingError Error { get; }
}
