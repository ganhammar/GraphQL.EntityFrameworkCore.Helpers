using System;
using System.Collections.Generic;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure
{
    public abstract class StarWarsCharacter
    {
        public Guid Id { get; set; }
        [Filterable]
        public string Name { get; set; }
    }

    public class Human : StarWarsCharacter
    {
        public string HomePlanet { get; set; }
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTimeOffset UpdatedAtLocalTime { get; set; }
        public IEnumerable<Human> Friends { get; set; }
        public IEnumerable<Droid> Droids { get; set; }
    }

    public class Droid : StarWarsCharacter
    {
        public string PrimaryFunction { get; set; }
        public Guid OwnerId { get; set; }
        public Human Owner { get; set; }
    }
}