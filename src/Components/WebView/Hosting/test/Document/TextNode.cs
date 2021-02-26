namespace Microsoft.AspNetCore.Components.WebView.Headless.Document
{
    internal class TextNode : HeadlessNode
    {
        public TextNode(string textContent)
        {
            Text = textContent;
        }

        public string Text { get; set; }
    }
}
