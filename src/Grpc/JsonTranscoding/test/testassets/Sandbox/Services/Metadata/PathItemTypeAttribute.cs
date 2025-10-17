namespace Sandbox.Services.Metadata
{
    public enum PathItemType
    {
        Standard,
        Webhook
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Delegate)]
    public class PathItemTypeAttribute : Attribute
    {
        public PathItemType Type { get; }
        public PathItemTypeAttribute(PathItemType type) => Type = type;
    }
}
