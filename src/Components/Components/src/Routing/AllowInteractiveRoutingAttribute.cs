namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// Indicates whether the component should be matched by <see cref="Router"/>.
/// If this attribute is not present, <see cref="Router"/> will match it by default.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AllowInteractiveRoutingAttribute : Attribute
{
    /// <summary>
    /// Constructs an instance of <see cref="AllowInteractiveRoutingAttribute"/>.
    /// </summary>
    /// <param name="allow">Specifies whether the component should be matched by <see cref="Router"/>.</param>
    public AllowInteractiveRoutingAttribute(bool allow)
    {
        Allow = allow;
    }

    /// <summary>
    /// Gets a flag that indicates whether the component should be matched by <see cref="Router"/>.
    /// </summary>
    public bool Allow { get; }
}
