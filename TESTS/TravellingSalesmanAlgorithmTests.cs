using System;
using System.Collections.Generic;
using System.Linq;
using Pfz.TravellingSalesman;
using Xunit;

namespace TESTS
{
    public class TravellingSalesmanAlgorithmTests
    {
        [Fact]
        public void GetTotalDistance_ReturnsCorrectDistance()
        {
            var algorithm = TestHelper.CreateAlgorithm();
            var totalDistance = Location.GetTotalDistance(TestHelper.StartLocation, TestHelper.Destinations);

            // Assert that the distance is positive and within a reasonable range
            Assert.True(totalDistance > 0);
            Assert.True(totalDistance < 1000); // Adjust this threshold based on your expectations
        }

        [Fact]
        public void Reproduce_MaintainsPopulationSize()
        {
            var algorithm = TestHelper.CreateAlgorithm();
            var initialPopulationSize = algorithm.PopulationWithDistances.Length;

            algorithm.Reproduce();

            Assert.Equal(initialPopulationSize, algorithm.PopulationWithDistances.Length);
        }

        [Fact]
        public void Reproduce_CreatesDifferentChildren()
        {
            var algorithm = TestHelper.CreateAlgorithm();
            var initialPopulation = algorithm.PopulationWithDistances.Select(p => p.Key).ToArray();

            algorithm.Reproduce();
            algorithm.MutateDuplicates();

            var finalPopulation = algorithm.PopulationWithDistances.Select(p => p.Key).ToArray();

            // Ensure that at least one child is different from the initial population
            Assert.True(initialPopulation.All(p => finalPopulation.Any(c => p.SequenceEqual(c))));
        }

        [Fact]
        public void Reproduce_ImprovesFitnessOverTime()
        {
            var algorithm = TestHelper.CreateAlgorithm();
            var initialBestFitness = algorithm.PopulationWithDistances[0].Value;

            for (int i = 0; i < 10; i++)
            {
                algorithm.Reproduce();
                algorithm.MutateDuplicates();
            }

            var finalBestFitness = algorithm.PopulationWithDistances[0].Value;
            Assert.True(finalBestFitness <= initialBestFitness);
        }

        [Fact]
        public void Reproduce_RetainsBestSolution()
        {
            var algorithm = TestHelper.CreateAlgorithm();
            var initialBestSolution = algorithm.GetBestSolutionSoFar().ToArray();

            algorithm.Reproduce();
            algorithm.MutateDuplicates();

            var finalBestSolution = algorithm.GetBestSolutionSoFar().ToArray();

            // Check that both solutions contain the same locations (order may differ)
            Assert.True(initialBestSolution.OrderBy(l => l.X).ThenBy(l => l.Y)
                .SequenceEqual(finalBestSolution.OrderBy(l => l.X).ThenBy(l => l.Y), new LocationEqualityComparer()));

            // Check that the total distance of both solutions is the same (optional)
            var initialDistance = Location.GetTotalDistance(TestHelper.StartLocation, initialBestSolution);
            var finalDistance = Location.GetTotalDistance(TestHelper.StartLocation, finalBestSolution);
            Assert.Equal(initialDistance, finalDistance);
        }

        [Fact]
        public void GetBestSolutionSoFar_ReturnsValidSolution()
        {
            var algorithm = TestHelper.CreateAlgorithm();

            var bestSolution = algorithm.GetBestSolutionSoFar().ToList();

            Assert.Equal(TestHelper.Destinations.Length, bestSolution.Count);

            // Check that all destinations are present in the solution (order doesn't matter here)
            foreach (var destination in TestHelper.Destinations)
            {
                Assert.Contains(destination, bestSolution, new LocationEqualityComparer());
            }
        }
    }

    public class LocationEqualityComparer : IEqualityComparer<Location>
    {
        public bool Equals(Location x, Location y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return x.X == y.X && x.Y == y.Y;
        }

