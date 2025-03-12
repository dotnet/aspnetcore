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
            compilation.GetTypeByMetadataName("System.Text.Json.Serialization.JsonDerivedTypeAttribute")!,
            compilation.GetTypeByMetadataName("System.ComponentModel.DataAnnotations.RequiredAttribute")!,
            compilation.GetTypeByMetadataName("System.ComponentModel.DataAnnotations.CustomValidationAttribute")!,
            compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.HttpContext")!,
            compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.HttpRequest")!,
            compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.HttpResponse")!,
            compilation.GetTypeByMetadataName("System.Threading.CancellationToken")!,
            compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.IFormCollection")!,
            compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.IFormFileCollection")!,
            compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.IFormFile")!,
            compilation.GetTypeByMetadataName("System.IO.Stream")!,
            compilation.GetTypeByMetadataName("System.IO.Pipelines.PipeReader")!
        );
    }
}
