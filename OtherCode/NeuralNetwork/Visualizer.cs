using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using NeuralNetwork;

namespace NeuralNetwork
{
	public class Visualizer : Window {
		private Canvas canvas;
		//private Path path;
		//private PathGeometry pathGeometry;
		private Matrix matrix;
		private MatrixTransform matrixTransform;

		private Point lastPos;

		public new const int Margin = 10;
		public const int NeuronSize = 50;
		public const int SpacingX = 20;
		public const int SpacingY = 200;

		public const int WindowWidth = 800;
		public const int WindowHeight = 600;

		private Network network;

		private List<InfoUnit> infoUnits;

		private int i, j;

		private bool running = false;
		public bool Running { get { return running; } }
		private int lastStep = 1;
		public int Step = 0;
		public double Jitter = 0.0;

		private System.Windows.Controls.Button stepButton;
		private System.Windows.Controls.TextBox stepBox;
		private System.Windows.Controls.TextBlock resultsTextBlock;
		private System.Windows.Controls.TextBlock totalTextBlock;
		private System.Windows.Controls.TextBox jitterBox;

		public string Results = "";
		public int Total = 0;

		public Visualizer(Network network)
		{
			this.Width = WindowWidth;
			this.Height = WindowHeight;
			this.Show();
			this.Background = Brushes.Black;
			this.network = network;

			Grid grid = new Grid();
			grid.Margin = new Thickness(10, 10, 10, 10);

			RowDefinition rd = new RowDefinition();
			rd.Height = new GridLength(25);
			grid.RowDefinitions.Add(rd);
			rd = new RowDefinition();
			rd.Height = new GridLength(10);
			grid.RowDefinitions.Add(rd);
			rd = new RowDefinition();
			rd.Height = new GridLength(0.9,GridUnitType.Star);
			grid.RowDefinitions.Add(rd);

			ColumnDefinition cd = new ColumnDefinition();
			cd.Width = new GridLength(80);
			grid.ColumnDefinitions.Add(cd);
			cd = new ColumnDefinition();
			cd.Width = new GridLength(10);
			grid.ColumnDefinitions.Add(cd);
			cd = new ColumnDefinition();
			cd.Width = new GridLength(80);
			grid.ColumnDefinitions.Add(cd);
			cd = new ColumnDefinition();
			cd.Width = new GridLength(10);
			grid.ColumnDefinitions.Add(cd);
			cd = new ColumnDefinition();
			cd.Width = new GridLength(80);
			grid.ColumnDefinitions.Add(cd);
			cd = new ColumnDefinition();
			cd.Width = new GridLength(10);
			grid.ColumnDefinitions.Add(cd);
			cd = new ColumnDefinition();
			cd.Width = new GridLength(150);
			grid.ColumnDefinitions.Add(cd);
			cd = new ColumnDefinition();
			cd.Width = new GridLength(10);
			grid.ColumnDefinitions.Add(cd);
			cd = new ColumnDefinition();
			cd.Width = new GridLength(80);
			grid.ColumnDefinitions.Add(cd);
			cd = new ColumnDefinition();
			cd.Width = new GridLength(10);
			grid.ColumnDefinitions.Add(cd);
			cd = new ColumnDefinition();
			cd.Width = new GridLength(80);
			grid.ColumnDefinitions.Add(cd);

			System.Windows.Controls.Button b = new System.Windows.Controls.Button();
			b.Content = "Start";
			b.Click += new RoutedEventHandler(runClick);
			Grid.SetColumn(b, 0);
			grid.Children.Add(b);

			stepButton = new System.Windows.Controls.Button();
			stepButton.Content = "Step";
			stepButton.Click += new RoutedEventHandler(stepClick);
			Grid.SetColumn(stepButton, 2);
			grid.Children.Add(stepButton);

			stepBox = new System.Windows.Controls.TextBox();
			stepBox.Text = 1+"";
			Grid.SetColumn(stepBox,4);
			grid.Children.Add(stepBox);

			totalTextBlock = new TextBlock();
			totalTextBlock.FontSize = 12.0f;
			totalTextBlock.Foreground = Brushes.White;
			Grid.SetColumn(totalTextBlock, 6);
			grid.Children.Add(totalTextBlock);

			resultsTextBlock = new TextBlock();
			resultsTextBlock.FontSize = 12.0f;
			resultsTextBlock.Foreground = Brushes.White;
			Grid.SetRow(resultsTextBlock, 2);
			grid.Children.Add(resultsTextBlock);

			b = new System.Windows.Controls.Button();
			b.Content = "Jitter";
			b.Click += new RoutedEventHandler(jitterClick);
			Grid.SetColumn(b, 8);
			grid.Children.Add(b);

			jitterBox = new System.Windows.Controls.TextBox();
			jitterBox.Text = "0.1";
			Grid.SetColumn(jitterBox, 10);
			grid.Children.Add(jitterBox);


			canvas = new Canvas();
			grid.Children.Add(canvas);
			this.Content = grid;

			this.MouseWheel += new MouseWheelEventHandler(zoomHandler);
			this.MouseDown += new MouseButtonEventHandler(mouseDownHandler);
			this.MouseMove += new System.Windows.Input.MouseEventHandler(mouseMoveHandler);
			this.MouseUp += new MouseButtonEventHandler(mouseUpHandler);


			infoUnits = new List<InfoUnit>();
			for( i = 0; i < network.NeuronLayers.Length; i++ ) {
				for( j = 0; j < network.NeuronLayers[i].Length; j++ ) {
					infoUnits.Add(new InfoUnit(network, network.NeuronLayers[i][j], i, canvas));
				}
			}

			matrix = new Matrix();
			matrix.Translate(0, -SpacingY);
			matrix.ScaleAt(0.6, 0.6, WindowWidth / 2, 0);
			matrixTransform = new MatrixTransform(matrix);
			canvas.RenderTransform = matrixTransform;

			update();
		}

