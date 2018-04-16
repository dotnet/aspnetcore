using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public static class ClientProxyExtensions
    {
        /// <summary>
        /// Invokes a method on the connection(s) represented by the <see cref="IClientProxy"/> instance.
        /// Does not wait for a response from the receiver.
        /// </summary>
        /// <param name="clientProxy">The <see cref="IClientProxy"/></param>
        /// <param name="method">name of the method to invoke</param>
        /// <returns>A task that represents when the data has been sent to the client.</returns>
        public static Task SendAsync(this IClientProxy clientProxy, string method)
        {
            return clientProxy.SendCoreAsync(method, Array.Empty<object>());
        }

        /// <summary>
        /// Invokes a method on the connection(s) represented by the <see cref="IClientProxy"/> instance.
        /// Does not wait for a response from the receiver.
        /// </summary>
        /// <param name="clientProxy">The <see cref="IClientProxy"/></param>
        /// <param name="method">name of the method to invoke</param>
        /// <param name="arg1">The first argument</param>
        /// <returns>A task that represents when the data has been sent to the client.</returns>
        public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1)
        {
            return clientProxy.SendCoreAsync(method, new[] { arg1 });
        }

        /// <summary>
        /// Invokes a method on the connection(s) represented by the <see cref="IClientProxy"/> instance.
        /// Does not wait for a response from the receiver.
        /// </summary>
        /// <param name="clientProxy">The <see cref="IClientProxy"/></param>
        /// <param name="method">name of the method to invoke</param>
        /// <param name="arg1">The first argument</param>
        /// <param name="arg2">The second argument</param>
        /// <returns>A task that represents when the data has been sent to the client.</returns>
        public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2)
        {
            return clientProxy.SendCoreAsync(method, new[] { arg1, arg2 });
        }

        /// <summary>
        /// Invokes a method on the connection(s) represented by the <see cref="IClientProxy"/> instance.
        /// Does not wait for a response from the receiver.
        /// </summary>
        /// <param name="clientProxy">The <see cref="IClientProxy"/></param>
        /// <param name="method">name of the method to invoke</param>
        /// <param name="arg1">The first argument</param>
        /// <param name="arg2">The second argument</param>
        /// <param name="arg3">The third argument</param>
        /// <returns>A task that represents when the data has been sent to the client.</returns>
        public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2, object arg3)
        {
            return clientProxy.SendCoreAsync(method, new[] { arg1, arg2, arg3 });
        }

        /// <summary>
        /// Invokes a method on the connection(s) represented by the <see cref="IClientProxy"/> instance.
        /// Does not wait for a response from the receiver.
        /// </summary>
        /// <param name="clientProxy">The <see cref="IClientProxy"/></param>
        /// <param name="method">name of the method to invoke</param>
        /// <param name="arg1">The first argument</param>
        /// <param name="arg2">The second argument</param>
        /// <param name="arg3">The third argument</param>
        /// <param name="arg4">The fourth argument</param>
        /// <returns>A task that represents when the data has been sent to the client.</returns>
        public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4)
        {
            return clientProxy.SendCoreAsync(method, new[] { arg1, arg2, arg3, arg4 });
        }

        /// <summary>
        /// Invokes a method on the connection(s) represented by the <see cref="IClientProxy"/> instance.
        /// Does not wait for a response from the receiver.
        /// </summary>
        /// <param name="clientProxy">The <see cref="IClientProxy"/></param>
        /// <param name="method">name of the method to invoke</param>
        /// <param name="arg1">The first argument</param>
        /// <param name="arg2">The second argument</param>
        /// <param name="arg3">The third argument</param>
        /// <param name="arg4">The fourth argument</param>
        /// <param name="arg5">The fifth argument</param>
        /// <returns>A task that represents when the data has been sent to the client.</returns>
        public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            return clientProxy.SendCoreAsync(method, new[] { arg1, arg2, arg3, arg4, arg5 });
        }

        /// <summary>
        /// Invokes a method on the connection(s) represented by the <see cref="IClientProxy"/> instance.
        /// Does not wait for a response from the receiver.
        /// </summary>
        /// <param name="clientProxy">The <see cref="IClientProxy"/></param>
        /// <param name="method">name of the method to invoke</param>
        /// <param name="arg1">The first argument</param>
        /// <param name="arg2">The second argument</param>
        /// <param name="arg3">The third argument</param>
        /// <param name="arg4">The fourth argument</param>
        /// <param name="arg5">The fifth argument</param>
        /// <param name="arg6">The sixth argument</param>
        /// <returns>A task that represents when the data has been sent to the client.</returns>
        public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
        {
            return clientProxy.SendCoreAsync(method, new[] { arg1, arg2, arg3, arg4, arg5, arg6 });
        }

        /// <summary>
        /// Invokes a method on the connection(s) represented by the <see cref="IClientProxy"/> instance.
        /// Does not wait for a response from the receiver.
        /// </summary>
        /// <param name="clientProxy">The <see cref="IClientProxy"/></param>
        /// <param name="method">name of the method to invoke</param>
        /// <param name="arg1">The first argument</param>
        /// <param name="arg2">The second argument</param>
        /// <param name="arg3">The third argument</param>
        /// <param name="arg4">The fourth argument</param>
        /// <param name="arg5">The fifth argument</param>
        /// <param name="arg6">The sixth argument</param>
        /// <param name="arg7">The seventh argument</param>
        /// <returns>A task that represents when the data has been sent to the client.</returns>
        public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
        {
            return clientProxy.SendCoreAsync(method, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 });
        }

        /// <summary>
        /// Invokes a method on the connection(s) represented by the <see cref="IClientProxy"/> instance.
        /// Does not wait for a response from the receiver.
        /// </summary>
        /// <param name="clientProxy">The <see cref="IClientProxy"/></param>
        /// <param name="method">name of the method to invoke</param>
        /// <param name="arg1">The first argument</param>
        /// <param name="arg2">The second argument</param>
        /// <param name="arg3">The third argument</param>
        /// <param name="arg4">The fourth argument</param>
        /// <param name="arg5">The fifth argument</param>
        /// <param name="arg6">The sixth argument</param>
        /// <param name="arg7">The seventh argument</param>
        /// <param name="arg8">The eigth argument</param>
        /// <returns>A task that represents when the data has been sent to the client.</returns>
        public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
        {
            return clientProxy.SendCoreAsync(method, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 });
        }

        /// <summary>
        /// Invokes a method on the connection(s) represented by the <see cref="IClientProxy"/> instance.
        /// Does not wait for a response from the receiver.
        /// </summary>
        /// <param name="clientProxy">The <see cref="IClientProxy"/></param>
        /// <param name="method">name of the method to invoke</param>
        /// <param name="arg1">The first argument</param>
        /// <param name="arg2">The second argument</param>
        /// <param name="arg3">The third argument</param>
        /// <param name="arg4">The fourth argument</param>
        /// <param name="arg5">The fifth argument</param>
        /// <param name="arg6">The sixth argument</param>
        /// <param name="arg7">The seventh argument</param>
        /// <param name="arg8">The eigth argument</param>
        /// <param name="arg9">The ninth argument</param>
        /// <returns>A task that represents when the data has been sent to the client.</returns>
        public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
        {
            return clientProxy.SendCoreAsync(method, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 });
        }

        /// <summary>
        /// Invokes a method on the connection(s) represented by the <see cref="IClientProxy"/> instance.
        /// Does not wait for a response from the receiver.
        /// </summary>
        /// <param name="clientProxy">The <see cref="IClientProxy"/></param>
        /// <param name="method">name of the method to invoke</param>
        /// <param name="arg1">The first argument</param>
        /// <param name="arg2">The second argument</param>
        /// <param name="arg3">The third argument</param>
        /// <param name="arg4">The fourth argument</param>
        /// <param name="arg5">The fifth argument</param>
        /// <param name="arg6">The sixth argument</param>
        /// <param name="arg7">The seventh argument</param>
        /// <param name="arg8">The eigth argument</param>
        /// <param name="arg9">The ninth argument</param>
        /// <param name="arg10">The tenth argument</param>
        /// <returns>A task that represents when the data has been sent to the client.</returns>
        public static Task SendAsync(this IClientProxy clientProxy, string method, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9, object arg10)
        {
            return clientProxy.SendCoreAsync(method, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 });
        }
    }
}
