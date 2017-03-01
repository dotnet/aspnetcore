// Copyright (c) .NET Foundation. All rights reserved.
// See License.txt in the project root for license information

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MusicStore.Models;
using Xunit;

namespace MusicStore.Test
{
    public class ShoppingCartTest : IClassFixture<ShoppingCartFixture>
    {
        private readonly ShoppingCartFixture _fixture;

        public ShoppingCartTest(ShoppingCartFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async void ComputesTotal()
        {
            var cartId = Guid.NewGuid().ToString();
            using (var db = _fixture.CreateContext())
            {
                var a = db.Albums.Add(
                    new Album
                    {
                        Price = 15.99m
                    }).Entity;

                db.CartItems.Add(new CartItem { Album = a, Count = 2, CartId = cartId });

                db.SaveChanges();

                Assert.Equal(31.98m, await ShoppingCart.GetCart(db, cartId).GetTotal());
            }
        }
    }

    public class ShoppingCartFixture
    {
        private readonly IServiceProvider _serviceProvider;

        public ShoppingCartFixture()
        {
            var efServiceProvider = new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

            var services = new ServiceCollection();

            services.AddDbContext<MusicStoreContext>(b => b.UseInMemoryDatabase("Scratch").UseInternalServiceProvider(efServiceProvider));

            _serviceProvider = services.BuildServiceProvider();
        }

        public virtual MusicStoreContext CreateContext()
            => _serviceProvider.GetRequiredService<MusicStoreContext>();
    }
}
