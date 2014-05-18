using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

namespace NeuralNetwork
{
	public enum ActivationType { Sigmoid, Linear, Logarithm, Sine, Tanh };

	[Serializable]
	public class Network
	{
		public ActivationType ActivationFunction;

		private double learningRate = 0.1;
		public double LearningRate {
			get { return learningRate; }
			set { learningRate = value; if( learningRate < 0.0 ) learningRate = 0.0; if( learningRate > 1.0 ) learningRate = 1.0; }
		}
		
		public int NumInputs;
		public int NumOutputs;
		public int NumHiddenNeurons;
		public int NumHiddenLayers;
		public int TotalLayers;
		
		public double[] Inputs;
		public double[] Outputs;

		private readonly Neuron[] inputNeurons;
		private readonly Neuron[] outputNeurons;
		public readonly Neuron[][] NeuronLayers;
				
		private int i, j, k;

		public Network(int numInputs, int numOutputs, int numHiddenNeurons)
			: this(numInputs, numOutputs, numHiddenNeurons, 1, ActivationType.Sigmoid) {}

		public Network( int numInputs, int numOutputs, int numHiddenNeurons, ActivationType activationFunction ) : 
			this(numInputs, numOutputs, numHiddenNeurons, 1, activationFunction) {}

		public Network( int numInputs, int numOutputs, int numHiddenNeurons, int numHiddenLayers )
			: this(numInputs, numOutputs, numHiddenNeurons, numHiddenLayers, ActivationType.Sigmoid) { }

		public Network( int numInputs, int numOutputs, int numHiddenNeurons, int numHiddenLayers, ActivationType activationFunction ){
			if( numHiddenLayers < 1 ){
				throw new ApplicationException("There must be at least one hidden layer");
			}
			this.Inputs = new double[numInputs];
			this.Outputs = new double[numOutputs];
			this.NumInputs = numInputs;
			this.NumOutputs = numOutputs;			
			this.NumHiddenNeurons = numHiddenNeurons;
			this.NumHiddenLayers = numHiddenLayers;
			this.TotalLayers = 2 + numHiddenLayers;
			// initialize neurons
			NeuronLayers = new Neuron[TotalLayers][];
			inputNeurons = new Neuron[numInputs];
			for( i = 0; i < numInputs; i++ ){
				inputNeurons[i] = new Neuron(this,1,numHiddenNeurons,i);				
				inputNeurons[i].ActivationValue = double.NegativeInfinity;
				inputNeurons[i].Weights[0] = 1.0;				
			}
			NeuronLayers[0] = inputNeurons;
			for( i = 0; i < numHiddenLayers; i++ ){
				NeuronLayers[i+1] = new Neuron[numHiddenNeurons];
				// hidden -> hidden
				int inputs = numHiddenNeurons;
				int outputs = numHiddenNeurons;
				// input -> hidden
				if( i == 0 ){ 
					inputs = numInputs;
				}
				// hidden -> output
				if( i == numHiddenLayers - 1 ){
					outputs = numOutputs;
				}
				for( j = 0; j < numHiddenNeurons; j++ ){
					NeuronLayers[i + 1][j] = new Neuron(this, inputs, outputs, j);
				}
			}
			outputNeurons = new Neuron[numOutputs];
			for( i = 0; i < numOutputs; i++ ){
				outputNeurons[i] = new Neuron(this, numHiddenNeurons, 1, i);
				outputNeurons[i].ActivationValue = 0.0;
			}
			NeuronLayers[TotalLayers-1] = outputNeurons;
			// connect layers
			for( i = 0; i < NeuronLayers.Length - 1; i++ ){
				for( j = 0; j < NeuronLayers[i].Length; j++ ) {
					NeuronLayers[i][j].OutNeurons = NeuronLayers[i+1];
				}
			}
		}

		public void Train( double[] inputs, double[] targets ){
			Train(inputs, targets, double.NaN);
		}
					
		public void Train( double[] inputs, double[] targets, double learningRate ){
			if( learningRate == double.NaN ) {
				learningRate = LearningRate;
			}
			// calculate output
			GetOutputs( inputs );					
			// calculate output error
			for( i = 0; i < Outputs.Length; i++ ){
				outputNeurons[i].ErrorValue = targets[i] - Outputs[i];
				outputNeurons[i].ErrorValue *= outputNeurons[i].ActivateDerivative(outputNeurons[i].OutputValue);
				//Console.WriteLine(outputNeurons[i].OutputValue + " : " + targets[i] + " error: " + outputNeurons[i].ErrorValue);
			}
			// calculate error in hidden layers
			double errorSum;
			for( i = TotalLayers - 2; i > 0; i-- ){
				for( j = 0; j < NeuronLayers[i].Length; j++ ) {
					errorSum  = 0.0;
					for( k = 0; k < NeuronLayers[i + 1].Length; k++ ) {
						errorSum += NeuronLayers[i+1][k].ErrorValue * NeuronLayers[i+1][k].Weights[j];
					}
					NeuronLayers[i][j].ErrorValue = errorSum * NeuronLayers[i][j].ActivateDerivative(NeuronLayers[i][j].OutputValue);
					//Console.WriteLine("hidden " + j + ": " + neuronLayers[i][j].OutputValue + " error: " + neuronLayers[i][j].ErrorValue);
				}
			}
			// adjust weights and activation values
			for( i = TotalLayers - 2; i >= 0; i-- ){
				for( j = 0; j < NeuronLayers[i].Length; j++ ) {
					//Console.WriteLine("train hidden " + j + " act: " + neuronLayers[i][j].ActivationValue + " + " + (LearningRate * neuronLayers[i][j].ErrorValue));
					NeuronLayers[i][j].ActivationValue += LearningRate * NeuronLayers[i][j].ErrorValue;
					for( k = 0; k < NeuronLayers[i+1].Length; k++ ) {
						NeuronLayers[i + 1][k].Weights[j] += LearningRate * NeuronLayers[i][j].OutputValue * NeuronLayers[i + 1][k].ErrorValue;
					}
				}
			}
		}

