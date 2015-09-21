// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Controllers
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
            return new TaskDerivedType();
        }

        public TaskOfTDerivedType<int> TaskActionWithCustomTaskOfTReturnType(int i, string s)
        {
            return new TaskOfTDerivedType<int>(1);
        }

        /// <summary>
        /// Returns a <see cref="Task{TResult}"/> instead of a <see cref="Task"/>. This should throw an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
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

        public string EchoWithDefaultValue([DefaultValue("hello")] string input)
        {
            return input;
        }

        public string EchoWithDefaultValueAndAttribute([DefaultValue("hello")] string input = "world")
        {
            return input;
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