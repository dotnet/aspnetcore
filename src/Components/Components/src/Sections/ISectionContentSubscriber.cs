namespace Microsoft.AspNetCore.Components.Sections
{
    internal interface ISectionContentSubscriber
    {
        void ContentChanged(RenderFragment? content);
    }
}
