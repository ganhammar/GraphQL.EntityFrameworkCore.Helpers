using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure
{
    public static class StarWarsData
    {
        public static async Task Seed(TestDbContext dbContext)
        {
            var luke = new Human
            {
                Id = Guid.NewGuid(),
                Name = "Luke",
                HomePlanet = "Tatooine",
                Order = 2,
                CreatedAt = DateTime.Now.AddHours(-1),
                UpdatedAtLocalTime = DateTimeOffset.Now.AddHours(-1),
            };
            var leia = new Human
            {
                Id = Guid.NewGuid(),
                Name = "Leia",
                HomePlanet = "Alderaan",
                Friends = new List<Human> { luke },
                Order = 1,
                CreatedAt = DateTime.Now.AddHours(-3),
                UpdatedAt = DateTime.Now,
                UpdatedAtLocalTime = DateTimeOffset.Now.AddHours(-3),
            };
            luke.Friends = new List<Human> { leia };
            dbContext.Humans.Add(luke);
            dbContext.Humans.Add(leia);
            dbContext.Humans.Add(new Human
            {
                Id = Guid.NewGuid(),
                Name = "Vader",
                HomePlanet = "Tatooine",
                Order = 11,
                CreatedAt = DateTime.Now.AddHours(-2),
                UpdatedAtLocalTime = DateTimeOffset.Now.AddHours(-2),
            });

            dbContext.Droids.Add(new Droid
            {
                Id = Guid.NewGuid(),
                Name = "R2-D2",
                PrimaryFunction = "Astromech",
                Owner = luke,
            });
            dbContext.Droids.Add(new Droid
            {
                Id = Guid.NewGuid(),
                Name = "C-3PO",
                PrimaryFunction = "Protocol",
                Owner = luke,
            });

            await dbContext.SaveChangesAsync();
        }
    }
}