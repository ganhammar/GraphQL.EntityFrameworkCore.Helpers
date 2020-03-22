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
            };
            var leia = new Human
            {
                Id = Guid.NewGuid(),
                Name = "Leia",
                HomePlanet = "Alderaan",
                Friends = new List<Human> { luke },
            };
            luke.Friends = new List<Human> { leia };
            dbContext.Add(luke);
            dbContext.Add(leia);
            dbContext.Add(new Human
            {
                Id = Guid.NewGuid(),
                Name = "Vader",
                HomePlanet = "Tatooine",
            });

            dbContext.Add(new Droid
            {
                Id = Guid.NewGuid(),
                Name = "R2-D2",
                PrimaryFunction = "Astromech",
                Owner = luke,
            });
            dbContext.Add(new Droid
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