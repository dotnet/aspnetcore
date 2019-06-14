# Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl

``` diff
-namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl {
 {
-    public struct FlowControl {
 {
-        public FlowControl(uint initialWindowSize);

-        public int Available { get; private set; }

-        public bool IsAborted { get; private set; }

-        public void Abort();

-        public void Advance(int bytes);

-        public bool TryUpdateWindow(int bytes);

-    }
-    public class InputFlowControl {
 {
-        public InputFlowControl(uint initialWindowSize, uint minWindowSizeIncrement);

-        public bool IsAvailabilityLow { get; }

-        public int Abort();

-        public void StopWindowUpdates();

-        public bool TryAdvance(int bytes);

-        public bool TryUpdateWindow(int bytes, out int updateSize);

-    }
-    public class OutputFlowControl {
 {
-        public OutputFlowControl(uint initialWindowSize);

-        public OutputFlowControlAwaitable AvailabilityAwaitable { get; }

-        public int Available { get; }

-        public bool IsAborted { get; }

-        public void Abort();

-        public void Advance(int bytes);

-        public bool TryUpdateWindow(int bytes);

-    }
-    public class OutputFlowControlAwaitable : ICriticalNotifyCompletion, INotifyCompletion {
 {
-        public OutputFlowControlAwaitable();

-        public bool IsCompleted { get; }

-        public void Complete();

-        public OutputFlowControlAwaitable GetAwaiter();

-        public void GetResult();

-        public void OnCompleted(Action continuation);

-        public void UnsafeOnCompleted(Action continuation);

-    }
-    public class StreamInputFlowControl {
 {
-        public StreamInputFlowControl(int streamId, Http2FrameWriter frameWriter, InputFlowControl connectionLevelFlowControl, uint initialWindowSize, uint minWindowSizeIncrement);

-        public void Abort();

-        public void Advance(int bytes);

-        public void StopWindowUpdates();

-        public void UpdateWindows(int bytes);

-    }
-    public class StreamOutputFlowControl {
 {
-        public StreamOutputFlowControl(OutputFlowControl connectionLevelFlowControl, uint initialWindowSize);

-        public int Available { get; }

-        public bool IsAborted { get; }

-        public void Abort();

-        public void Advance(int bytes);

-        public int AdvanceUpToAndWait(long bytes, out OutputFlowControlAwaitable awaitable);

-        public bool TryUpdateWindow(int bytes);

-    }
-}
```

