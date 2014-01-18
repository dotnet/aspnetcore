// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.TestCommon
{
    /// <summary>
    /// MSTest assert class to make assertions about tests using <see cref="Task"/>.
    /// </summary>
    public class TaskAssert
    {
        private static int timeOutMs = System.Diagnostics.Debugger.IsAttached ? TimeoutConstant.DefaultTimeout : TimeoutConstant.DefaultTimeout * 10;
        private static TaskAssert singleton = new TaskAssert();

        public static TaskAssert Singleton { get { return singleton; } }

        /// <summary>
        /// Asserts the given task has been started.  TAP guidelines are that all
        /// <see cref="Task"/> objects returned from public API's have been started.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to test.</param>
        public void IsStarted(Task task)
        {
            Assert.NotNull(task);
            Assert.True(task.Status != TaskStatus.Created);
        }

        /// <summary>
        /// Asserts the given task completes successfully.  This method will block the
        /// current thread waiting for the task, but will timeout if it does not complete.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to test.</param>
        public void Succeeds(Task task)
        {
            IsStarted(task);
            task.Wait(timeOutMs);
            AggregateException aggregateException = task.Exception;
            Exception innerException = aggregateException == null ? null : aggregateException.InnerException;
            Assert.Null(innerException);
        }

        /// <summary>
        /// Asserts the given task completes successfully and returns a result.
        /// Use this overload for a generic <see cref="Task"/> whose generic parameter is not known at compile time.
        /// This method will block the current thread waiting for the task, but will timeout if it does not complete.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to test.</param>
        /// <returns>The result from that task.</returns>
        public object SucceedsWithResult(Task task)
        {
            Succeeds(task);
            Assert.True(task.GetType().IsGenericType);
            Type[] genericArguments = task.GetType().GetGenericArguments();
            Assert.Equal(1, genericArguments.Length);
            PropertyInfo resultProperty = task.GetType().GetProperty("Result");
            Assert.NotNull(resultProperty);
            return resultProperty.GetValue(task, null);
        }

        /// <summary>
        /// Asserts the given task completes successfully and returns a <typeparamref name="T"/> result.
        /// This method will block the current thread waiting for the task, but will timeout if it does not complete.
        /// </summary>
        /// <typeparam name="T">The result of the <see cref="Task"/>.</typeparam>
        /// <param name="task">The <see cref="Task"/> to test.</param>
        /// <returns>The result from that task.</returns>
        public T SucceedsWithResult<T>(Task<T> task)
        {
            Succeeds(task);
            return task.Result;
        }

        /// <summary>
        /// Asserts the given <see cref="Task"/> completes successfully and yields
        /// the expected result.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to test.</param>
        /// <param name="expectedObj">The expected result.</param>
        public void ResultEquals(Task task, object expectedObj)
        {
            object actualObj = SucceedsWithResult(task);
            Assert.Equal(expectedObj, actualObj);
        }

        /// <summary>
        /// Asserts the given <see cref="Task"/> completes successfully and yields
        /// the expected result.
        /// </summary>
        /// <typeparam name="T">The type the task will return.</typeparam>
        /// <param name="task">The task to test.</param>
        /// <param name="expectedObj">The expected result.</param>
        public void ResultEquals<T>(Task<T> task, T expectedObj)
        {
            T actualObj = SucceedsWithResult<T>(task);
            Assert.Equal(expectedObj, actualObj);
        }
    }
}
