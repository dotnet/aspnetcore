using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Http.Core.Collections;
using Xunit;

namespace MusicStore.Models
{
    public class ShoppingCartTest
    {
        [Fact]
        public void GetCartId_ReturnsCartIdFromCookies()
        {
            // Arrange
            var cartId = "cartId_A";

            var httpContext = new DefaultHttpContext();
            httpContext.SetFeature<IRequestCookiesFeature>(new CookiesFeature("Session=" + cartId));

            var cart = new ShoppingCart(new MusicStoreContext());

            // Act
            var result = cart.GetCartId(httpContext);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cartId, result);
        }

        private class CookiesFeature : IRequestCookiesFeature
        {
            private readonly RequestCookiesCollection _cookies;

            public CookiesFeature(string cookiesHeader)
            {
                _cookies = new RequestCookiesCollection();
                _cookies.Reparse(cookiesHeader);
            }

            public IReadableStringCollection Cookies
            {
                get { return _cookies; }
            }
        }
    }
}
