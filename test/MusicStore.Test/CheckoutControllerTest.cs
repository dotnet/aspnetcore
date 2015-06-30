using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Microsoft.Framework.DependencyInjection;
using MusicStore.Models;
using Xunit;

namespace MusicStore.Controllers
{
    public class CheckoutControllerTest
    {
        private readonly IServiceProvider _serviceProvider;

        public CheckoutControllerTest()
        {
            var services = new ServiceCollection();

            services.AddEntityFramework()
                      .AddInMemoryDatabase()
                      .AddDbContext<MusicStoreContext>(options => options.UseInMemoryDatabase());

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public void AddressAndPayment_ReturnsDefaultView()
        {
            // Arrange
            var controller = new CheckoutController();

            // Act
            var result = controller.AddressAndPayment();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        [Fact]
        public async Task AddressAndPayment_RedirectToCompleteWhenSuccessful()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            var orderId = 10;
            var order = new Order()
            {
                OrderId = orderId,
            };

            // Session initialization
            var cartId = "CartId_A";
            httpContext.Session = new TestSession();
            httpContext.Session.SetString("Session", cartId);

            // FormCollection initialization
            httpContext.Request.Form =
                new FormCollection(
                    new Dictionary<string, string[]>()
                        { { "PromoCode", new string[] { "FREE" } } }
                    );

            // UserName initialization
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUserName") };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            
            // DbContext initialization
            var dbContext = _serviceProvider.GetRequiredService<MusicStoreContext>();
            var cartItems = CreateTestCartItems(
                cartId,
                itemPrice: 10,
                numberOfItem: 1);
            dbContext.AddRange(cartItems.Select(n => n.Album).Distinct());
            dbContext.AddRange(cartItems);
            dbContext.SaveChanges();

            var controller = new CheckoutController()
            {
                DbContext = dbContext,
            };
            controller.ActionContext.HttpContext = httpContext;
            
            // Act
            var result = await controller.AddressAndPayment(order, CancellationToken.None);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Complete", redirectResult.ActionName);
            Assert.Null(redirectResult.ControllerName);
            Assert.NotNull(redirectResult.RouteValues);

            Assert.Equal(orderId, redirectResult.RouteValues["Id"]);
        }

        [Fact]
        public async Task AddressAndPayment_ReturnsOrderIfInvalidPromoCode()
        {
            // Arrange
            var context = new DefaultHttpContext();

            // AddressAndPayment action reads the Promo code from FormCollection.
            context.Request.Form =
                new FormCollection(new Dictionary<string, string[]>());

            var controller = new CheckoutController();
            controller.ActionContext.HttpContext = context;

            // Do not need actual data for Order; the Order object will be checked for the reference equality.
            var order = new Order();

            // Act
            var result = await controller.AddressAndPayment(order, CancellationToken.None);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);

            Assert.NotNull(viewResult.ViewData);
            Assert.Same(order, viewResult.ViewData.Model);
        }

        [Fact]
        public async Task AddressAndPayment_ReturnsOrderIfRequestCanceled()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Form =
                new FormCollection(new Dictionary<string, string[]>());

            var controller = new CheckoutController();
            controller.ActionContext.HttpContext = context;

            var order = new Order();

            // Act
            var result = await controller.AddressAndPayment(order, new CancellationToken(true));

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);

            Assert.NotNull(viewResult.ViewData);
            Assert.Same(order, viewResult.ViewData.Model);
        }

        [Fact]
        public async Task AddressAndPayment_ReturnsOrderIfInvalidOrderModel()
        {
            // Arrange
            var controller = new CheckoutController();
            controller.ModelState.AddModelError("a", "ModelErrorA");

            var order = new Order();

            // Act
            var result = await controller.AddressAndPayment(order, CancellationToken.None);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);

            Assert.NotNull(viewResult.ViewData);
            Assert.Same(order, viewResult.ViewData.Model);
        }

        [Fact]
        public async Task Complete_ReturnsOrderIdIfValid()
        {
            // Arrange
            var orderId = 100;
            var userName = "TestUserA";
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, userName) };

            var httpContext = new DefaultHttpContext()
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims)),
            };

            var dbContext =
                _serviceProvider.GetRequiredService<MusicStoreContext>();
            dbContext.Add(new Order()
            {
                OrderId = orderId,
                Username = userName
            });
            dbContext.SaveChanges();

            var controller = new CheckoutController()
            {
                DbContext = dbContext,
            };
            controller.ActionContext.HttpContext = httpContext;

            // Act
            var result = await controller.Complete(orderId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);

            Assert.NotNull(viewResult.ViewData);
            Assert.Equal(orderId, viewResult.ViewData.Model);
        }

        [Fact]
        public async Task Complete_ReturnsErrorIfInvalidOrder()
        {
            // Arrange
            var invalidOrderId = 100;
            var dbContext =
                _serviceProvider.GetRequiredService<MusicStoreContext>();

            var controller = new CheckoutController()
            {
                DbContext = dbContext,
            };
            controller.ActionContext.HttpContext = new DefaultHttpContext();

            // Act
            var result = await controller.Complete(invalidOrderId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.Equal("Error", viewResult.ViewName);
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
                    AlbumId = n % 10,
                    Album = albums[n % 10],
                }).ToArray();

            return cartItems;
        }
    }
}