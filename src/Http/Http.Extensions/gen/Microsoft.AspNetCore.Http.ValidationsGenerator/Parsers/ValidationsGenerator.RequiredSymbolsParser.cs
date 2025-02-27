// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

public sealed partial class ValidationsGenerator : IIncrementalGenerator
{
    internal RequiredSymbols ExtractRequiredSymbols(Compilation compilation, CancellationToken cancellationToken)
    {
        return new RequiredSymbols(
            compilation.GetTypeByMetadataName("System.ComponentModel.DataAnnotations.DisplayAttribute")!,
            compilation.GetTypeByMetadataName("System.ComponentModel.DataAnnotations.ValidationAttribute")!,
            compilation.GetTypeByMetadataName("System.Collections.IEnumerable")!,
            compilation.GetTypeByMetadataName("System.ComponentModel.DataAnnotations.IValidatableObject")!,
            compilation.GetTypeByMetadataName("System.Text.Json.Serialization.JsonDerivedTypeAttribute")!
        );
    }
}
