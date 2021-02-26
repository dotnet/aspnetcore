namespace Microsoft.AspNetCore.Components.WebView.Document
{
    internal class RootComponentNode : ComponentNode
    {
        public RootComponentNode(int componentId, string selector) : base(componentId)
        {
            Selector = selector;
        }

        public string Selector { get; }
    }
}
