using System;
using System.Collections.Generic;
using System.Text;

namespace NeuralNetwork
{
	[Serializable]
	public class Neuron
	{
		public int NumInputs;
		public int NumOutputs;

		public int Index;
		
		public Neuron[] OutNeurons;

		public double[] Weights;
		public double ActivationValue;
		public double ErrorValue;
		public double OutputValue;

		public double[] InputValues;		
		
		private int i;
		private static Random rand = new Random();
		private Network network;

		public Neuron( Network network, int numInputs, int numOutputs, int index ){
			this.network = network;
			this.Index = index;
			this.NumInputs = numInputs;
			this.NumOutputs = numOutputs;			
			InputValues = new double[numInputs];
			Weights = new double[numInputs];
			Reset();
		}
		
		public void Reset(){
			for( i = 0; i < Weights.Length; i++ ) {
				Weights[i] = getRand();
			}
			ActivationValue = getRand();
		}
		
		public void Propagate(){
			GetOutput();
			for( i = 0; i < OutNeurons.Length; i++ ) {
				OutNeurons[i].InputValues[Index] = OutputValue;
			}
		}

		public double GetOutput() {
			if( ActivationValue == double.NegativeInfinity ) {
				OutputValue = InputValues[0];
			} else {
				double summation = 0.0;
				for( i = 0; i < InputValues.Length; i++ ) {
					// calculate output values
					summation += InputValues[i] * Weights[i];
				}
				OutputValue = activate(summation + ActivationValue);
			}
			return OutputValue;
		}

		private double activate( double value ) {			
			switch( network.ActivationFunction ) {
				case ActivationType.Sigmoid:
					value = Math.Exp(-value);
					return 1.0 / (1 + value);
				case ActivationType.Linear:
					return value;
				case ActivationType.Logarithm:
					return Math.Log(1 + Math.Abs(value));
				case ActivationType.Sine:
					return Math.Sin(value);
				case ActivationType.Tanh:
					return Math.Tanh(value);
				default:
					throw new Exception("Activation function was not recognized");
			}
		}

		public double ActivateDerivative( double value ) {
			switch( network.ActivationFunction ) {
				case ActivationType.Sigmoid:
					return (value * (1 - value));
				case ActivationType.Linear:
					return 1;
				case ActivationType.Logarithm:
					return (1 / Math.Exp(value));
				case ActivationType.Sine:
					return Math.Sqrt(1 - value * value);
				case ActivationType.Tanh:
					return (1 - value * value);
				default:
					throw new Exception("Activation function was not recognized");
			}
		}

		private double getRand() {
			return (double)rand.NextDouble() * 2.0 - 1.0; 
		}

		internal void Jitter( double maxChange, bool changeActivationValue ) {
			double change;
			if( changeActivationValue ) {
				change = (double)rand.NextDouble() * maxChange - (maxChange / 2.0);
				ActivationValue = clip(ActivationValue + change);
			}
			for( i = 0; i < Weights.Length; i++ ) {
				change = (double)rand.NextDouble() * maxChange - (maxChange / 2.0);
				Weights[i] = clip(Weights[i] + change);
			}
		}

		private double clip(double d) {
			if( d > 1.0 ) return 1.0;
			if( d < -1.0 ) return -1.0;
			return d;
		}
	}
}