		public void TrainMultiple(double[][] inputs, double[][] targets) {
			if( inputs.Length != targets.Length ) {
				throw new ApplicationException("TrainMultiple: input and target arrays must have same length");
			}
			for( i = 0; i < inputs.Length; i++ ) {
				Train( inputs[i], targets[i] );
			}
		}
		
		public double[] GetOutputs( double[] inputs ){
			if( inputs != null ){
				this.Inputs = inputs;
			}
			// inputs
			for( j = 0; j < NeuronLayers[0].Length; j++ ) {
				NeuronLayers[0][j].InputValues[0] = Inputs[j];
			}
			// input -> hidden -> output
			for( i = 0; i < NeuronLayers.Length - 1; i++ ) {
				for( j = 0; j < NeuronLayers[i].Length; j++ ) {
					NeuronLayers[i][j].Propagate();
				}
			}
			// outputs
			for( j = 0; j < NeuronLayers[TotalLayers-1].Length; j++ ) {
				Outputs[j] = NeuronLayers[TotalLayers-1][j].GetOutput();
			}
			return Outputs;
		}

		public void Jitter( double maxChange ) {
			for( i = 0; i < NeuronLayers.Length; i++ ) {
				for( j = 0; j < NeuronLayers[i].Length; j++ ) {
					bool changeActivationValue = true;
					if( i == 0 || i == TotalLayers - 1 ) {
						changeActivationValue = false;
					}
					NeuronLayers[i][j].Jitter(maxChange,changeActivationValue);
				}
			}
		}

		public void Reset() {
			// initialize neurons
			for( i = 0; i < NeuronLayers.Length; i++ ) {
				for( j = 0; j < NeuronLayers[i].Length; j++ ) {
					NeuronLayers[i][j].Reset();
				}
			}
		}

		public static Network Load(string path) {
			using( Stream file = File.OpenRead(path) ) {
				BinaryFormatter bf = new BinaryFormatter();
				return (Network)bf.Deserialize(file);
			}
		}

		public void Save(string path) {
			using( Stream file = File.OpenWrite(path) ) {
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(file,this);
			}
		}		
				
		public void SaveXml( string path ) {
			using( System.Xml.XmlWriter writer = System.Xml.XmlWriter.Create(File.Open(path, FileMode.Create)) ) {
				// start document
				writer.WriteStartDocument();
				writer.WriteStartElement("Network");
				// write attributes
				writer.WriteAttributeString("Layers", TotalLayers + "");
				writer.WriteAttributeString("HiddenLayers", NumHiddenLayers + "");
				writer.WriteAttributeString("NumInputs", NumInputs + "");
				writer.WriteAttributeString("NumOutputs", NumOutputs + "");
				writer.WriteAttributeString("NumHiddenNeurons", NumHiddenNeurons + "");
				writer.WriteAttributeString("ActivationFunction", ActivationFunction + "");
				writer.WriteAttributeString("LearningRate", LearningRate + "");
				// write layers
				for( i = 0; i < NeuronLayers.Length; i++ ) {
					writer.WriteStartElement("Layer");
					if( i == 0 ) {
						writer.WriteAttributeString("Type", "Input");
					} else if( i == NeuronLayers.Length - 1 ) {
						writer.WriteAttributeString("Type", "Output");
					} else {
						writer.WriteAttributeString("Type", "Hidden");
					}
					writer.WriteAttributeString("Nodes", NeuronLayers[i].Length + "");					
					for( j = 0; j < NeuronLayers[i].Length; j++ ) {
						writer.WriteStartElement("Node");
						writer.WriteAttributeString("ActivationValue", NeuronLayers[i][j].ActivationValue + "");
						for( k = 0; k < NeuronLayers[i][j].Weights.Length; k++ ) {
							writer.WriteStartElement("Weight");
							writer.WriteAttributeString("Index", k + "");
							writer.WriteAttributeString("Value", NeuronLayers[i][j].Weights[k] + "");
							writer.WriteEndElement();
						}
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
				}
				// end document
				writer.WriteEndElement();
				writer.WriteEndDocument();
			}		
		}

		// maps a value to the range 0.0 - 1.0
		public static double Map( double value, double min, double max ) {
			return (value - min) / (max - min);
		}	
	}
}
