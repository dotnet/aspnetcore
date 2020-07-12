using System;

/// <summary>
/// ComponentBroadcast used to Broadcast all components to make refresh
/// </summary>
internal sealed class ComponentBroadcast : IComponentBroadcast
{
    private static readonly Lazy<ComponentBroadcast>
        Lazy =
            new Lazy<ComponentBroadcast>
                (() => new ComponentBroadcast());

    public static ComponentBroadcast Instance => Lazy.Value;

    private ComponentBroadcast()
    {
    }

    public event Action RefreshRequested;
    public void CallRequestRefresh()
    {
        RefreshRequested?.Invoke();
    }
}
