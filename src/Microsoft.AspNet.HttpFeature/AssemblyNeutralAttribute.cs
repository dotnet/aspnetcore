using System;

namespace Microsoft.Net.Runtime
{
    [AssemblyNeutralAttribute]
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class AssemblyNeutralAttribute : Attribute
    {
    }
}