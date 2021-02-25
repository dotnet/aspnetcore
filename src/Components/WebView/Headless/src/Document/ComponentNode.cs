namespace Microsoft.AspNetCore.Components.WebView.Headless.Document
{
    internal class ComponentNode : ContainerNode
    {
        public ComponentNode(int componentId)
        {
            ComponentId = componentId;
        }

        public int ComponentId { get; }
    }
}
