using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Core.Builder
{
    public class MvcApplicationBuilderExtensionsTest
    {
        [Fact]
        public void UseMvc_ThrowsInvalidOperationException_IfMvcMarkerServiceIsNotRegistered()
        {
            // Arrange
            var applicationBuilderMock = new Mock<IApplicationBuilder>();
            applicationBuilderMock
                .Setup(s => s.ApplicationServices)
                .Returns(Mock.Of<IServiceProvider>());

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => applicationBuilderMock.Object.UseMvc(rb => { }));

            Assert.Equal(
                "Unable to find the required services. Please add all the required services by calling " +
                "'IServiceCollection.AddMvc' inside the call to 'ConfigureServices(...)' " +
                "in the application startup code.",
                exception.Message);
        }
    }
}
