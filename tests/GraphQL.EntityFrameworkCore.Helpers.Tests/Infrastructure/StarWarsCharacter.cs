using System;
using System.Collections.Generic;
using GraphQL.EntityFrameworkCore.Helpers.Connection;

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
        [Filterable]
        public string Species { get; set; }
        [Filterable]
        public string EyeColor { get; set; }
        public Guid HomePlanetId { get; set; }
        [Unique]
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; }
        [Unique]
        public DateTime? UpdatedAt { get; set; }
        public DateTimeOffset UpdatedAtLocalTime { get; set; }
        public Planet HomePlanet { get; set; }
        public IEnumerable<Human> Friends { get; set; }
        public IEnumerable<Droid> Droids { get; set; }
    }

    public class Droid : StarWarsCharacter
    {
        public string PrimaryFunction { get; set; }
        public Guid OwnerId { get; set; }
        public Human Owner { get; set; }
    }

    public class Planet
    {
        public Guid Id { get; set; }
        [Filterable]
        public string Name { get; set; }
        [Filterable]
        public string Region { get; set; }
        [Filterable]
        public string Sector { get; set; }
        [Filterable]
        public string System { get; set; }
    }
}