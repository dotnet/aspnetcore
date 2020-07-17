using System;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    internal static class AccessibilityModificatorDirectives
    {
        public static readonly DirectiveDescriptor InternalDirective = DirectiveDescriptor.CreateDirective(
            "internal",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
                builder.Description = "Add internal access modifier for component type";
            });

        public static readonly DirectiveDescriptor PublicDirective = DirectiveDescriptor.CreateDirective(
            "public",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
                builder.Description = "Add public access modifier for component type";
            });

        public static readonly DirectiveDescriptor PrivateDirective = DirectiveDescriptor.CreateDirective(
            "private",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
                builder.Description = "Add private access modifier for component type";
            });

        public static void Register(RazorProjectEngineBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddDirective(InternalDirective, FileKinds.Legacy, FileKinds.Component, FileKinds.ComponentImport);
            builder.AddDirective(PrivateDirective, FileKinds.Legacy, FileKinds.Component, FileKinds.ComponentImport);
            builder.AddDirective(PublicDirective, FileKinds.Legacy, FileKinds.Component, FileKinds.ComponentImport);
            builder.Features.Add(new AccessibilityModificatorDirectivesPass());
        }
    }
}
