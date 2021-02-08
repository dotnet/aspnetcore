namespace Microsoft.AspNetCore.Components.Rendering
{
    /// <summary>
    /// The hosted state of the component
    /// </summary>
    public interface IComponentState
    {
        /// <summary>
        /// Used to notify a component of changes.
        /// </summary>
        /// <param name="lifetime"></param>
        void NotifyChanged(in ParameterView lifetime);
    }
}
