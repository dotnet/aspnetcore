// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//using System;
//using System.Reflection;
//using Xunit.Abstractions;
//using Xunit.Sdk;

//namespace Microsoft.Extensions.Caching.StackExchangeRedis
//{
    // TODO - should replace this whole approach with a CollectionFixture when
    // Xunit CollectionFixtures are working correctly.
    //public class RedisXunitTestExecutor : XunitTestFrameworkExecutor, IDisposable
    //{
    //    private bool _isDisposed;

    //    public RedisXunitTestExecutor(
    //        AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider)
    //        : base(assemblyName, sourceInformationProvider)
    //    {
    //        try
    //        {
    //            RedisTestConfig.GetOrStartServer();
    //        }
    //        catch (Exception)
    //        {
    //            // do not let exceptions starting server prevent XunitTestFrameworkExecutor from being created
    //        }
    //    }

    //    ~RedisXunitTestExecutor()
    //    {
    //        Dispose(false);
    //    }

    //    void IDisposable.Dispose()
    //    {
    //        Dispose(true);
    //        GC.SuppressFinalize(this);
    //    }

    //    protected virtual void Dispose(bool disposing)
    //    {
    //        if (!_isDisposed)
    //        {
    //            try
    //            {
    //                RedisTestConfig.StopRedisServer();
    //            }
    //            catch (Exception)
    //            {
    //                // do not let exceptions stopping server prevent XunitTestFrameworkExecutor from being disposed
    //            }

    //            _isDisposed = true;
    //        }
    //    }
    //}
//}
