namespace Microsoft.AspNet.Mvc
{
    // Abstracts out the serialization process for an anti-forgery token
    internal interface IAntiForgeryTokenSerializer
    {
        AntiForgeryToken Deserialize(string serializedToken);
        string Serialize(AntiForgeryToken token);
    }
}