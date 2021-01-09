using System;
using System.Collections.Generic;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure
{
    public abstract class StarWarsCharacter
    {
        public Guid Id { get; set; }
        [IsFilterable]
        public string Name { get; set; }
    }

    public class Human : StarWarsCharacter
    {
        [IsFilterable]
        public string Species { get; set; }
        [IsFilterable]
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
        public HumanForceAlignment ForceAlignment { get; set; }
    }

    public class Droid : StarWarsCharacter
    {
        public string PrimaryFunction { get; set; }
        public Guid OwnerId { get; set; }
        public Human Owner { get; set; }
    }

    public class Planet : StarWarsCharacter
    {
        public string Region { get; set; }
        public string Sector { get; set; }
        public string System { get; set; }
        public IEnumerable<Human> Habitants { get; set; }
    }

    public class Force
    {
        public string Type { get; set; }
        public IEnumerable<HumanForceAlignment> HumanForceAlignments { get; set; }
    }

    public class HumanForceAlignment
    {
        public Guid HumanId { get; set; }
        public string Alignment { get; set; }
        public Human Human { get; set; }
        public Force Force { get; set; }
    }

    public class Galaxy : StarWarsCharacter
    {
    }
}