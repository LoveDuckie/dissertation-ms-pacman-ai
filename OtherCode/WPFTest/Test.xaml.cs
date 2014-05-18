using System;
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

namespace WPFTest
{
	public partial class Test
	{
		private Canvas canvas;
		private Path path;
		private PathGeometry pathGeometry;
		private Matrix matrix;
		private MatrixTransform matrixTransform;

		private Point lastPos;

		private const int margin = 10;
		private const int neuronSize = 50;
		private const int xSpacing = 20;
		private const int ySpacing = 100;

		Network network;

		private readonly int maxNeurons = 0;
		private int i, j, k;

		public Test()
		{			
			this.InitializeComponent();
			canvas = new Canvas();
			//Grid canvas = new Grid();
			this.Content = canvas;


			this.MouseWheel += new MouseWheelEventHandler(zoomHandler);
			this.MouseDown += new MouseButtonEventHandler(mouseDownHandler);
			this.MouseMove += new System.Windows.Input.MouseEventHandler(mouseMoveHandler);
			this.MouseUp += new MouseButtonEventHandler(mouseUpHandler);

			
			matrix = new Matrix();
			matrixTransform = new MatrixTransform(matrix);

			//pathGeometry.Transform = matrixTransform;
			canvas.RenderTransform = matrixTransform;

			TextBlock t = new TextBlock();
			t.FontSize = 30.0f;
			t.Text = "Cool ... !";
						
			canvas.Children.Add(t);

			path = new Path();			
			pathGeometry = new PathGeometry();

			network = new Network(3, 2, 10, 5);
			for( i = 0; i < network.NeuronLayers.Length; i++ ) {
				if( network.NeuronLayers[i].Length > maxNeurons ) {
					maxNeurons = network.NeuronLayers[i].Length;
				}
			}
			// redraw network
			for( i = 0; i < network.NeuronLayers.Length; i++ ) {
				for( j = 0; j < network.NeuronLayers[i].Length; j++ ) {
					Point pos = drawPoint(i, j);

					pathGeometry.AddGeometry(new RectangleGeometry(new Rect(pos.X, pos.Y, neuronSize, neuronSize), 3, 3));
					//gfx.DrawRectangle(new Pen(Brushes.Black, 2.0f), pos.X, pos.Y, neuronSize, neuronSize);
					if( i > 0 ) {
						for( k = 0; k < network.NeuronLayers[i - 1].Length; k++ ) {
							pathGeometry.AddGeometry(new LineGeometry(top(i, j), bottom(i - 1, k)));
							//gfx.DrawLine(new Pen(Brushes.Black, 1.0f), top(i, j), bottom(i - 1, k));
						}
					}
				}
			}
			
			
			path.Stroke = Brushes.Yellow;
			path.StrokeThickness = 2.0f;
			path.Data = pathGeometry;			
			canvas.Children.Add(path);

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
			lastPos = args.GetPosition(this);
		}

		public void mouseUpHandler( object sender, System.Windows.Input.MouseEventArgs args ) {
			
		}

		public void mouseMoveHandler( object sender, System.Windows.Input.MouseEventArgs args ) {
			if( args.LeftButton == MouseButtonState.Pressed ) {
				Point curPos = args.GetPosition(this);
				matrix.Translate(curPos.X - lastPos.X, curPos.Y - lastPos.Y);
				matrixTransform.Matrix = matrix;
				lastPos = curPos;
			}
		}

		private Point center( int layer, int index ) {
			return new Point((this.Width / 2) + (index * (xSpacing + neuronSize)) - (calcWidth(network.NeuronLayers[layer].Length - 1) / 2), layer * (neuronSize + ySpacing) + margin + (neuronSize / 2));
		}

		private Point top( int layer, int index ) {
			Point t = center(layer, index);
			t.Y -= neuronSize / 2;
			return t;
		}

		private Point bottom( int layer, int index ) {
			Point b = center(layer, index);
			b.Y += neuronSize / 2;
			return b;
		}

		private Point drawPoint( int layer, int index ) {
			Point c = center(layer, index);
			c.X -= neuronSize / 2;
			c.Y -= neuronSize / 2;
			return c;
		}

		private int calcWidth( int neurons ) {
			return neurons * (neuronSize + xSpacing) + margin * 2;
		}

		private int calcHeight() {
			return network.NeuronLayers.Length * (ySpacing + neuronSize) + margin * 2 - neuronSize + 40;
		}
	}
}
