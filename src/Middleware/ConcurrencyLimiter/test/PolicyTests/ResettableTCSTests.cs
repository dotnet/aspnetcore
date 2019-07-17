using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.ConcurrencyLimiter.Tests.PolicyTests
{
    public static class ResettableTCSTests
    {
        [Fact]
        public async static void CanBeReset()
        {
            var tcs = new ResettableBooleanTCS();

            var flag1 = false;
            var task1 = tcs.GetAwaiter();

            _ = Task.Run(async () =>
            {
                flag1 = await task1;
            });

            await Task.Delay(50);
            Assert.False(flag1);

            tcs.CompleteTrue();

            await Task.Delay(50);
            Assert.True(flag1);

            // ====== second run ======

            var flag2 = false;
            var task2 = tcs.GetAwaiter();

            _ = Task.Run(async () =>
            {
                flag2 = await task2;
            });

            await Task.Delay(50);
            Assert.False(flag2);

            tcs.CompleteTrue();

            await Task.Delay(50);
            Assert.True(flag2);

        }
    }
}
