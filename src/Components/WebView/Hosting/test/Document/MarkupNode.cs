namespace Microsoft.AspNetCore.Components.WebView.Headless.Document
{
    internal class MarkupNode : HeadlessNode
    {
        public MarkupNode(string markupContent)
        {
            Content = markupContent;
        }

        public string Content { get; }
    }
}
