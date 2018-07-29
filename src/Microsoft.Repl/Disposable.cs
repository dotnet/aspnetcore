using System;

namespace Microsoft.Repl
{
    public class Disposable : IDisposable
    {
        private Action _onDispose;

        public Disposable(Action onDispose)
        {
            _onDispose = onDispose;
        }
        public virtual void Dispose()
        {
            _onDispose?.Invoke();
            _onDispose = null;
        }
    }

    public class Disposable<T> : Disposable
        where T : class
    {
        public Disposable(T value, Action onDispose)
            : base (onDispose)
        {
            Value = value;
        }

        public T Value { get; private set; }

        public override void Dispose()
        {
            if (Value is IDisposable d)
            {
                d.Dispose();
                Value = null;
            }

            base.Dispose();
        }
    }
}
