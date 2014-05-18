using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NeuralNetwork
{
    public static class Importer
    {
		public static List<TrainingSet> Load(string path) {
			List<TrainingSet> sets = new List<TrainingSet>();
			int totalVariables = -1;
			using( StreamReader sr = new StreamReader(File.OpenRead(path)) ) {
				while( !sr.EndOfStream ) {
					string[] line = sr.ReadLine().Split('\t');
					if( totalVariables == -1 ) {
						totalVariables = line.Length;
					} else if( line.Length != totalVariables ) {
						continue;
					}
					double[] inputs = new double[totalVariables-1];
					double[] outputs = new double[1];
					for( int i = 0; i < line.Length - 1; i++ ) {
						inputs[i] = Double.Parse(line[i]);
					}
					outputs[0] = Double.Parse(line[line.Length-1]);
					sets.Add(new TrainingSet(inputs,outputs));
				}
			}
			return sets;
		}

		public static List<List<TrainingSet>> LoadSectorData(string path) {
			List<List<TrainingSet>> sets = new List<List<TrainingSet>>();
			for( int i = 0; i < 6; i++ ) {
				sets.Add(new List<TrainingSet>());
			}
			using( StreamReader sr = new StreamReader(File.OpenRead(path)) ) {
				while( !sr.EndOfStream ) {
					string[] line = sr.ReadLine().Split(';');
					int sector = Int16.Parse(line[0]);
					string[] sInputs = { line[3], line[4], line[5], line[6] };
					string[] sOutputs = { line[1] };
					double[] inputs = new double[sInputs.Length];
					double[] outputs = new double[sOutputs.Length];
					for( int i = 0; i < sInputs.Length; i++ ) {
						inputs[i] = Int16.Parse(sInputs[i]) / 40.0;
					}
					for( int i = 0; i < sOutputs.Length; i++ ) {
						outputs[i] = Int16.Parse(sOutputs[i]) / 10.0;
					}
					sets[sector].Add(new TrainingSet(inputs, outputs));
				}
			}
			return sets;
		}

		public static List<TrainingSet> LoadDangerData(string path) {
			List<TrainingSet> sets = new List<TrainingSet>();
			using( StreamReader sr = new StreamReader(File.OpenRead(path)) ) {
				while( !sr.EndOfStream ) {
					string[] line = sr.ReadLine().Split(';');
					string direction = line[0];
					string[] sInputs = { line[2], line[3], line[4], line[5], line[6], line[7], line[8], line[9] };
					string[] sOutputs = { line[1] };
					double[] inputs = new double[sInputs.Length];
					double[] outputs = new double[sOutputs.Length];
					for( int i = 0; i < sInputs.Length; i++ ) {
						if( i % 2 == 0 ) {
							double angle = double.Parse(sInputs[i]);
							if( direction == "Right" ) {
								angle += 0.25;
							} else if( direction == "Left" ) {
								angle -= 0.25;
							} else if( direction == "Down" ) {
								angle -= 0.5;
							}
							if( angle < 0.0 ) {
								angle = 1.0 + angle;
							}
							if( angle > 1.0 ) {
								angle -= 1.0;
							}
							inputs[i] = angle;
						} else {
							inputs[i] = Int16.Parse(sInputs[i]) / 40.0;
						}
					}
					for( int i = 0; i < sOutputs.Length; i++ ) {
						outputs[i] = Int16.Parse(sOutputs[i]) / 10.0;
					}
					sets.Add(new TrainingSet(inputs, outputs));
				}
			}
			return sets;
		}

		public static List<TrainingSet> LoadPredictionDangerData(string path) {
			List<TrainingSet> sets = new List<TrainingSet>();
			using( StreamReader sr = new StreamReader(File.OpenRead(path)) ) {
				while( !sr.EndOfStream ) {
					string[] line = sr.ReadLine().Split(';');
					string direction = line[0];
					string[] sInputs = { line[2], line[3], line[4] };
					string[] sOutputs = { line[1] };
					double[] inputs = new double[sInputs.Length];
					double[] outputs = new double[sOutputs.Length];
					for( int i = 0; i < sInputs.Length; i++ ) {
						inputs[i] = double.Parse(sInputs[i]);
					}
					for( int i = 0; i < sOutputs.Length; i++ ) {
						outputs[i] = Int16.Parse(sOutputs[i]) / 10.0;
					}
					sets.Add(new TrainingSet(inputs, outputs));
				}
			}
			return sets;
		}
    }

	public class TrainingSet
	{
		public readonly double[] Inputs;
		public readonly double[] Outputs;

		public TrainingSet(double[] inputs, double[] outputs) {
			this.Inputs = inputs;
			this.Outputs = outputs;
		}
	}
}
