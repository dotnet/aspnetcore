using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Session;
using Microsoft.AspNet.Testing.Logging;
using Microsoft.Framework.Caching.Distributed;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.DependencyInjection;
using MusicStore.Models;
using MusicStore.ViewModels;
using Xunit;

namespace MusicStore.Controllers
{
    public class ShoppingCartControllerTest
    {
        private readonly IServiceProvider _serviceProvider;

        public ShoppingCartControllerTest()
        {
            var services = new ServiceCollection();

            services.AddEntityFramework()
                      .AddInMemoryStore()
                      .AddDbContext<MusicStoreContext>();

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task Index_ReturnsNoCartItems_WhenSessionEmpty()
        {
            // Arrange
            var sessionFeature = new SessionFeature()
            {
                Session = CreateTestSession(),
            };

            var httpContext = new DefaultHttpContext();
            httpContext.SetFeature<ISessionFeature>(sessionFeature);

            var controller = new ShoppingCartController()
            {
                DbContext = _serviceProvider.GetRequiredService<MusicStoreContext>(),
            };
            controller.ActionContext.HttpContext = httpContext;

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData);
            Assert.Null(viewResult.ViewName);

            var model = Assert.IsType<ShoppingCartViewModel>(viewResult.ViewData.Model);
            Assert.Equal(0, model.CartItems.Count);
            Assert.Equal(0, model.CartTotal);
        }

        [Fact]
        public async Task Index_ReturnsNoCartItems_WhenNoItemsInCart()
        {
            // Arrange
            var sessionFeature = new SessionFeature()
            {
                Session = CreateTestSession(),
            };

            var httpContext = new DefaultHttpContext();
            httpContext.SetFeature<ISessionFeature>(sessionFeature);
            httpContext.Session.SetString("Session", "CartId_A");

            var controller = new ShoppingCartController()
            {
                DbContext = _serviceProvider.GetRequiredService<MusicStoreContext>(),
            };
            controller.ActionContext.HttpContext = httpContext;

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData);
            Assert.Null(viewResult.ViewName);

            var model = Assert.IsType<ShoppingCartViewModel>(viewResult.ViewData.Model);
            Assert.Equal(0, model.CartItems.Count);
            Assert.Equal(0, model.CartTotal);
        }

        [Fact]
        public async Task Index_ReturnsCartItems_WhenItemsInCart()
        {
            // Arrange
            var cartId = "CartId_A";
            var sessionFeature = new SessionFeature()
            {
                Session = CreateTestSession(),
            };

            var httpContext = new DefaultHttpContext();
            httpContext.SetFeature<ISessionFeature>(sessionFeature);
            httpContext.Session.SetString("Session", cartId);

            var dbContext = _serviceProvider.GetRequiredService<MusicStoreContext>();
            var cartItems = CreateTestCartItems(
                cartId,
                itemPrice: 10,
                numberOfItem: 5);
            dbContext.AddRange(cartItems.Select(n => n.Album).Distinct());
            dbContext.AddRange(cartItems);
            dbContext.SaveChanges();

            var controller = new ShoppingCartController()
            {
                DbContext = dbContext,
            };
            controller.ActionContext.HttpContext = httpContext;

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData);
            Assert.Null(viewResult.ViewName);

            var model = Assert.IsType<ShoppingCartViewModel>(viewResult.ViewData.Model);
            Assert.Equal(5, model.CartItems.Count);
            Assert.Equal(5 * 10, model.CartTotal);
        }

        private static ISession CreateTestSession()
        {

            return new DistributedSession(
                new LocalCache(new MemoryCache(new MemoryCacheOptions())),
                "sessionId_A",
                idleTimeout: TimeSpan.MaxValue,
                tryEstablishSession: () => true,
                loggerFactory: new NullLoggerFactory(),
                isNewSessionKey: true);
        }

        private static CartItem[] CreateTestCartItems(string cartId, decimal itemPrice, int numberOfItem)
        {
            var albums = Enumerable.Range(1, 10).Select(n =>
                new Album()
                {
                    AlbumId = n,
                    Price = itemPrice,
                }).ToArray();

            var cartItems = Enumerable.Range(1, numberOfItem).Select(n =>
                new CartItem()
                {
                    Count = 1,
                    CartId = cartId,
                    AlbumId = n,
                    Album = albums[n - 1],
                }).ToArray();

            return cartItems;
        }
    }
}