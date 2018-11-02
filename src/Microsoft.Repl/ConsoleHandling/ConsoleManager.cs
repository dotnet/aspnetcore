// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.Repl.ConsoleHandling
{
    public class ConsoleManager : IConsoleManager
    {
        private readonly List<Action> _breakHandlers = new List<Action>();

        public Point Caret => new Point(Console.CursorLeft, Console.CursorTop);

        public Point CommandStart => new Point(Caret.X - CaretPosition % Console.BufferWidth, Caret.Y - CaretPosition / Console.BufferWidth);

        public int CaretPosition { get; private set; }

        public bool IsKeyAvailable => Console.KeyAvailable;

        public bool IsCaretVisible
        {
            get => Reporter.Output.IsCaretVisible;
            set => Reporter.Output.IsCaretVisible = value;
        }

        public ConsoleManager()
        {
            Error = new Writable(CaretUpdateScope, Reporter.Error);
            Console.CancelKeyPress += OnCancelKeyPress;
        }

        public void Clear()
        {
            using (CaretUpdateScope())
            {
                Console.Clear();
                ResetCommandStart();
            }
        }

        public void MoveCaret(int positions)
        {
            using (CaretUpdateScope())
            {
                if (positions == 0)
                {
                    return;
                }

                int bufferWidth = Console.BufferWidth;
                int cursorTop = Console.CursorTop;
                int cursorLeft = Console.CursorLeft;

                while (positions < 0 && CaretPosition > 0)
                {
                    if (-positions > bufferWidth)
                    {
                        if (cursorTop == 0)
                        {
                            cursorLeft = 0;
                            positions = 0;
                        }
                        else
                        {
                            positions += bufferWidth;
                            --cursorTop;
                        }
                    }
                    else
                    {
                        int remaining = cursorLeft + positions;

                        if (remaining >= 0)
                        {
                            cursorLeft = remaining;
                        }
                        else if (cursorTop == 0)
                        {
                            cursorLeft = 0;
                        }
                        else
                        {
                            --cursorTop;
                            cursorLeft = bufferWidth + remaining;
                        }

                        positions = 0;
                    }
                }

                while (positions > 0)
                {
                    if (positions > bufferWidth)
                    {
                        positions -= bufferWidth;
                        ++cursorTop;
                    }
                    else
                    {
                        int spaceLeftOnLine = bufferWidth - cursorLeft - 1;
                        if (positions > spaceLeftOnLine)
                        {
                            ++cursorTop;
                            cursorLeft = positions - spaceLeftOnLine - 1;
                        }
                        else
                        {
                            cursorLeft += positions;
                        }

                        positions = 0;
                    }
                }

                Console.SetCursorPosition(cursorLeft, cursorTop);
            }
        }

        public ConsoleKeyInfo ReadKey(CancellationToken cancellationToken)
        {
            while (!Console.KeyAvailable && !cancellationToken.IsCancellationRequested)
            {
                Thread.Sleep(2);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return default(ConsoleKeyInfo);
            }
            else
            {
                return Console.ReadKey(true);
            }
        }

        public void ResetCommandStart()
        {
            CaretPosition = 0;
        }

        public void Write(char c)
        {
            using (CaretUpdateScope())
            {
                Reporter.Output.Write(c);
            }
        }

        public void Write(string s)
        {
            using (CaretUpdateScope())
            {
                Reporter.Output.Write(s);
            }
        }

        public void WriteLine()
        {
            using (CaretUpdateScope())
            {
                Reporter.Output.WriteLine();
            }
        }

        public void WriteLine(string s)
        {
            if (s is null)
            {
                return;
            }

            using (CaretUpdateScope())
            {
                Reporter.Output.WriteLine(s);
            }
        }

        public IDisposable AddBreakHandler(Action handler)
        {
            Disposable result = new Disposable(() => ReleaseBreakHandler(handler));
            _breakHandlers.Add(handler);
            return result;
        }

        private IDisposable CaretUpdateScope()
        {
            Point currentCaret = Caret;
            return new Disposable(() =>
            {
                int y = Caret.Y - currentCaret.Y;
                int x = Caret.X - currentCaret.X;
                CaretPosition += y * Console.BufferWidth + x;
            });
        }

        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Action handler = _breakHandlers.LastOrDefault();
            handler?.Invoke();
        }

        private void ReleaseBreakHandler(Action handler)
        {
            _breakHandlers.Remove(handler);
        }

        public IWritable Error { get; }
    }
}
