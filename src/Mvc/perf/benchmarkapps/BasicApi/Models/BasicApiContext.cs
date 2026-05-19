// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

namespace BasicApi.Models
{
    public class BasicApiContext : DbContext
    {
        public BasicApiContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Image> Images { get; set; }

        public DbSet<Pet> Pets { get; set; }

        public DbSet<Tag> Tags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var id = -1;
            var categories = new[]
            {
                new Category { Id = id--, Name = "Dogs" },
                new Category { Id = id--, Name = "Cats" },
                new Category { Id = id--, Name = "Rabbits" },
                new Category { Id = id, Name = "Lions" },
            };

            id = -1;
            var categoryId = -1;
            var pets = new[]
            {
                new
                {
                    Age = 1,
                    CategoryId = categoryId,
                    HasVaccinations = true,
                    Id = id--,
                    Name = "Dogs1",
                    Status = "available",
                },
                new
                {
                    Age = 1,
                    CategoryId = categoryId,
                    HasVaccinations = true,
                    Id = id--,
                    Name = "Dogs2",
                    Status = "available",
                },
                new
                {
                    Age = 1,
                    CategoryId = categoryId--,
                    HasVaccinations = true,
                    Id = id--,
                    Name = "Dogs3",
                    Status = "available",
                },
                new
                {
                    Age = 1,
                    CategoryId = categoryId,
                    HasVaccinations = true,
                    Id = id--,
                    Name = "Cats1",
                    Status = "available",
                },
                new
                {
                    Age = 1,
                    CategoryId = categoryId,
                    HasVaccinations = true,
                    Id = id--,
                    Name = "Cats2",
                    Status = "available",
                },
                new
                {
                    Age = 1,
                    CategoryId = categoryId--,
                    HasVaccinations = true,
                    Id = id--,
                    Name = "Cats3",
                    Status = "available",
                },
                new
                {
                    Age = 1,
                    CategoryId = categoryId,
                    HasVaccinations = true,
                    Id = id--,
                    Name = "Rabbits1",
                    Status = "available",
                },
                new
                {
                    Age = 1,
                    CategoryId = categoryId,
                    HasVaccinations = true,
                    Id = id--,
                    Name = "Rabbits2",
                    Status = "available",
                },
                new
                {
                    Age = 1,
                    CategoryId = categoryId--,
                    HasVaccinations = true,
                    Id = id--,
                    Name = "Rabbits3",
                    Status = "available",
                },
                new
                {
                    Age = 1,
                    CategoryId = categoryId,
                    HasVaccinations = true,
                    Id = id--,
                    Name = "Lions1",
                    Status = "available",
                },
                new
                {
                    Age = 1,
                    CategoryId = categoryId,
                    HasVaccinations = true,
                    Id = id--,
                    Name = "Lions2",
                    Status = "available",
                },
                new
                {
                    Age = 1,
                    CategoryId = categoryId,
                    HasVaccinations = true,
                    Id = id,
                    Name = "Lions3",
                    Status = "available",
                },
            };

            id = -1;
            var images = new[]
            {
                new { Id = id, PetId = id, Url = $"http://example.com/pets/{id--}_1.png" },
                new { Id = id, PetId = id, Url = $"http://example.com/pets/{id--}_1.png" },
                new { Id = id, PetId = id, Url = $"http://example.com/pets/{id--}_1.png" },
                new { Id = id, PetId = id, Url = $"http://example.com/pets/{id--}_1.png" },
                new { Id = id, PetId = id, Url = $"http://example.com/pets/{id--}_1.png" },
                new { Id = id, PetId = id, Url = $"http://example.com/pets/{id--}_1.png" },
                new { Id = id, PetId = id, Url = $"http://example.com/pets/{id--}_1.png" },
                new { Id = id, PetId = id, Url = $"http://example.com/pets/{id--}_1.png" },
                new { Id = id, PetId = id, Url = $"http://example.com/pets/{id--}_1.png" },
                new { Id = id, PetId = id, Url = $"http://example.com/pets/{id--}_1.png" },
                new { Id = id, PetId = id, Url = $"http://example.com/pets/{id--}_1.png" },
                new { Id = id, PetId = id, Url = $"http://example.com/pets/{id}_1.png" },
            };

            id = -1;
            var tags = new[]
            {
                new { Id = id, PetId = id--, Name = "Tag1" },
                new { Id = id, PetId = id--, Name = "Tag1" },
                new { Id = id, PetId = id--, Name = "Tag1" },
                new { Id = id, PetId = id--, Name = "Tag1" },
                new { Id = id, PetId = id--, Name = "Tag1" },
                new { Id = id, PetId = id--, Name = "Tag1" },
                new { Id = id, PetId = id--, Name = "Tag1" },
                new { Id = id, PetId = id--, Name = "Tag1" },
                new { Id = id, PetId = id--, Name = "Tag1" },
                new { Id = id, PetId = id--, Name = "Tag1" },
                new { Id = id, PetId = id--, Name = "Tag1" },
                new { Id = id, PetId = id, Name = "Tag1" },
            };

            modelBuilder.Entity<Category>().HasData(categories);
            modelBuilder.Entity<Pet>().HasData(pets);
            modelBuilder.Entity<Image>().HasData(images);
            modelBuilder.Entity<Tag>().HasData(tags);
        }
    }
}
