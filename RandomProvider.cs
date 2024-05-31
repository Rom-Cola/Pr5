using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Pfz.TravellingSalesman
{
	public static class RandomProvider
	{
		private static readonly Random _random = new Random();

		public static int GetRandomValue(int limit)
		{
			return _random.Next(limit);
		}

		public static Location[] GetRandomDestinations(int count)
		{
			if (count < 2)
				throw new ArgumentOutOfRangeException(nameof(count));

			Location[] result = new Location[count];
			for(int i=0; i<count; i++)
			{
				int x = GetRandomValue(700) + 50;
				int y = GetRandomValue(500) + 50;
				result[i] = new Location(x, y);
			}

			return result;
		}

		public static void MutateRandomLocations(Location[] locations)
		{
			if (locations == null)
				throw new ArgumentNullException(nameof(locations));

			if (locations.Length < 2)
				throw new ArgumentException("The locations array must have at least two items.", nameof(locations));

		
			int mutationCount = GetRandomValue(locations.Length/10) + 1;
			for(int mutationIndex=0; mutationIndex<mutationCount; mutationIndex++)
			{
				int index1 = GetRandomValue(locations.Length);
				int index2 = GetRandomValue(locations.Length-1);
				if (index2 >= index1)
					index2++;

				switch(GetRandomValue(3))
				{
					case 0: Location.SwapLocations(locations, index1, index2); break;
					case 1: Location.MoveLocations(locations, index1, index2); break;
					case 2: Location.ReverseRange(locations, index1, index2); break;
					default: throw new InvalidOperationException();
				}
			}
		}

		public static void FullyRandomizeLocations(Location[] locations)
		{
			if (locations == null)
				throw new ArgumentNullException(nameof(locations));
			
			int count = locations.Length;
			for(int i=count-1; i>0; i--)
			{
				int value = GetRandomValue(i+1);
				if (value != i)
					Location.SwapLocations(locations, i, value);
			}
		}

		public static void CrossOver(Location[] locations1, Location[] locations2, bool mutateFailedCrossovers)
		{
			if (locations1 == null)
				throw new ArgumentNullException(nameof(locations1));

			if (locations1.Length < 2)
				throw new ArgumentException("The locations1 array must have at least two items.", nameof(locations1));
			
			if (locations2 == null)
				throw new ArgumentNullException(nameof(locations2));

			if (locations2.Length < 2)
				throw new ArgumentException("The locations2 array must have at least two items.", nameof(locations2));

			var availableLocations = new HashSet<Location>(locations1);

			int startPosition = GetRandomValue(locations1.Length);
			int crossOverCount = GetRandomValue(locations1.Length - startPosition);

			if (mutateFailedCrossovers)
			{
				bool useMutation = true;
				int pastEndPosition = startPosition + crossOverCount;
				for (int i=startPosition; i<pastEndPosition; i++)
				{
					if (locations1[i] != locations2[i])
					{
						useMutation = false;
						break;
					}
				}

				// if the crossover is not going to give any change, we
				// force a mutation.
				if (useMutation)
				{
					MutateRandomLocations(locations1);
					return;
				}
			}

			Array.Copy(locations2, startPosition, locations1, startPosition, crossOverCount);
			List<int> toReplaceIndexes = null;

			// Now we will remove the used locations from the available locations.
			// If we can't remove one, this means it was used in duplicate. At this
			// moment we only register those indexes that have duplicate locations.
			int index = 0;
			foreach(var value in locations1)
			{
				if (!availableLocations.Remove(value))
				{
					if (toReplaceIndexes == null)
						toReplaceIndexes = new List<int>();

					toReplaceIndexes.Add(index);
				}

				index++;
			}

			// Finally we will replace duplicated items by those that are still available.
			// This is how we avoid having chromosomes that contain duplicated places to go.
			if (toReplaceIndexes != null)
			{
				// To do this, we enumerate two objects in parallel.
				// If we could use foreach(var indexToReplace, location from toReplaceIndexex, location1) it would be great.
				using(var enumeratorIndex = toReplaceIndexes.GetEnumerator())
				{
					using(var enumeratorLocation = availableLocations.GetEnumerator())
					{
						while(true)
						{
							if (!enumeratorIndex.MoveNext())
							{
								Debug.Assert(!enumeratorLocation.MoveNext());
								break;
							}

							if (!enumeratorLocation.MoveNext())
								throw new InvalidOperationException("Something wrong happened.");

							locations1[enumeratorIndex.Current] = enumeratorLocation.Current;
						}
					}
				}
			}
		}
	}
}
