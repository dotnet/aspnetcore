namespace Microsoft.AspNetCore.Components
{
    // This is a temporary workaround for the fact that public previews of VS look for
    // this type. Without this the tooling won't understand bind or event handlers.
    //
    // This has already gotten better in 16.3 and we look for IComponent rather
    // than specific implementation details.
    public static class BindMethods
    {
    }
}
