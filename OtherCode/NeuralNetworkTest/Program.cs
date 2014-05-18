using System;
using System.Collections.Generic;
using System.Text;
/*
 * From NeuralPredictionPac
 * Network network = new Network(3, 1, 20, 3);
			for( int i = 0; i < 40000; i++ ) {
Training: 244
within 0.05: 177
within 0.1: 37
within 0.2: 22
within 0.3: 2
within 0.5: 6
incorrect: 0
total: 244

Test: 124
within 0.05: 79
within 0.1: 9
within 0.2: 9
within 0.3: 9
within 0.5: 5
incorrect: 13
total: 124
*/


using NeuralNetwork;

namespace NeuralNetwork
{
    class Program
    {
        // Makes an (almost) deep copy of a List<List>TrainingSet>>.
        private static List<List<TrainingSet>> DeepCopyTrainingSets(List<List<TrainingSet>> sets)
        {
            List<List<TrainingSet>> newSets = sets.GetRange(0, sets.Count);
            for (int i = 0; i < newSets.Count; i++)
            {
                newSets[i] = sets[i].GetRange(0, sets[i].Count);
            }
            return newSets;
        }

        static void Main(string[] args)
        {
			List<TrainingSet> sets = Importer.LoadPredictionDangerData("predictionDangerTrainingData.txt");

			List<TrainingSet> trainingSets = sets.GetRange(0, (sets.Count / 3) * 2);
			List<TrainingSet> testSets = sets.GetRange(trainingSets.Count, sets.Count - trainingSets.Count);
			
			Network network = new Network(3, 1, 20, 3);
			for( int i = 0; i < 40000; i++ ) {
				foreach( TrainingSet set in trainingSets ) {
					network.Train(set.Inputs, set.Outputs);
				}
			}
			network.Save("danger.nn");
			Console.WriteLine("Training: " + trainingSets.Count);
			Console.WriteLine(GetResults(network, trainingSets));
			Console.WriteLine("Test: " + testSets.Count);
			Console.WriteLine(GetResults(network, testSets));

            Console.ReadLine();
        }

		private static Network TrainNetwork(Network network, List<TrainingSet> trainingSet, List<TrainingSet> testSet)
        {
            int correct = 0;
            for (int t = 0; t < 5000; t++)
            {
                foreach (TrainingSet set in trainingSet)
                {
                    network.Train(set.Inputs, set.Outputs);
                }
                // in the beginning things are too noisy to do an early stop... after about 1000 iterations things are better
                if (t > 1000 && correct > EvaluateClassificationPrecision(network, testSet))
                {
                    Console.WriteLine(" Overtrained (" + t + " iterations)");
                    return network;
                }
                correct = EvaluateClassificationPrecision(network, testSet);
                if (t % 100 == 0)
                {
                    Console.Write(".");
                }
            }
            return network;
        }

        private static double EvaluatePrecision(Network network, List<TrainingSet> testSets)
        {
            double totalDiff = 0;
            foreach (TrainingSet set in testSets)
            {
                totalDiff += Math.Abs(network.GetOutputs(set.Inputs)[0] - set.Outputs[0]);
            }
            return totalDiff / testSets.Count;
        }

        // returns the number of correct classified instances
        private static int EvaluateClassificationPrecision(Network network, List<TrainingSet> testSets)
        {
            int correct = 0;
            foreach (TrainingSet set in testSets)
            {
                double difference = Math.Abs(network.GetOutputs(set.Inputs)[0] - set.Outputs[0]);
                if (difference < 0.1)
                    correct++;
            }
            return correct;

        }

        public static string GetResults(Network network, List<TrainingSet> sets)
        {
            int total = 0;
            int within005 = 0;
            int within01 = 0;
            int within02 = 0;
            int within03 = 0;
            int within05 = 0;
            foreach (TrainingSet set in sets)
            {
                double diff = Math.Abs(network.GetOutputs(set.Inputs)[0] - set.Outputs[0]);
                if (diff < 0.05)
                    within005++;
                else if (diff < 0.1)
                    within01++;
                else if (diff < 0.2)
                    within02++;
                else if (diff < 0.3)
                    within03++;
                else if (diff < 0.5)
                    within05++;
                total++;
            }
            string results = "";
            results += "within 0.05: " + within005 + "\n";
            results += "within 0.1: " + within01 + "\n";
            results += "within 0.2: " + within02 + "\n";
            results += "within 0.3: " + within03 + "\n";
            results += "within 0.5: " + within05 + "\n";
            results += "incorrect: " + (total - (within005 + within01 + within02 + within03 + within05)) + "\n";
            //results += "incorrect (Not within 0.1): " + (total - (within01 + within005)) + "\n";
            results += "total: " + total + "\n";
            return results;
        }

