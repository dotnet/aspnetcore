using System;

/// <summary>
/// Interface for ComponentBroadcast
/// </summary>
internal interface IComponentBroadcast
{
    event Action RefreshRequested;
    void CallRequestRefresh();
}
