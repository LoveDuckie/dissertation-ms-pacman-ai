using System;
using System.Collections.Generic;
using System.Text;

namespace NeuralNetwork
{	
	// specialized to PacMan
	[Serializable]
	public class QLearning
	{
		public readonly Network Network;
		private List<State> history = new List<State>();

		public double DiscountFactor;
		public double BestActionProb;

		private int i;
		private Random rand = new Random();

		public QLearning( Network network ) : this(network, 0.7, 0.8) { }

		public QLearning( Network network, double discountFactor, double bestActionProb ) {
			this.Network = network;			
			this.DiscountFactor = discountFactor;
			this.BestActionProb = bestActionProb;
		}

		public int GetAction( double[] inputs ) {
			double[] outputs = Network.GetOutputs(inputs);
			history.Add(new State(inputs, outputs));
			if( history.Count > 20 ) {
				history.RemoveAt(history.Count - 1);
			}
			// select best
			if( rand.NextDouble() < BestActionProb ) {
				double bestValue = outputs[0];
				int bestAction = 0;
				for( i = 1; i < outputs.Length; i++ ) {
					if( outputs[i] > bestValue ) {
						bestAction = i;
					}
				}
				return bestAction;
			}
			// select random
			else {
				return rand.Next(0, outputs.Length - 1);
			}
		}

		public double[] GetOutput(double[] inputs) {
			double[] outputs = Network.GetOutputs(inputs);
			history.Add(new State(inputs, outputs));
			if( history.Count > 20 ) {
				history.RemoveAt(history.Count - 1);
			}
			return outputs;
		}

		public void GiveReward( double reward ) {			
			for( i = 0; i < history.Count; i++ ) {
				if( reward > 0.0 ) {
					Network.Train(history[i].Inputs, history[i].Outputs, reward);
				} else {
					Network.Train(history[i].Inputs, history[i].InverseOutputs, -reward);
				}
				reward *= DiscountFactor;
			}			
		}

		public void ClearHistory() {
			history.Clear();
		}

		[Serializable]
		private class State
		{
			public readonly double[] Inputs;
			public readonly double[] Outputs;
			public readonly double[] InverseOutputs;
			public readonly double Reward;

			public State( double[] inputs, double[] outputs ) : this(inputs, outputs, 0.0) { }

			public State( double[] inputs, double[] outputs, double reward ) {
				this.Inputs = clone(inputs);
				this.Outputs = clone(outputs);
				this.InverseOutputs = cloneInverse(outputs);
				this.Reward = reward;
			}

			private double[] clone( double[] array ) {
				double[] clone = new double[array.Length];
				for( int i = 0; i < array.Length; i++ ) {
					clone[i] = array[i];
				}
				return clone;
			}

			private double[] cloneInverse( double[] array ) {
				double[] clone = new double[array.Length];
				for( int i = 0; i < array.Length; i++ ) {
					clone[i] = 1.0 - array[i];
				}
				return clone;
			}
		}
	}
}
