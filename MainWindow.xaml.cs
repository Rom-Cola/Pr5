using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Pfz.TravellingSalesman
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private int _destinationCount = 20;
		private int _populationCount = 114;
		private bool _closing;
		private bool _paused;

		private readonly Location _startLocation = new Location(50, 50);
		private readonly object _algorithmLock = new object();
		private TravellingSalesmanAlgorithm _algorithm;
		private Location[] _randomLocations;
		public MainWindow()
		{
			
			InitializeComponent();

			checkBoxRunning.Checked += new RoutedEventHandler(checkBoxRunning_Checked);
			checkBoxRunning.Unchecked += new RoutedEventHandler(checkBoxRunning_Unchecked);
			buttonResetSearch.Click += new RoutedEventHandler(buttonResetSearch_Click);
			buttonNewDestinations.Click += new RoutedEventHandler(buttonNewDestinations_Click);
			buttonMoreDestinations.Click += new RoutedEventHandler(buttonMoreDestinations_Click);
			buttonLessDestinations.Click += new RoutedEventHandler(buttonLessDestinations_Click);

			checkBoxMutateFailedCrossOvers.Checked += new RoutedEventHandler(checkBoxMutateFailedCrossOvers_Checked);
			checkBoxMutateFailedCrossOvers.Unchecked += new RoutedEventHandler(checkBoxMutateFailedCrossOvers_Unchecked);
			checkBoxMutateDuplicates.Checked += new RoutedEventHandler(checkBoxMutateDuplicates_Checked);
			checkBoxMutateDuplicates.Unchecked += new RoutedEventHandler(checkBoxMutateDuplicates_Unchecked);
			checkBoxDoCrossover.Checked += new RoutedEventHandler(checkBoxDoCrossover_Checked);
			checkBoxDoCrossover.Unchecked += new RoutedEventHandler(checkBoxDoCrossover_Unchecked);
			_populationCount = Convert.ToInt32(textBoxPopulationCount.Text);
			_randomLocations = RandomProvider.GetRandomDestinations(_destinationCount);
			_algorithm = new TravellingSalesmanAlgorithm(_startLocation, _randomLocations, _populationCount);

			_bestSolutionSoFar = _algorithm.GetBestSolutionSoFar().ToArray();
			_DrawLines();

			var thread = new Thread(_Thread);
			thread.Start();
		}

		void checkBoxDoCrossover_Checked(object sender, RoutedEventArgs e)
		{
			_mustDoCrossovers = true;
		}

		void checkBoxDoCrossover_Unchecked(object sender, RoutedEventArgs e)
		{
			_mustDoCrossovers = false;
		}

		void checkBoxMutateFailedCrossOvers_Checked(object sender, RoutedEventArgs e)
		{
			_mutateFailedCrossovers = true;
		}

		void checkBoxMutateFailedCrossOvers_Unchecked(object sender, RoutedEventArgs e)
		{
			_mutateFailedCrossovers = false;
		}

		void checkBoxMutateDuplicates_Checked(object sender, RoutedEventArgs e)
		{
			_mutateDuplicates = true;
		}

		void checkBoxMutateDuplicates_Unchecked(object sender, RoutedEventArgs e)
		{
			_mutateDuplicates = false;
		}

		private void _AdjustDestinations()
		{
			labelDestinationCount.Text = _destinationCount.ToString() + " ";
			buttonMoreDestinations.IsEnabled = _destinationCount < 57;
			buttonLessDestinations.IsEnabled = _destinationCount > 6;

			_randomLocations = RandomProvider.GetRandomDestinations(_destinationCount);

			lock(_algorithmLock)
				_algorithm = new TravellingSalesmanAlgorithm(_startLocation, _randomLocations, _populationCount);

			_bestSolutionSoFar = _algorithm.GetBestSolutionSoFar().ToArray();
			_DrawLines();
		}
		void buttonMoreDestinations_Click(object sender, RoutedEventArgs e)
		{
			// This can look stupid, but if you hold ENTER pressed over an enabled button,
			// it may become disabled and new Click events are still generated when it is already
			// disabled. So, to avoid executing it when it is disabled, we test.
			if (!buttonMoreDestinations.IsEnabled)
				return;

			_destinationCount++;

			_AdjustDestinations();
		}

		void buttonLessDestinations_Click(object sender, RoutedEventArgs e)
		{
			if (!buttonLessDestinations.IsEnabled)
				return;

			_destinationCount--;

			_AdjustDestinations();
		}

		void buttonNewDestinations_Click(object sender, RoutedEventArgs e)
		{
			_randomLocations = RandomProvider.GetRandomDestinations(_destinationCount);

			lock(_algorithmLock)
				_algorithm = new TravellingSalesmanAlgorithm(_startLocation, _randomLocations, _populationCount);

			_bestSolutionSoFar = _algorithm.GetBestSolutionSoFar().ToArray();
			_DrawLines();
		}

		void buttonResetSearch_Click(object sender, RoutedEventArgs e)
		{
			lock(_algorithmLock)
				_algorithm = new TravellingSalesmanAlgorithm(_startLocation, _randomLocations, _populationCount);

			_bestSolutionSoFar = _algorithm.GetBestSolutionSoFar().ToArray();
			_DrawLines();
		}

		void checkBoxRunning_Unchecked(object sender, RoutedEventArgs e)
		{
			lock(_lock)
				_paused = true;
		}

		void checkBoxRunning_Checked(object sender, RoutedEventArgs e)
		{
			lock(_lock)
			{
				_paused = false;
				Monitor.Pulse(_lock);
			}
		}

		private readonly object _lock = new object();
		private Location[] _bestSolutionSoFar;
		private bool _messageWaitingToBeProcessed;
		private volatile bool _mutateFailedCrossovers = false;
		private volatile bool _mutateDuplicates = false;
		private volatile bool _mustDoCrossovers = true;
		private void _Thread()
		{
			while(!_closing)
			{
				if (_paused)
				{
					lock(_lock)
					{
						if (_closing)
							return;
	
						while(_paused)
						{
							Monitor.Wait(_lock);

							if (_closing)
								return;
						}
					}
				}

				lock(_algorithmLock)
				{
					// Try setting the parameter to false to see the results.
					_algorithm.MustMutateFailedCrossovers = _mutateFailedCrossovers;
					_algorithm.MustDoCrossovers = _mustDoCrossovers;
					_algorithm.Reproduce();

					if (_mutateDuplicates)
						_algorithm.MutateDuplicates();

					var newSolution = _algorithm.GetBestSolutionSoFar().ToArray();
					if (!newSolution.SequenceEqual(_bestSolutionSoFar))
					{
						// Probably the lock is not necessary... yet I prefer to keep it.
						lock(_lock)
						{
							_bestSolutionSoFar = newSolution;

							if (!_messageWaitingToBeProcessed)
							{
								_messageWaitingToBeProcessed = true;

								// Using Dispatcher.Invoke here will cause a dead-lock, as the other thread
								// will try to get the lock that we are still holding.
								Dispatcher.BeginInvoke(new Action(_DrawLines), DispatcherPriority.ApplicationIdle);
							}
						}

						// In a real application we should not do this sleep. But here we want to show how the
						// algorithm is discovering better solutions.
						Thread.Sleep(100);
					}
				}
			}
		}

		private void _DrawLines()
		{
			Location[] bestSolutionSoFar;
			lock(_lock) // again, this lock is probably not necessary.
			{
				_messageWaitingToBeProcessed = false;
				bestSolutionSoFar = _bestSolutionSoFar;
			}

			labelDistance.Content = (long)Location.GetTotalDistance(_startLocation, bestSolutionSoFar);

			var canvasChildren = canvas.Children;
			canvasChildren.Clear();

			var actualLocation = _startLocation;
			int index = 0;
			foreach (var destination in _AddEndLocation(bestSolutionSoFar))
			{
				int red = 255 * index / bestSolutionSoFar.Length;
				int blue = 255 - red;

				var line = new Line();
				var color = Color.FromRgb((byte)red, 0, (byte)blue);
				line.Stroke = new SolidColorBrush(color);
				line.X1 = actualLocation.X;
				line.Y1 = actualLocation.Y;
				line.X2 = destination.X;
				line.Y2 = destination.Y;
				canvasChildren.Add(line);

				var circle = new Ellipse();
				circle.Stroke = Brushes.Black;

				if (destination == _startLocation)
					circle.Fill = Brushes.Red;
				else
					circle.Fill = Brushes.Yellow;

				circle.Width = 11;
				circle.Height = 11;
				Canvas.SetLeft(circle, destination.X - 5);
				Canvas.SetTop(circle, destination.Y - 5);
				Panel.SetZIndex(circle, 57);
				canvasChildren.Add(circle);

				actualLocation = destination;
				index++;
			}
		}

		private IEnumerable<Location> _AddEndLocation(Location[] middleLocations)
		{
			foreach(var location in middleLocations)
				yield return location;

			yield return _startLocation;
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			_closing = true;

			lock(_lock)
				Monitor.Pulse(_lock);
		}
	}
}
