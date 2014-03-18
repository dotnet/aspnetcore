using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Abstractions
{
    public abstract class HttpContext : IDisposable
    {
        public abstract HttpRequest Request { get; }

        public abstract HttpResponse Response { get; }
        
        public abstract IDictionary<object, object> Items { get; }

        public abstract IServiceProvider ApplicationServices { get; set; }

        public abstract IServiceProvider RequestServices { get; set; }

        public abstract void Dispose();

        public abstract object GetFeature(Type type);

        public abstract void SetFeature(Type type, object instance);

        public virtual T GetFeature<T>()
        {
            return (T)GetFeature(typeof(T));
        }

        public virtual void SetFeature<T>(T instance)
        {
            SetFeature(typeof(T), instance);
        }
    }
}
