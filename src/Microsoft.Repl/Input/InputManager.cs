// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Repl.Input
{
    public class InputManager : IInputManager
    {
        private readonly Dictionary<ConsoleKey, Dictionary<ConsoleModifiers, AsyncKeyPressHandler>> _handlers = new Dictionary<ConsoleKey, Dictionary<ConsoleModifiers, AsyncKeyPressHandler>>();
        private readonly List<char> _inputBuffer = new List<char>();

        public bool IsOverwriteMode { get; set; }

        public void Clear(IShellState state)
        {
            SetInput(state, string.Empty);
        }

        public string GetCurrentBuffer()
        {
            return _inputBuffer.Stringify();
        }

        public IInputManager RegisterKeyHandler(ConsoleKey key, AsyncKeyPressHandler handler)
        {
            if (!_handlers.TryGetValue(key, out Dictionary<ConsoleModifiers, AsyncKeyPressHandler> handlers))
            {
                _handlers[key] = handlers = new Dictionary<ConsoleModifiers, AsyncKeyPressHandler>();
            }

            if (handler == null)
            {
                handlers.Remove(default(ConsoleModifiers));
            }
            else
            {
                handlers[default(ConsoleModifiers)] = handler;
            }

            return this;
        }

        public IInputManager RegisterKeyHandler(ConsoleKey key, ConsoleModifiers modifiers, AsyncKeyPressHandler handler)
        {
            if (!_handlers.TryGetValue(key, out Dictionary<ConsoleModifiers, AsyncKeyPressHandler> handlers))
            {
                _handlers[key] = handlers = new Dictionary<ConsoleModifiers, AsyncKeyPressHandler>();
            }

            if (handler == null)
            {
                handlers.Remove(modifiers);
            }
            else
            {
                handlers[modifiers] = handler;
            }

            return this;
        }

        public void RemoveCurrentCharacter(IShellState state)
        {
            int caret = state.ConsoleManager.CaretPosition;

            if (caret == _inputBuffer.Count)
            {
                return;
            }

            List<char> update = _inputBuffer.ToList();
            update.RemoveAt(caret);
            state.ConsoleManager.IsCaretVisible = false;
            SetInput(state, update);
            state.ConsoleManager.MoveCaret(caret - state.ConsoleManager.CaretPosition);
            state.ConsoleManager.IsCaretVisible = true;
        }

        public void RemovePreviousCharacter(IShellState state)
        {
            int caret = state.ConsoleManager.CaretPosition;
            if (caret == 0)
            {
                return;
            }

            List<char> update = _inputBuffer.ToList();
            update.RemoveAt(caret - 1);
            state.ConsoleManager.IsCaretVisible = false;
            SetInput(state, update, false);
            state.ConsoleManager.MoveCaret(caret - state.ConsoleManager.CaretPosition - 1);
            state.ConsoleManager.IsCaretVisible = true;
        }

        public void SetInput(IShellState state, string input)
        {
            SetInput(state, input.ToCharArray());
        }

        public void ResetInput()
        {
            _inputBuffer.Clear();
        }

        private string _ttyState;

        private void StashEchoState()
        {
            string sttyFlags = null;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                _ttyState = GetTtyState();
                sttyFlags = "gfmt1:erase=08:werase=08 -echo";
            }
            //If it's any of the ubuntu variants on 18.x, stty tweaks are required
            else if (System.Runtime.InteropServices.RuntimeInformation.OSDescription.IndexOf("buntu", StringComparison.OrdinalIgnoreCase) > -1)
            {
                _ttyState = GetTtyState();
                sttyFlags = "erase 0x08 werase 0x08 -echo";
            }

            if (!string.IsNullOrEmpty(sttyFlags))
            {
                ProcessStartInfo psi = new ProcessStartInfo("stty", sttyFlags);
                Process p = Process.Start(psi);
                p?.WaitForExit();
            }
        }

        private static string GetTtyState()
        {
            ProcessStartInfo psi = new ProcessStartInfo("stty", "-g")
            {
                RedirectStandardOutput = true
            };
            Process p = Process.Start(psi);
            p?.WaitForExit();
            string result = p?.StandardOutput.ReadToEnd().Trim();
            return result;
        }

        private void RestoreTtyState()
        {
            if (!string.IsNullOrEmpty(_ttyState))
            {
                ProcessStartInfo psi = new ProcessStartInfo("stty", _ttyState);
                Process p = Process.Start(psi);
                p?.WaitForExit();
            }
        }

        private void SetInput(IShellState state, IReadOnlyList<char> input, bool moveCaret = true)
        {
            bool oldCaretVisibility = state.ConsoleManager.IsCaretVisible;
            state.ConsoleManager.IsCaretVisible = false;
            int lastCommonPosition = 0;

            for (; lastCommonPosition < input.Count && lastCommonPosition < _inputBuffer.Count && _inputBuffer[lastCommonPosition] == input[lastCommonPosition]; ++lastCommonPosition)
            {
            }

            state.ConsoleManager.MoveCaret(-state.ConsoleManager.CaretPosition + lastCommonPosition);
            string str = new string(input.Skip(lastCommonPosition).ToArray());
            int trailing = _inputBuffer.Count - input.Count;

            if (trailing > 0)
            {
                str = str.PadRight(trailing + str.Length);
            }

            state.ConsoleManager.Write(str);

            if (trailing > 0 && moveCaret)
            {
                state.ConsoleManager.MoveCaret(-trailing);
            }

            _inputBuffer.Clear();
            _inputBuffer.AddRange(input);

            if (oldCaretVisibility)
            {
                state.ConsoleManager.IsCaretVisible = true;
            }
        }

        public async Task StartAsync(IShellState state, CancellationToken cancellationToken)
        {
            StashEchoState();

            try
            {
                List<ConsoleKeyInfo> presses = null;
                while (!state.IsExiting && !cancellationToken.IsCancellationRequested)
                {
                    ConsoleKeyInfo keyPress = state.ConsoleManager.ReadKey(cancellationToken);

                    if (_handlers.TryGetValue(keyPress.Key, out Dictionary<ConsoleModifiers, AsyncKeyPressHandler> handlerLookup) && handlerLookup.TryGetValue(keyPress.Modifiers, out AsyncKeyPressHandler handler))
                    {
                        using (CancellationTokenSource source = new CancellationTokenSource())
                        using (state.ConsoleManager.AddBreakHandler(() => source.Cancel()))
                        {
                            if (presses != null)
                            {
                                FlushInput(state, ref presses);
                            }

                            await handler(keyPress, state, source.Token).ConfigureAwait(false);
                        }
                    }
                    else if (!string.IsNullOrEmpty(_ttyState) && keyPress.Modifiers == ConsoleModifiers.Control)
                    {
                        if (presses != null)
                        {
                            FlushInput(state, ref presses);
                        }

                        //TODO: Verify on a mac whether these are still needed
                        if (keyPress.Key == ConsoleKey.A)
                        {
                            state.ConsoleManager.MoveCaret(-state.ConsoleManager.CaretPosition);
                        }
                        else if (keyPress.Key == ConsoleKey.E)
                        {
                            state.ConsoleManager.MoveCaret(_inputBuffer.Count - state.ConsoleManager.CaretPosition);
                        }
                    }
                    //TODO: Register these like regular commands
                    else if (!string.IsNullOrEmpty(_ttyState) && keyPress.Modifiers == ConsoleModifiers.Alt)
                    {
                        if (presses != null)
                        {
                            FlushInput(state, ref presses);
                        }

                        //Move back a word
                        if (keyPress.Key == ConsoleKey.B)
                        {
                            int i = state.ConsoleManager.CaretPosition - 1;

                            if (i < 0)
                            {
                                continue;
                            }

                            bool letterMode = char.IsLetterOrDigit(_inputBuffer[i]);

                            for (; i > 0 && (char.IsLetterOrDigit(_inputBuffer[i]) == letterMode); --i)
                            {
                            }

                            if (letterMode && i > 0)
                            {
                                ++i;
                            }

                            if (i > -1)
                            {
                                state.ConsoleManager.MoveCaret(i - state.ConsoleManager.CaretPosition);
                            }
                        }
                        //Move forward a word
                        else if (keyPress.Key == ConsoleKey.F)
                        {
                            int i = state.ConsoleManager.CaretPosition + 1;

                            if (i >= _inputBuffer.Count)
                            {
                                continue;
                            }

                            bool letterMode = char.IsLetterOrDigit(_inputBuffer[i]);

                            for (; i < _inputBuffer.Count && (char.IsLetterOrDigit(_inputBuffer[i]) == letterMode); ++i)
                            {
                            }

                            if (letterMode && i < _inputBuffer.Count - 1 && i > 0)
                            {
                                --i;
                            }

                            state.ConsoleManager.MoveCaret(i - state.ConsoleManager.CaretPosition);
                        }
                    }
                    else if (!keyPress.Modifiers.HasFlag(ConsoleModifiers.Alt) && !keyPress.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        if (state.ConsoleManager.IsKeyAvailable)
                        {
                            if (presses == null)
                            {
                                presses = new List<ConsoleKeyInfo>();
                            }

                            presses.Add(keyPress);
                            continue;
                        }

                        if (presses != null)
                        {
                            presses.Add(keyPress);
                            FlushInput(state, ref presses);
                            continue;
                        }

                        if (state.ConsoleManager.CaretPosition == _inputBuffer.Count)
                        {
                            _inputBuffer.Add(keyPress.KeyChar);
                            state.ConsoleManager.Write(keyPress.KeyChar);
                        }
                        else if (IsOverwriteMode)
                        {
                            _inputBuffer[state.ConsoleManager.CaretPosition] = keyPress.KeyChar;
                            state.ConsoleManager.Write(keyPress.KeyChar);
                        }
                        else
                        {
                            state.ConsoleManager.IsCaretVisible = false;
                            _inputBuffer.Insert(state.ConsoleManager.CaretPosition, keyPress.KeyChar);
                            int currentCaretPosition = state.ConsoleManager.CaretPosition;
                            string s = new string(_inputBuffer.ToArray(), state.ConsoleManager.CaretPosition, _inputBuffer.Count - state.ConsoleManager.CaretPosition);
                            state.ConsoleManager.Write(s);
                            state.ConsoleManager.MoveCaret(currentCaretPosition - state.ConsoleManager.CaretPosition + 1);
                            state.ConsoleManager.IsCaretVisible = true;
                        }
                    }
                }
            }
            finally
            {
                RestoreTtyState();
            }
        }

        private void FlushInput(IShellState state, ref List<ConsoleKeyInfo> presses)
        {
            string str = new string(presses.Select(x => x.KeyChar).ToArray());

            if (state.ConsoleManager.CaretPosition == _inputBuffer.Count)
            {
                _inputBuffer.AddRange(str);
                state.ConsoleManager.Write(str);
            }
            else if (IsOverwriteMode)
            {
                for (int i = 0; i < str.Length; ++i)
                {
                    if (state.ConsoleManager.CaretPosition + i < _inputBuffer.Count)
                    {
                        _inputBuffer[state.ConsoleManager.CaretPosition + i] = str[i];
                    }
                    else
                    {
                        _inputBuffer.AddRange(str.Skip(i));
                        break;
                    }
                }

                state.ConsoleManager.Write(str);
            }
            else
            {
                state.ConsoleManager.IsCaretVisible = false;
                _inputBuffer.InsertRange(state.ConsoleManager.CaretPosition, str);
                int currentCaretPosition = state.ConsoleManager.CaretPosition;
                string s = new string(_inputBuffer.ToArray(), state.ConsoleManager.CaretPosition, _inputBuffer.Count - state.ConsoleManager.CaretPosition);
                state.ConsoleManager.Write(s);
                state.ConsoleManager.MoveCaret(currentCaretPosition - state.ConsoleManager.CaretPosition + str.Length);
                state.ConsoleManager.IsCaretVisible = true;
            }

            presses = null;
        }
    }
}
