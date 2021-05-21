// Pending API review

namespace System.Threading.ResourceLimits
{
    public struct Resource : IDisposable
    {
        public bool IsAcquired { get; }

        public object? State { get; }

        private Action<object?>? _onDispose;

        public Resource(bool isAcquired, object? state, Action<object?>? onDispose)
        {
            IsAcquired = isAcquired;
            State = state;
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose?.Invoke(State);
        }

        public static Resource SuccessNoopResource = new Resource(true, null, null);
        public static Resource FailNoopResource = new Resource(false, null, null);
    }
}
