using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Indicates that a parameter should be bound using the FromFormOrJsonModelBinder.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class FromFormOrJsonAttribute : Attribute, IBindingSourceMetadata
{
    public BindingSource BindingSource => BindingSource.Custom;
}