		public void Update() {
			this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, new System.Threading.ThreadStart(delegate { update(); }));
		}

		private void update() {
			// update results
			resultsTextBlock.Text = Results;
			// update totals
			totalTextBlock.Text = Total + " iterations";
			// update stepping
			if( Step > 0 ) {
				stepBox.Text = Step + "";
			} else {
				stepBox.Text = lastStep + "";
			}
			// update graph
			foreach( InfoUnit unit in infoUnits ) {
				unit.Update();
			}
		}

		private void jitterClick(object sender, RoutedEventArgs e) {
			try {
				Jitter = Double.Parse(jitterBox.Text);
			} catch( FormatException fe ) {
				jitterBox.Text = "0.1";
				Console.WriteLine(fe.Message);
			}
		}

		private void stepClick(object sender, RoutedEventArgs e) {
			try{
				Step = Int32.Parse(stepBox.Text);
				lastStep = Step;
			} catch{
				stepBox.Text = lastStep+"";
			}
		}

		private void runClick(object sender, RoutedEventArgs e) {
			running = !running;
			if( running ) {
				((System.Windows.Controls.Button)sender).Content = "Stop";
				stepButton.IsEnabled = false;
			} else {
				((System.Windows.Controls.Button)sender).Content = "Start";
				stepButton.IsEnabled = true;
			}
		}

		private class InfoUnit
		{
			private Neuron neuron;
			private TextBlock activationText = new TextBlock();
			private Path boxPath = new Path();
			private Path[] weightPaths;
			private double[] lastWeightValues;
			private double[] weightChanges;

			private double lastActivationValue;
			private double activationChange = 0.0;

			public InfoUnit(Network network, Neuron neuron, int layer, Canvas canvas) {
				this.neuron = neuron;
				PathGeometry geom;
				// set values
				lastActivationValue = neuron.ActivationValue;
				// get position
				Point pos = DrawPoint(layer, neuron.Index, network);
				// set up box
				geom = new PathGeometry();
				geom.AddGeometry(new RectangleGeometry(new Rect(pos.X, pos.Y, Visualizer.NeuronSize, Visualizer.NeuronSize), 3, 3));
				boxPath.Data = geom;
				boxPath.StrokeThickness = 3.0f;
				canvas.Children.Add(boxPath);
				// set up text
				activationText = new TextBlock();
				activationText.FontSize = 16.0f;
				activationText.TextAlignment = TextAlignment.Center;
				activationText.Width = Visualizer.NeuronSize;
				activationText.Height = Visualizer.NeuronSize;
				activationText.Foreground = Brushes.White;
				Canvas.SetTop(activationText, Visualizer.BottomPoint(layer, neuron.Index, network).Y);
				Canvas.SetLeft(activationText, Visualizer.Center(layer, neuron.Index, network).X - Visualizer.NeuronSize / 2);
				canvas.Children.Add(activationText);
				// set up weights
				if( layer > 0 ) {
					weightPaths = new Path[neuron.Weights.Length];
					lastWeightValues = new double[neuron.Weights.Length];
					weightChanges = new double[neuron.Weights.Length];
					for( int i = 0; i < neuron.Weights.Length; i++ ) {
						weightChanges[i] = 0.0;
						lastWeightValues[i] = neuron.Weights[i];
						geom = new PathGeometry();
						geom.AddGeometry(new LineGeometry(Visualizer.TopPoint(layer, neuron.Index, network), Visualizer.BottomPoint(layer - 1, i, network)));
						weightPaths[i] = new Path();
						weightPaths[i].Data = geom;
						weightPaths[i].StrokeThickness = 1.0f;
						canvas.Children.Add(weightPaths[i]);
					}
				} else {
					weightPaths = new Path[0];
				}
			}

