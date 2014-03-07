// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class TestController
    {
        public static void VoidAction()
        {
        }

        public async Task TaskAction(int i, string s)
        {
            return;
        }

        public async Task<int> TaskValueTypeAction(int i, string s)
        {
            Console.WriteLine(s);
            return i;
        }

        public async Task<Task<int>> TaskOfTaskAction(int i, string s)
        {
            return TaskValueTypeAction(i, s);
        }

        public Task<int> TaskValueTypeActionWithoutAsync(int i, string s)
        {
            return TaskValueTypeAction(i, s);
        }

        public async Task<int> TaskActionWithException(int i, string s)
        {
            throw new NotImplementedException("Not Implemented Exception");
        }

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