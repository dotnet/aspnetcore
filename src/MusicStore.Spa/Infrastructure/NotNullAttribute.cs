using System;

namespace MusicStore.Spa.Infrastructure
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    internal sealed class NotNullAttribute : Attribute
    {
    }
}