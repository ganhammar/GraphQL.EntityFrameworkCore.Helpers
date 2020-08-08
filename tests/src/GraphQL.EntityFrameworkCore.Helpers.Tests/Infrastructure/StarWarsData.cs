using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure
{
    public static class StarWarsData
    {
        public static Guid AnakinId = Guid.NewGuid();
        public static Guid LeiaId = Guid.NewGuid();
        public static Guid LukeId = Guid.NewGuid();

        public static Guid TatooineId = Guid.NewGuid();
        public static Guid AlderaanId = Guid.NewGuid();

        public static async Task Seed(TestDbContext dbContext)
        {
            var tatooine = new Planet
            {
                Id = TatooineId,
                Name = "Tatooine",
                Region = "Outer Rim",
                Sector = "Arkanis",
                System = "Tatoo",
            };
            var alderaan = new Planet
            {
                Id = AlderaanId,
                Name = "Alderaan",
                Region = "Core Worlds",
                Sector = "Alderaan",
                System = "Alderaan",
            };
            dbContext.Planets.Add(tatooine);
            dbContext.Planets.Add(alderaan);
            
            var luke = new Human
            {
                Id = LukeId,
                Name = "Luke",
                Species = "Human",
                HomePlanet = tatooine,
                EyeColor = "Blue",
                Order = 2,
                CreatedAt = DateTime.Now.AddHours(-1),
                UpdatedAtLocalTime = DateTimeOffset.Now.AddHours(-1),
            };
            var leia = new Human
            {
                Id = LeiaId,
                Name = "Leia",
                Species = "Human",
                HomePlanet = alderaan,
                EyeColor = "Brown",
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
                Id = AnakinId,
                Name = "Anakin",
                Species = "Human",
                HomePlanet = tatooine,
                EyeColor = "Blue",
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