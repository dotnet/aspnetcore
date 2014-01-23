using System;

namespace Microsoft.AspNet.Abstractions
{
    public abstract class HttpContext : IDisposable
    {
        // TODO - review IOwinContext for properties

        public abstract HttpRequest Request { get; }
        public abstract HttpResponse Response { get; }

        public abstract void Dispose();

        public abstract object GetInterface(Type type);

        public abstract void SetInterface(Type type, object instance);

        public virtual T GetInterface<T>()
        {
            return (T)GetInterface(typeof(T));
        }

        public virtual void SetInterface<T>(T instance)
        {
            SetInterface(typeof(T), instance);
        }
    }
}