			public void Update() {
				// update text
				activationText.Text = (Math.Round(neuron.ActivationValue * 1000) / 1000.0) + "";
				// update change in activation value
				double activationDiff = Math.Abs(neuron.ActivationValue - lastActivationValue);
				activationChange /= 1.01;
				if( Double.IsNaN(activationDiff) ) {
					activationDiff = 0.0;
				}
				activationChange += activationDiff * 5;
				if( activationChange > 1.0 ) activationChange = 1.0;
				if( activationChange < 0.0 ) activationChange = 0.0;
				lastActivationValue = neuron.ActivationValue;
				boxPath.StrokeThickness = activationChange * 4 + 0.6;
				boxPath.Stroke = new SolidColorBrush(Color.FromRgb((byte)Math.Floor(activationChange * 255), 0, (byte)Math.Ceiling(255 - (activationChange * 255))));
				// update change in weights
				for( int i = 0; i < weightPaths.Length; i++ ) {
					double weightDiff = Math.Abs(neuron.Weights[i] - lastWeightValues[i]);
					weightChanges[i] /= 1.01;
					if( Double.IsNaN(weightDiff) ) {
						weightDiff = 0.0;
					}
					weightChanges[i] += weightDiff * 4;
					if( weightChanges[i] > 1.0 ) weightChanges[i] = 1.0;
					if( weightChanges[i] < 0.0 ) weightChanges[i] = 0.0;
					lastWeightValues[i] = neuron.Weights[i];
					weightPaths[i].StrokeThickness = weightChanges[i] * 2 + 0.2;
					weightPaths[i].Stroke = new SolidColorBrush(Color.FromRgb((byte)Math.Floor(weightChanges[i] * 255), 0, (byte)Math.Ceiling(255 - (weightChanges[i] * 255))));
				}
			}
		}

		public void zoomHandler( object sender, MouseWheelEventArgs args ) {
			Point mousePos = args.GetPosition(this);
			Point mouseScreenPercentage = new Point(mousePos.X / this.Width, mousePos.Y / this.Height);
			double delta = args.Delta / 1200.0;
			matrix.Scale(1.0 + delta, 1.0 + delta);
			matrix.Translate(-mouseScreenPercentage.X * this.Width * delta, -mouseScreenPercentage.Y * this.Height * delta);
			matrixTransform.Matrix = matrix;		
		}

		public void mouseDownHandler( object sender, System.Windows.Input.MouseEventArgs args ) {
			if( args.GetPosition(this).Y < 35 ) {
				return;
			}
			lastPos = args.GetPosition(this);
		}

		public void mouseUpHandler( object sender, System.Windows.Input.MouseEventArgs args ) {
			
		}

		public void mouseMoveHandler( object sender, System.Windows.Input.MouseEventArgs args ) {
			if( args.GetPosition(this).Y < 35 ) {
				return;
			}
			if( args.LeftButton == MouseButtonState.Pressed ) {
				Point curPos = args.GetPosition(this);
				matrix.Translate(curPos.X - lastPos.X, curPos.Y - lastPos.Y);
				matrixTransform.Matrix = matrix;
				lastPos = curPos;
			}
		}

		public static Point Center(int layer, int index, Network network) {
			return new Point((WindowWidth/ 2) + (index * (SpacingX + NeuronSize)) - (CalcWidth(network.NeuronLayers[layer].Length - 1) / 2), (network.NeuronLayers.Length - layer) * (NeuronSize + SpacingY) + Margin + (NeuronSize / 2));
		}

		public static Point TopPoint(int layer, int index, Network network) {
			Point t = Center(layer, index, network);
			t.Y += NeuronSize / 2;
			return t;
		}

		public static Point BottomPoint(int layer, int index, Network network) {
			Point b = Center(layer, index, network);
			b.Y -= NeuronSize / 2;
			return b;
		}

		public static Point DrawPoint(int layer, int index, Network network) {
			Point c = Center(layer, index, network);
			c.X -= NeuronSize / 2;
			c.Y -= NeuronSize / 2;
			return c;
		}

		public static int CalcWidth(int neurons) {
			return neurons * (NeuronSize + SpacingX) + Margin * 2;
		}

	/*	public static int CalcHeight() {
			return network.NeuronLayers.Length * (SpacingY + NeuronSize) + Margin * 2 - NeuronSize + 40;
		}*/

	}
}
