# Microsoft.Extensions.Logging.Console.Internal

``` diff
-namespace Microsoft.Extensions.Logging.Console.Internal {
 {
-    public class AnsiLogConsole : IConsole {
 {
-        public AnsiLogConsole(IAnsiSystemConsole systemConsole);

-        public void Flush();

-        public void Write(string message, Nullable<ConsoleColor> background, Nullable<ConsoleColor> foreground);

-        public void WriteLine(string message, Nullable<ConsoleColor> background, Nullable<ConsoleColor> foreground);

-    }
-    public class ConsoleLoggerProcessor : IDisposable {
 {
-        public IConsole Console;

-        public ConsoleLoggerProcessor();

-        public void Dispose();

-        public virtual void EnqueueMessage(LogMessageEntry message);

-    }
-    public interface IAnsiSystemConsole {
 {
-        void Write(string message);

-        void WriteLine(string message);

-    }
-    public interface IConsole {
 {
-        void Flush();

-        void Write(string message, Nullable<ConsoleColor> background, Nullable<ConsoleColor> foreground);

-        void WriteLine(string message, Nullable<ConsoleColor> background, Nullable<ConsoleColor> foreground);

-    }
-    public struct LogMessageEntry {
 {
-        public Nullable<ConsoleColor> LevelBackground;

-        public Nullable<ConsoleColor> LevelForeground;

-        public Nullable<ConsoleColor> MessageColor;

-        public string LevelString;

-        public string Message;

-    }
-    public class WindowsLogConsole : IConsole {
 {
-        public WindowsLogConsole();

-        public void Flush();

-        public void Write(string message, Nullable<ConsoleColor> background, Nullable<ConsoleColor> foreground);

-        public void WriteLine(string message, Nullable<ConsoleColor> background, Nullable<ConsoleColor> foreground);

-    }
-}
```