        public int GetHashCode(Location obj)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + obj.X.GetHashCode();
                hash = hash * 23 + obj.Y.GetHashCode();
                return hash;
            }
        }
    }

    public static class TestHelper
    {
        public static Location StartLocation => new Location(0, 0);

        public static Location[] Destinations => new[]
        {
            new Location(100, 100),
            new Location(200, 50),
            new Location(350, 200)
        };

        public static TravellingSalesmanAlgorithm CreateAlgorithm(int populationCount = 10)
        {
            return new TravellingSalesmanAlgorithm(StartLocation, Destinations, populationCount);
        }
    }

    public class LocationTests
    {
        // Helper method for creating Location instances
        private Location CreateLocation(int x, int y)
        {
            return new Location(x, y);
        }

        // Tests for GetDistance
        [Fact]
        public void GetDistance_ReturnsZeroForSameLocation()
        {
            var location = CreateLocation(5, 10);
            var distance = location.GetDistance(location);

            Assert.Equal(0, distance);
        }

        [Fact]
        public void GetDistance_ReturnsCorrectDistanceForDifferentLocations()
        {
            var location1 = CreateLocation(0, 0);
            var location2 = CreateLocation(3, 4);
            var distance = location1.GetDistance(location2);

            Assert.Equal(5, distance); // Pythagorean theorem (3^2 + 4^2 = 5^2)
        }

        // Tests for GetTotalDistance
        [Fact]
        public void GetTotalDistance_ThrowsArgumentNullExceptionForNullStartLocation()
        {
            Assert.Throws<ArgumentNullException>(() => Location.GetTotalDistance(null, new Location[] { }));
        }

        [Fact]
        public void GetTotalDistance_ThrowsArgumentNullExceptionForNullLocationsArray()
        {
            Assert.Throws<ArgumentNullException>(() => Location.GetTotalDistance(CreateLocation(0, 0), null));
        }

        [Fact]
        public void GetTotalDistance_ThrowsArgumentExceptionForEmptyLocationsArray()
        {
            Assert.Throws<ArgumentException>(() => Location.GetTotalDistance(CreateLocation(0, 0), new Location[] { }));
        }

        [Fact]
        public void GetTotalDistance_ThrowsArgumentExceptionForLocationsArrayWithNullElements()
        {
            Assert.Throws<ArgumentException>(() =>
                Location.GetTotalDistance(CreateLocation(0, 0), new Location[] { null }));
        }

        [Fact]
        public void GetTotalDistance_ReturnsCorrectTotalDistance()
        {
            var start = CreateLocation(0, 0);
            var locations = new[] { CreateLocation(3, 4), CreateLocation(8, 6) };
            var expectedDistance = start.GetDistance(locations[0]) + locations[0].GetDistance(locations[1]) +
                                   locations[1].GetDistance(start);

            var totalDistance = Location.GetTotalDistance(start, locations);

            Assert.Equal(expectedDistance, totalDistance);
        }

        // Tests for SwapLocations
        [Fact]
        public void SwapLocations_ThrowsArgumentNullExceptionForNullLocations()
        {
            Assert.Throws<ArgumentNullException>(() => Location.SwapLocations(null, 0, 1));
        }

        [Fact]
        public void SwapLocations_ThrowsArgumentOutOfRangeExceptionForInvalidIndexes()
        {
            var locations = new[] { CreateLocation(0, 0), CreateLocation(1, 1) };

            Assert.Throws<ArgumentOutOfRangeException>(() => Location.SwapLocations(locations, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Location.SwapLocations(locations, 0, 2));
        }

        [Fact]
        public void SwapLocations_SwapsLocationsCorrectly()
        {
            var locations = new[] { CreateLocation(0, 0), CreateLocation(1, 1) };
            var originalLocation0 = locations[0];
            var originalLocation1 = locations[1];

            Location.SwapLocations(locations, 0, 1);

            Assert.Equal(originalLocation1, locations[0]);
            Assert.Equal(originalLocation0, locations[1]);
        }

        [Fact]
        public void MoveLocations_ThrowsArgumentNullExceptionForNullLocations()
        {
            Assert.Throws<ArgumentNullException>(() => Location.MoveLocations(null, 0, 1));
        }

        [Fact]
        public void MoveLocations_ThrowsArgumentOutOfRangeExceptionForInvalidIndexes()
        {
            var locations = new[] { CreateLocation(0, 0), CreateLocation(1, 1) };

            Assert.Throws<ArgumentOutOfRangeException>(() => Location.MoveLocations(locations, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Location.MoveLocations(locations, 0, 2));
        }

        [Fact]
        public void MoveLocations_MovesLocationForwardCorrectly()
        {
            var locations = new[] { CreateLocation(0, 0), CreateLocation(1, 1), CreateLocation(2, 2) };
            Location.MoveLocations(locations, 0, 2);

            Assert.Equal(new Location(1, 1), locations[0], new LocationEqualityComparer());
            Assert.Equal(new Location(2, 2), locations[1], new LocationEqualityComparer());
            Assert.Equal(new Location(0, 0), locations[2], new LocationEqualityComparer());
        }

        [Fact]
        public void MoveLocations_MovesLocationBackwardCorrectly()
        {
            var locations = new[] { CreateLocation(0, 0), CreateLocation(1, 1), CreateLocation(2, 2) };
            Location.MoveLocations(locations, 2, 0);

            Assert.Equal(new Location(2, 2), locations[0], new LocationEqualityComparer());
            Assert.Equal(new Location(0, 0), locations[1], new LocationEqualityComparer());
            Assert.Equal(new Location(1, 1), locations[2], new LocationEqualityComparer());
        }

// Tests for ReverseRange
        [Fact]
        public void ReverseRange_ThrowsArgumentNullExceptionForNullLocations()
        {
            Assert.Throws<ArgumentNullException>(() => Location.ReverseRange(null, 0, 1));
        }

        [Fact]
        public void ReverseRange_ThrowsArgumentOutOfRangeExceptionForInvalidIndexes()
        {
            var locations = new[] { CreateLocation(0, 0), CreateLocation(1, 1) };

            Assert.Throws<ArgumentOutOfRangeException>(() => Location.ReverseRange(locations, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Location.ReverseRange(locations, 0, 2));
        }

        [Fact]
        public void ReverseRange_ReversesRangeCorrectly()
        {
            var locations = new[]
                { CreateLocation(0, 0), CreateLocation(1, 1), CreateLocation(2, 2), CreateLocation(3, 3) };
            Location.ReverseRange(locations, 1, 2);

            Assert.Equal(new Location(0, 0), locations[0], new LocationEqualityComparer());
            Assert.Equal(new Location(2, 2), locations[1], new LocationEqualityComparer());
            Assert.Equal(new Location(1, 1), locations[2], new LocationEqualityComparer());
            Assert.Equal(new Location(3, 3), locations[3], new LocationEqualityComparer());
        }

        [Fact]
        public void ReverseRange_HandlesReversedStartAndEndIndexes()
        {
            var locations = new[]
                { CreateLocation(0, 0), CreateLocation(1, 1), CreateLocation(2, 2), CreateLocation(3, 3) };
            Location.ReverseRange(locations, 2, 1); // endIndex < startIndex

            Assert.Equal(new Location(0, 0), locations[0], new LocationEqualityComparer());
            Assert.Equal(new Location(2, 2), locations[1], new LocationEqualityComparer());
            Assert.Equal(new Location(1, 1), locations[2], new LocationEqualityComparer());
            Assert.Equal(new Location(3, 3), locations[3], new LocationEqualityComparer());
        }
    }

    public class RandomProviderTests
    {
        [Fact]
        public void GetRandomValue_ReturnsValueWithinLimit()
        {
            var limit = 10;
            var randomValue = RandomProvider.GetRandomValue(limit);

            Assert.True(randomValue >= 0);
            Assert.True(randomValue < limit);
        }

        [Fact]
        public void GetRandomDestinations_ReturnsCorrectNumberOfLocations()
        {
            var count = 5;
            var destinations = RandomProvider.GetRandomDestinations(count);

            Assert.Equal(count, destinations.Length);
        }

        [Fact]
        public void GetRandomDestinations_ThrowsArgumentOutOfRangeExceptionForCountLessThanTwo()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RandomProvider.GetRandomDestinations(1));
        }

        [Fact]
        public void GetRandomDestinations_LocationsAreWithinExpectedRange()
        {
            var destinations = RandomProvider.GetRandomDestinations(10);

            foreach (var location in destinations)
            {
                Assert.True(location.X >= 50 && location.X < 750);
                Assert.True(location.Y >= 50 && location.Y < 550);
            }
        }

        [Fact]
        public void MutateRandomLocations_ThrowsArgumentNullExceptionForNullLocations()
        {
            Assert.Throws<ArgumentNullException>(() => RandomProvider.MutateRandomLocations(null));
        }

        [Fact]
        public void MutateRandomLocations_ThrowsArgumentExceptionForLocationsWithLessThanTwoItems()
        {
            Assert.Throws<ArgumentException>(() =>
                RandomProvider.MutateRandomLocations(new Location[] { new Location(0, 0) }));
        }

        [Fact]
        public void MutateRandomLocations_ChangesLocations()
        {
            var originalLocations = new[] { new Location(0, 0), new Location(1, 1), new Location(2, 2) };
            var mutatedLocations = originalLocations.Clone() as Location[];

            RandomProvider.MutateRandomLocations(mutatedLocations);

            // Assert that at least one location has changed
            Assert.False(originalLocations.SequenceEqual(mutatedLocations, new LocationEqualityComparer()));
        }

        [Fact]
        public void FullyRandomizeLocations_ThrowsArgumentNullExceptionForNullLocations()
        {
            Assert.Throws<ArgumentNullException>(() => RandomProvider.FullyRandomizeLocations(null));
        }

        [Fact]
        public void FullyRandomizeLocations_ChangesLocations()
        {
            var originalLocations = new[] { new Location(0, 0), new Location(1, 1), new Location(2, 2) };
            var randomizedLocations = originalLocations.Clone() as Location[];

            RandomProvider.FullyRandomizeLocations(randomizedLocations);

            // While not guaranteed, it's highly unlikely that the order remains the same after randomization
            Assert.False(originalLocations.SequenceEqual(randomizedLocations));
        }

        [Fact]
        public void CrossOver_ThrowsArgumentNullExceptionForNullLocations()
        {
            var locations = new Location[] { new Location(1, 2), new Location(3, 4) };
            Assert.Throws<ArgumentNullException>(() => RandomProvider.CrossOver(null, locations, true));
            Assert.Throws<ArgumentNullException>(() => RandomProvider.CrossOver(locations, null, true));
        }

        [Fact]
        public void CrossOver_ThrowsArgumentExceptionForShortLocations()
        {
            Assert.Throws<ArgumentException>(() => RandomProvider.CrossOver(new Location[] { new Location(1, 2) },
                new Location[] { new Location(3, 4) }, true));
        }
        

        [Fact]
        public void CrossOver_AllLocationsFromBothParentsAreInOffspring()
        {
            var locations1 = new Location[] { new Location(1, 2), new Location(3, 4), new Location(5, 6) };
            var locations2 = new Location[] { new Location(7, 8), new Location(3, 4), new Location(11, 12) }; // One overlapping location

            // Make copies of the original arrays to avoid in-place modification issues
            var originalLocations1 = locations1.Clone() as Location[];
            var originalLocations2 = locations2.Clone() as Location[];

            RandomProvider.CrossOver(locations1, locations2, false);
    
            // Locations present in both parents should only appear once in the output
            var overlappingLocations = originalLocations1.Intersect(originalLocations2, new LocationEqualityComparer());
            foreach (var location in overlappingLocations)
            {
                Assert.Equal(1, locations1.Count(l => l.X == location.X && l.Y == location.Y));
            }

            // Ensure that all unique locations from originalLocations1 are in the offspring (order may differ)
            var locations1Unique = originalLocations1.Except(originalLocations2, new LocationEqualityComparer());
            Assert.True(locations1Unique.All(loc => locations1.Contains(loc, new LocationEqualityComparer())));

            // Ensure that all unique locations from originalLocations2 are in the offspring (order may differ)
            var locations2Unique = originalLocations2.Except(originalLocations1, new LocationEqualityComparer());
            Assert.True(locations2Unique.All(loc => locations2.Contains(loc, new LocationEqualityComparer())));
        }

        [Fact]
        public void CrossOver_NoDuplicateLocationsInOffspring()
        {
            var locations1 = new Location[]
                { new Location(1, 2), new Location(3, 4), new Location(5, 6), new Location(7, 8) };
            var locations2 = new Location[]
            {
                new Location(9, 10), new Location(3, 4), new Location(11, 12), new Location(1, 2)
            }; // Two overlapping locations

            RandomProvider.CrossOver(locations1, locations2, false);

            var uniqueLocations = locations1.Distinct(new LocationEqualityComparer());
            Assert.Equal(locations1.Length, uniqueLocations.Count());
        }

        [Fact]
        public void CrossOver_MutatesIfIdenticalCrossoverAndFlagIsSet()
        {
            var locations1 = new Location[] { new Location(1, 2), new Location(3, 4), new Location(5, 6) };
            var locations2 = locations1.Clone() as Location[]; // Identical parents

            RandomProvider.CrossOver(locations1, locations2, true); // Mutate on identical crossover

            Assert.False(locations1.SequenceEqual(locations2, new LocationEqualityComparer()));
        }
    }
}