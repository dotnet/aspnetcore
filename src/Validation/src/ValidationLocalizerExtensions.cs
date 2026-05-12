// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation;

internal static class ValidationLocalizerExtensions
{
    extension(IValidationLocalizer? localizer)
    {
        /// <summary>
        /// Resolves the error message for a failed validation, optionally going through
        /// <see cref="IValidationLocalizer.ResolveErrorMessage"/> when a localizer is configured
        /// and the attribute does not already use <see cref="ValidationAttribute.ErrorMessageResourceType"/>.
        /// </summary>
        /// <remarks>
        /// The localizer is bypassed when:
        /// <list type="bullet">
        ///   <item><description>no localizer is configured (<paramref name="localizer"/> is <see langword="null"/>), or</description></item>
        ///   <item><description>the attribute already resolves its message via <see cref="ValidationAttribute.ErrorMessageResourceType"/>,
        ///   in which case <c>DataAnnotations</c> has already produced the localized message in
        ///   <see cref="ValidationResult.ErrorMessage"/>.</description></item>
        /// </list>
        /// </remarks>
        internal string? ResolveAttributeErrorMessage(
            string memberName,
            string displayName,
            Type? declaringType,
            ValidationAttribute attribute,
            ValidationResult result)
        {
            if (localizer is null || attribute.ErrorMessageResourceType is not null)
            {
                return result.ErrorMessage;
            }

            return localizer.ResolveErrorMessage(new ErrorMessageLocalizationContext
            {
                MemberName = memberName,
                DisplayName = displayName,
                DeclaringType = declaringType,
                Attribute = attribute,
            }) ?? result.ErrorMessage;
        }
    }
}
