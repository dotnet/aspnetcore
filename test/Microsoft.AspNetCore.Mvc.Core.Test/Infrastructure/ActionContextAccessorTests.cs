// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
#if NET46
    public class ActionContextAccessorTests
    {
        private static void DomainFunc()
        {
            var accessor = new ActionContextAccessor();
            Assert.Equal(null, accessor.ActionContext);
            accessor.ActionContext = new ActionContext();
        }

        [Fact]
        public void ChangingAppDomainsDoesNotBreak_ActionContextAccessor()
        {
            // Arrange
            var accessor = new ActionContextAccessor();
            var context = new ActionContext();
            var setupInfo = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory
            };

            var domain = AppDomain.CreateDomain("newDomain", securityInfo: null, info: setupInfo);

            // Act
            domain.DoCallBack(DomainFunc);
            AppDomain.Unload(domain);
            accessor.ActionContext = context;

            // Assert
            Assert.True(ReferenceEquals(context, accessor.ActionContext));
        }
    }
#elif NETCOREAPP2_0
#else
#error target frameworks needs to be updated.    
#endif
}
