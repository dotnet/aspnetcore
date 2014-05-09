// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor
{
    public abstract class StateMachine<TReturn>
    {
        protected delegate StateResult State();

        protected abstract State StartState { get; }

        protected State CurrentState { get; set; }

        protected virtual TReturn Turn()
        {
            if (CurrentState != null)
            {
                StateResult result;
                do
                {
                    // Keep running until we get a null result or output
                    result = CurrentState();
                    CurrentState = result.Next;
                }
                while (result != null && !result.HasOutput);

                if (result == null)
                {
                    return default(TReturn); // Terminated
                }
                return result.Output;
            }
            return default(TReturn);
        }

        /// <summary>
        /// Returns a result indicating that the machine should stop executing and return null output.
        /// </summary>
        protected StateResult Stop()
        {
            return null;
        }

        /// <summary>
        /// Returns a result indicating that this state has no output and the machine should immediately invoke the specified state
        /// </summary>
        /// <remarks>
        /// By returning no output, the state machine will invoke the next state immediately, before returning
        /// controller to the caller of <see cref="Turn"/>
        /// </remarks>
        protected StateResult Transition(State newState)
        {
            return new StateResult(newState);
        }

        /// <summary>
        /// Returns a result containing the specified output and indicating that the next call to
        /// <see cref="Turn"/> should invoke the provided state.
        /// </summary>
        protected StateResult Transition(TReturn output, State newState)
        {
            return new StateResult(output, newState);
        }

        /// <summary>
        /// Returns a result indicating that this state has no output and the machine should remain in this state
        /// </summary>
        /// <remarks>
        /// By returning no output, the state machine will re-invoke the current state again before returning
        /// controller to the caller of <see cref="Turn"/>
        /// </remarks>
        protected StateResult Stay()
        {
            return new StateResult(CurrentState);
        }

        /// <summary>
        /// Returns a result containing the specified output and indicating that the next call to
        /// <see cref="Turn"/> should re-invoke the current state.
        /// </summary>
        protected StateResult Stay(TReturn output)
        {
            return new StateResult(output, CurrentState);
        }

        protected class StateResult
        {
            public StateResult(State next)
            {
                HasOutput = false;
                Next = next;
            }

            public StateResult(TReturn output, State next)
            {
                HasOutput = true;
                Output = output;
                Next = next;
            }

            public bool HasOutput { get; set; }
            public TReturn Output { get; set; }
            public State Next { get; set; }
        }
    }
}
