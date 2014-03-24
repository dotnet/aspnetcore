// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace MusicStore.Models
{
    /// <summary>
    /// Bug: Mocked entities set. We should substitute this with DbSet once EF is available. 
    /// </summary>
    public class MusicStoreEntities
    {
        public List<Album> Albums { get; set; }
        public List<Genre> Genres { get; set; }
        public List<Artist> Artists { get; set; }
        public List<Cart> Carts { get; set; }
        public List<Order> Orders { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }

        /// <summary>
        /// Bug: Need to remove this method. Just adding this to unblock from compilation errors
        /// </summary>
        public void SaveChanges()
        {

        }

        private static MusicStoreEntities instance;

        public static MusicStoreEntities Instance
        {
            get
            {
                //TODO: Sync issues not handled. 
                if (instance == null)
                {
                    instance = new MusicStoreEntities();
                    SampleData.Seed(instance);
                }

                return instance;
            }
        }

        /// <summary>
        /// Bug: This is to just initialize the lists. Once we have EF this should be removed.
        /// </summary>
        /// <param name="dummy"></param>
        private MusicStoreEntities()
        {
            this.Albums = new List<Album>();
            this.Genres = new List<Genre>();
            this.Artists = new List<Artist>();
            this.Carts = new List<Cart>();
            this.Orders = new List<Order>();
            this.OrderDetails = new List<OrderDetail>();
        }
    }
}