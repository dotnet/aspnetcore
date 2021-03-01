namespace Microsoft.AspNetCore.Components.WebView.Document
{
    internal class MarkupNode : TestNode
    {
        public MarkupNode(string markupContent)
        {
            Content = markupContent;
        }

        public string Content { get; }
    }
}