		/*static void Main(string[] args)
        {
            List<List<TrainingSet>> sets = Importer.LoadSectorData("sectorTrainingData.txt");

            int trainingSize = (sets[0].Count / 3) * 2;

            // make two DEEP(!!!) copies of sets
            List<List<TrainingSet>> trainingSets = DeepCopyTrainingSets(sets);
            List<List<TrainingSet>> testSet = DeepCopyTrainingSets(sets);

            for (int i = 0; i < sets.Count; i++)
            {
                trainingSets[i].RemoveRange(trainingSize, trainingSets[i].Count - trainingSize);
                testSet[i].RemoveRange(0, trainingSize);
            }

            List<Network> networks = new List<Network>();
            foreach (List<TrainingSet> sectorSet in trainingSets)
            {
                networks.Add(new Network(4, 1, 10));
            }

              
            for (int i = 0; i < trainingSets.Count; i++)
            {
                Console.WriteLine("Training network: " + i);
                networks[i] = TrainNetwork(networks[i], trainingSets[i], testSet[i]);
                Console.WriteLine();
            } 


            Console.WriteLine("\nTraining complete.");

            for (int i = 0; i < trainingSets.Count; i++)
            {
                List<TrainingSet> sectorSet = trainingSets[i];
                Console.WriteLine("Sector " + i + ": ");
                Console.WriteLine(GetResults(networks[i], testSet[i]));
                networks[i].Save("Sector" + i + ".nn");
            }

            Console.ReadLine();
        }*/

        /*static void Main( string[] args ) {			

            //List<Importer.TrainingSet> sets = Importer.Load("weather.txt");
            List<Importer.TrainingSet> sets = Importer.Load("iris_normal.txt");

            NeuralNetwork.Network network = new NeuralNetwork.Network(sets[0].Inputs.Length, 1, 5, 2);
            network.LearningRate = 0.01;

            Visualizer visualizer = null;
            System.Threading.Thread vThread = new System.Threading.Thread(delegate() { visualizer = new Visualizer(network); new System.Windows.Application().Run(visualizer); });
            vThread.SetApartmentState(System.Threading.ApartmentState.STA);
            vThread.Start();

            while( visualizer == null ) {
                System.Threading.Thread.Sleep(100);
            }

            int counter = 0;
            while( true ){
                if( visualizer.Jitter != 0.0 ) {
                    network.Jitter(visualizer.Jitter);
                    visualizer.Update();
                    visualizer.Jitter = 0.0;
                }
                if( !visualizer.Running ) {
                    if( visualizer.Step > 0 ) {
                        visualizer.Step--;
                    } else {
                        visualizer.Results = GetResults(network, sets);
                        System.Threading.Thread.Sleep(50);
                        continue;
                    }
                }
                counter++;
                visualizer.Total = counter;
                foreach( Importer.TrainingSet set in sets ) {
                    network.Train(set.Inputs, set.Outputs);
                }
                if( counter % 5 == 0 ) {
                    visualizer.Results = GetResults(network, sets);
                }
                visualizer.Update();
                System.Threading.Thread.Sleep(10);
            }
        }

        public static string GetResults(Network network, List<TrainingSet> sets) {
            int total = 0;
            int correct = 0;
            foreach( TrainingSet set in sets ) {
                double diff = Math.Abs(network.GetOutputs(set.Inputs)[0] - set.Outputs[0]);
                if( diff < 0.33 )
                    correct++;
                total++;
            }
            string results = "";
            results += "correct: " + correct + "\n";
            results += "incorrect: " + (total - correct) + "\n";
            results += "total: " + total + "\n";
            return results;
        }*/
    }
}
