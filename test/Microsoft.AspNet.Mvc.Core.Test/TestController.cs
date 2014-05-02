// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class TestController
    {
        public static void VoidAction()
        {
        }

        #pragma warning disable 1998
        public async Task TaskAction(int i, string s)
        {
            return;
        }
        #pragma warning restore 1998

        #pragma warning disable 1998
        public async Task<int> TaskValueTypeAction(int i, string s)
        {
            Console.WriteLine(s);
            return i;
        }
        #pragma warning restore 1998

        #pragma warning disable 1998
        public async Task<Task<int>> TaskOfTaskAction(int i, string s)
        {
            return TaskValueTypeAction(i, s);
        }
        #pragma warning restore 1998

        public Task<int> TaskValueTypeActionWithoutAsync(int i, string s)
        {
            return TaskValueTypeAction(i, s);
        }

        #pragma warning disable 1998
        public async Task<int> TaskActionWithException(int i, string s)
        {
            throw new NotImplementedException("Not Implemented Exception");
        }
        #pragma warning restore 1998

        public Task<int> TaskActionWithExceptionWithoutAsync(int i, string s)
        {
            throw new NotImplementedException("Not Implemented Exception");
        }

        public async Task<int> TaskActionThrowAfterAwait(int i, string s)
        {
            await Task.Delay(500);
            throw new ArgumentException("Argument Exception");
        }

        public TaskDerivedType TaskActionWithCustomTaskReturnType(int i, string s)
        {
            Console.WriteLine(s);
            return new TaskDerivedType();
        }

        public TaskOfTDerivedType<int> TaskActionWithCustomTaskOfTReturnType(int i, string s)
        {
            Console.WriteLine(s);
            return new TaskOfTDerivedType<int>(1);
        }

        /// <summary>
        /// Returns a Task<Task> instead of a Task. This should throw an InvalidOperationException.
        /// </summary>
        /// <returns></returns>
        public Task UnwrappedTask(int i, string s)
        {
            return Task.Factory.StartNew(async () => await Task.Delay(50));
        }

        public string Echo(string input)
        {
            return input;
        }

        public string EchoWithException(string input)
        {
            throw new NotImplementedException();
        }

        public dynamic ReturnTaskAsDynamicValue(int i, string s)
        {
            return Task.Factory.StartNew(() => i);
        }

        public class TaskDerivedType : Task
        {
            public TaskDerivedType()
                : base(() => Console.WriteLine("In The Constructor"))
            {
            }
        }

        public class TaskOfTDerivedType<T> : Task<T>
        {
            public TaskOfTDerivedType(T input)
                : base(() => input)
            {
            }
        }
    }
}