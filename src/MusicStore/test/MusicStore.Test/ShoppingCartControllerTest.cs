using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using MusicStore.Models;
using MusicStore.ViewModels;
using Xunit;

namespace MusicStore.Controllers
{
    public class ShoppingCartControllerTest : IClassFixture<ShoppingCartControllerTest.Fixture>
    {
        private readonly Fixture _fixture;

        public ShoppingCartControllerTest(Fixture fixture)
        {
            _fixture = fixture;
            _fixture.CreateDatabase();
        }

        [Fact]
        public async Task Index_ReturnsNoCartItems_WhenSessionEmpty()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();

            var controller = new ShoppingCartController(
                _fixture.Context,
                _fixture.ServiceProvider.GetService<ILogger<ShoppingCartController>>());
            controller.ControllerContext.HttpContext = httpContext;

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData);
            Assert.Null(viewResult.ViewName);

            var model = Assert.IsType<ShoppingCartViewModel>(viewResult.ViewData.Model);
            Assert.Empty(model.CartItems);
            Assert.Equal(0, model.CartTotal);
        }

        [Fact]
        public async Task Index_ReturnsNoCartItems_WhenNoItemsInCart()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Session.SetString("Session", "CartId_A");

            var controller = new ShoppingCartController(
                _fixture.Context,
                _fixture.ServiceProvider.GetService<ILogger<ShoppingCartController>>());
            controller.ControllerContext.HttpContext = httpContext;

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData);
            Assert.Null(viewResult.ViewName);

            var model = Assert.IsType<ShoppingCartViewModel>(viewResult.ViewData.Model);
            Assert.Empty(model.CartItems);
            Assert.Equal(0, model.CartTotal);
        }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/12097")]
        public async Task Index_ReturnsCartItems_WhenItemsInCart()
        {
            // Arrange
            var cartId = "CartId_A";
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Session.SetString("Session", cartId);

            var dbContext = _fixture.Context;
            var cartItems = CreateTestCartItems(
                cartId,
                itemPrice: 10,
                numberOfItem: 5);
            dbContext.AddRange(cartItems.Select(n => n.Album).Distinct());
            dbContext.AddRange(cartItems);
            dbContext.SaveChanges();

            var controller = new ShoppingCartController(
                dbContext,
                _fixture.ServiceProvider.GetService<ILogger<ShoppingCartController>>());
            controller.ControllerContext.HttpContext = httpContext;

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

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/12097")]
        public async Task AddToCart_AddsItemToCart()
        {
            // Arrange
            var albumId = 3;
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Session.SetString("Session", "CartId_A");

            // Creates the albums of AlbumId = 1 ~ 10.
            var dbContext = _fixture.Context;
            var albums = CreateTestAlbums(
                10,
                new Artist
                {
                    ArtistId = 1, Name = "Kung Fu Kenny"
                }, new Genre
                {
                    GenreId = 1, Name = "Rap"
                });

            dbContext.AddRange(albums);
            dbContext.SaveChanges();

            var controller = new ShoppingCartController(
                dbContext,
                _fixture.ServiceProvider.GetService<ILogger<ShoppingCartController>>());
            controller.ControllerContext.HttpContext = httpContext;

            // Act
            var result = await controller.AddToCart(albumId, CancellationToken.None);

            // Assert
            var cart = ShoppingCart.GetCart(dbContext, httpContext);
            Assert.Single(await cart.GetCartItems());
            Assert.Equal(albumId, (await cart.GetCartItems()).Single().AlbumId);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Null(redirectResult.ControllerName);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/12097")]
        public async Task RemoveFromCart_RemovesItemFromCart()
        {
            // Arrange
            var cartId = "CartId_A";
            var cartItemId = 3;
            var numberOfItem = 5;
            var unitPrice = 10;
            var httpContext = new DefaultHttpContext();

            // Session and cart initialization
            httpContext.Session = new TestSession();
            httpContext.Session.SetString("Session", cartId);

            // DbContext initialization
            var dbContext = _fixture.Context;
            var cartItems = CreateTestCartItems(cartId, unitPrice, numberOfItem);
            dbContext.AddRange(cartItems.Select(n => n.Album).Distinct());
            dbContext.AddRange(cartItems);
            dbContext.SaveChanges();

            // ServiceProvder initialization
            var serviceProviderFeature = new ServiceProvidersFeature();
            httpContext.Features.Set<IServiceProvidersFeature>(serviceProviderFeature);

            // AntiForgery initialization
            serviceProviderFeature.RequestServices = _fixture.ServiceProvider;
            var antiForgery = serviceProviderFeature.RequestServices.GetRequiredService<IAntiforgery>();
            var tokens = antiForgery.GetTokens(httpContext);

            // Header initialization for AntiForgery
            var headers = new KeyValuePair<string, StringValues>(
                "RequestVerificationToken",
                new string[] { tokens.CookieToken + ":" + tokens.RequestToken });
            httpContext.Request.Headers.Add(headers);

            // Cotroller initialization
            var controller = new ShoppingCartController(
                dbContext,
                _fixture.ServiceProvider.GetService<ILogger<ShoppingCartController>>());
            controller.ControllerContext.HttpContext = httpContext;

            // Act
            var result = await controller.RemoveFromCart(cartItemId, CancellationToken.None);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var viewModel = Assert.IsType<ShoppingCartRemoveViewModel>(jsonResult.Value);
            Assert.Equal(numberOfItem - 1, viewModel.CartCount);
            Assert.Equal((numberOfItem - 1) * 10, viewModel.CartTotal);
            Assert.Equal("Greatest Hits has been removed from your shopping cart.", viewModel.Message);

            var cart = ShoppingCart.GetCart(dbContext, httpContext);
            Assert.DoesNotContain((await cart.GetCartItems()), c => c.CartItemId == cartItemId);
        }

        private static CartItem[] CreateTestCartItems(string cartId, decimal itemPrice, int numberOfItem)
        {
            var albums = CreateTestAlbums(
                itemPrice, new Artist
                {
                    ArtistId = 1, Name = "Kung Fu Kenny"
                }, new Genre
                {
                    GenreId = 1, Name = "Rap"
                });

            var cartItems = Enumerable.Range(1, numberOfItem).Select(n =>
                new CartItem()
                {
                    Count = 1,
                    CartId = cartId,
                    AlbumId = n % albums.Length,
                    Album = albums[n % albums.Length],
                }).ToArray();

            return cartItems;
        }

        private static Album[] CreateTestAlbums(decimal itemPrice, Artist artist, Genre genre)
        {
            return Enumerable.Range(1, 10).Select(n =>
                new Album
                {
                    Title = "Greatest Hits",
                    AlbumId = n,
                    Price = itemPrice,
                    Artist = artist,
                    Genre = genre
                }).ToArray();
        }

        public class Fixture : SqliteInMemoryFixture
        {
            public override IServiceCollection ConfigureServices(IServiceCollection services)
            {
                services = base.ConfigureServices(services);

                services.AddMvc();

                return services;
            }
        }
    }
}
