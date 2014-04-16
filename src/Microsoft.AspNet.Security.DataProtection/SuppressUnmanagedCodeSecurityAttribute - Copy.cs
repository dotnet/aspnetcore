using System;
using System.Runtime.InteropServices;

#if !NET45
namespace System.Security
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class SuppressUnmanagedCodeSecurityAttribute : Attribute { }
}
#endif
