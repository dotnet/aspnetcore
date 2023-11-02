// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class HostNameIsReachableAttribute : Attribute, ITestCondition
{
    private string _hostname;
    private string _error;
    private bool? _isMet;

    public bool IsMet
    {
        get
        {
            return _isMet ?? (_isMet = HostNameIsReachable().GetAwaiter().GetResult()).Value;
        }
    }

    public string SkipReason => _hostname != null
        ? $"Test cannot run when network is unreachable. Socket exception: '{_error}'"
        : "Could not determine hostname for current test machine";

    private async Task<bool> HostNameIsReachable()
    {
        try
        {
            _hostname = Dns.GetHostName();

            // if the network is unreachable on macOS, throws with SocketError.NetworkUnreachable
            // if the network device is not configured, throws with SocketError.HostNotFound
            // if the network is reachable, throws with SocketError.ConnectionRefused or succeeds
            var timeoutTask = Task.Delay(1000);
            if (await Task.WhenAny(ConnectToHost(_hostname, 80), timeoutTask) == timeoutTask)
            {
                _error = "Attempt to establish a connection took over a second without success or failure.";
                return false;
            }
        }
        catch (SocketException ex) when (
            ex.SocketErrorCode == SocketError.NetworkUnreachable
            || ex.SocketErrorCode == SocketError.HostNotFound)
        {
            _error = ex.Message;
            return false;
        }
        catch
        {
            // Swallow other errors. Allows the test to throw the failures instead
        }

        return true;
    }

    public static async Task<Socket> ConnectToHost(string hostName, int port)
    {
        var tcs = new TaskCompletionSource<Socket>(TaskCreationOptions.RunContinuationsAsynchronously);

        var socketArgs = new SocketAsyncEventArgs();
        socketArgs.RemoteEndPoint = new DnsEndPoint(hostName, port);
        socketArgs.Completed += (s, e) => tcs.TrySetResult(e.ConnectSocket);

        // Must use static ConnectAsync(), since instance Connect() does not support DNS names on OSX/Linux.
        if (Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, socketArgs))
        {
            await tcs.Task.ConfigureAwait(false);
        }

        var socket = socketArgs.ConnectSocket;

        if (socket == null)
        {
            throw new SocketException((int)socketArgs.SocketError);
        }
        else
        {
            return socket;
        }
    }
}
