namespace Microsoft.AspNetCore.Components.Sections
{
    internal interface ISectionContentProvider
    {
        RenderFragment? Content { get; }
    }
}
