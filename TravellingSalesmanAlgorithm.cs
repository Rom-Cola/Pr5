using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pfz.TravellingSalesman
{
    public sealed class TravellingSalesmanAlgorithm
    {
        private readonly Location _startLocation;
        private readonly KeyValuePair<Location[], double>[] _populationWithDistances;
        private readonly MongoDbService _mongoDbService;
        private int _generation;

        public KeyValuePair<Location[], double>[] PopulationWithDistances
        {
            get => _populationWithDistances;
        }

        public TravellingSalesmanAlgorithm(Location startLocation, Location[] destinations, int populationCount)
        {
            if (startLocation == null)
                throw new ArgumentNullException(nameof(startLocation));

            if (destinations == null)
                throw new ArgumentNullException(nameof(destinations));

            if (populationCount < 2)
                throw new ArgumentOutOfRangeException(nameof(populationCount));

            if (populationCount % 2 != 0)
                throw new ArgumentException("The populationCount parameter must be an even value.", nameof(populationCount));

            _startLocation = startLocation;
            destinations = (Location[])destinations.Clone();

            foreach (var destination in destinations)
                if (destination == null)
                    throw new ArgumentException("The destinations array can't contain null values.", nameof(destinations));

            _populationWithDistances = new KeyValuePair<Location[], double>[populationCount];

            // Create initial population.
            for (int solutionIndex = 0; solutionIndex < populationCount; solutionIndex++)
            {
                var newPossibleDestinations = (Location[])destinations.Clone();

                for (int randomIndex = 0; randomIndex < newPossibleDestinations.Length; randomIndex++)
                    RandomProvider.FullyRandomizeLocations(newPossibleDestinations);

                var distance = Location.GetTotalDistance(startLocation, newPossibleDestinations);
                var pair = new KeyValuePair<Location[], double>(newPossibleDestinations, distance);

                _populationWithDistances[solutionIndex] = pair;
            }

            Array.Sort(_populationWithDistances, _sortDelegate);

            _mongoDbService = new MongoDbService("mongodb://localhost:27017/", "TravellingSalesmanDb", "PopulationStatistics");
            _generation = 0;
        }

        private static readonly Comparison<KeyValuePair<Location[], double>> _sortDelegate = _Sort;
        private static int _Sort(KeyValuePair<Location[], double> value1, KeyValuePair<Location[], double> value2)
        {
            return value1.Value.CompareTo(value2.Value);
        }

        public IEnumerable<Location> GetBestSolutionSoFar()
        {
            foreach (var location in _populationWithDistances[0].Key)
                yield return location;
        }

        public bool MustMutateFailedCrossovers { get; set; }
        public bool MustDoCrossovers { get; set; }

        public void Reproduce()
        {
            Console.WriteLine("Reproduce method called");
            var bestSoFar = _populationWithDistances[0];

            int halfCount = _populationWithDistances.Length / 2;
            Parallel.For(0, halfCount, i =>
            {
                var parent = _populationWithDistances[i].Key;
                var child1 = _Reproduce(parent);
                var child2 = _Reproduce(parent);

                var pair1 = new KeyValuePair<Location[], double>(child1, Location.GetTotalDistance(_startLocation, child1));
                var pair2 = new KeyValuePair<Location[], double>(child2, Location.GetTotalDistance(_startLocation, child2));
                _populationWithDistances[i * 2] = pair1;
                _populationWithDistances[i * 2 + 1] = pair2;
            });

            // We keep the best alive from one generation to the other.
            _populationWithDistances[_populationWithDistances.Length - 1] = bestSoFar;

            Array.Sort(_populationWithDistances, _sortDelegate);

            // Save statistics to MongoDB
            SavePopulationStatistics();
        }

        public void MutateDuplicates()
        {
            Console.WriteLine("MutateDuplicates method called");
            bool needToSortAgain = false;
            int countDuplicates = 0;

            var previous = _populationWithDistances[0];
            Parallel.For(1, _populationWithDistances.Length, i =>
            {
                var current = _populationWithDistances[i];
                if (!previous.Key.SequenceEqual(current.Key))
                {
                    previous = current;
                    return;
                }

                countDuplicates++;

                needToSortAgain = true;
                RandomProvider.MutateRandomLocations(current.Key);
                _populationWithDistances[i] = new KeyValuePair<Location[], double>(current.Key, Location.GetTotalDistance(_startLocation, current.Key));
            });

            if (needToSortAgain)
                Array.Sort(_populationWithDistances, _sortDelegate);

            // Save statistics to MongoDb
            SavePopulationStatistics();
        }

        private Location[] _Reproduce(Location[] parent)
        {
            var result = (Location[])parent.Clone();

            if (!MustDoCrossovers)
            {
                // When we are not using cross-overs, we always apply mutations.
                RandomProvider.MutateRandomLocations(result);
                return result;
            }

            int otherIndex = RandomProvider.GetRandomValue(_populationWithDistances.Length / 2);
            var other = _populationWithDistances[otherIndex].Key;
            RandomProvider.CrossOver(result, other, MustMutateFailedCrossovers);

            if (!MustMutateFailedCrossovers)
                if (RandomProvider.GetRandomValue(10) == 0)
                    RandomProvider.MutateRandomLocations(result);

            return result;
        }

        private void SavePopulationStatistics()
        {
            var distances = _populationWithDistances.Select(p => p.Value).ToList();
            _mongoDbService.InsertPopulationStatistics(_generation++, distances);
        }
    }
}
