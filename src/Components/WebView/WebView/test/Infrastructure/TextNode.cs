namespace Microsoft.AspNetCore.Components.WebView.Document
{
    internal class TextNode : TestNode
    {
        public TextNode(string textContent)
        {
            Text = textContent;
        }

        public string Text { get; set; }
    }
}
