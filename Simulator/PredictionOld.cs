using System;
using System.Collections.Generic;
using System.Text;

namespace Pacman.Simulator
{
	public class PredictionOld
	{
		private GameState gs;
		private DangerMap[] dangerMaps;
		private List<PredictEntity> ghosts;
		private List<PredictEntity> curGhosts;
		public float[] PacDanger = new float[4];
		public int[] PacDangerLimit = new int[4];
		public readonly int Iterations;
		private const float iterationDevaluator = 2.0f;
		private const float rippleDevaluator = 2.0f;
		private const float dangerSaveLimit = 0.5f;

		public PredictionOld(GameState gs, int iterations) {
			if( iterations < 1 ) {
				throw new ApplicationException("At least one iteration must be predicted");
			}
			this.Iterations = iterations;
			this.gs = gs;
			ghosts = new List<PredictEntity>(4);
			curGhosts = new List<PredictEntity>(4);
			foreach( Ghosts.Ghost ghost in gs.Ghosts ) {
				//if( ghost == gs.Red )
				//if( ghost.Chasing || ghost.RemainingFlee < 200 ) {
					ghosts.Add(new PredictEntity(ghost, gs));
					curGhosts.Add(new PredictEntity(ghost, gs));
				//}
			}
			dangerMaps = new DangerMap[iterations + 1];
			dangerMaps[0] = new DangerMap(gs);
			run();
		}

		public Direction LeastDangerous() {
			float lowest = 1.0f;
			int dangerLimit = 0;
			Direction d = Direction.None;
			for( int i = 0; i < PacDanger.Length; i++ ) {
				if( PacDanger[i] == -1.0f ) continue;
				if( PacDanger[i] < lowest || (PacDanger[i] == lowest && PacDangerLimit[i] > dangerLimit) ) {
					dangerLimit = PacDangerLimit[i];
					lowest = PacDanger[i];
					d = (Direction)i;
				}
			}
			//Console.WriteLine("Selected: " + d);
			return d;
		}

		public float Danger(Node node, int iteration) {
			return Danger(node.X, node.Y, iteration);
		}

		public float Danger(int x, int y, int iteration) {
			return dangerMaps[iteration + 1].Danger[x, y];
		}

		private void run() {
			// generate danger maps
			insertGhostDanger(dangerMaps[0]);
			for( int i = 1; i < dangerMaps.Length; i++ ) {
				dangerMaps[i] = new DangerMap(dangerMaps[i - 1]);
				updateGhosts();
				insertGhostDanger(dangerMaps[i]);
				//Console.WriteLine(i + ": " + dangerMaps[i].Danger[13, 23]);
			}
			// calculate direction dangers
			runPac(Direction.Up, gs.Pacman.Node.Up);
			runPac(Direction.Down, gs.Pacman.Node.Down);
			runPac(Direction.Left, gs.Pacman.Node.Left);
			runPac(Direction.Right, gs.Pacman.Node.Right);
			Console.WriteLine("------------------");
			Console.WriteLine(PacDanger[0] + ", " + PacDanger[1] + ", " + PacDanger[2] + ", " + PacDanger[3]);
			//Console.WriteLine(PacDangerLimit[0] + ", " + PacDangerLimit[1] + ", " + PacDangerLimit[2] + ", " + PacDangerLimit[3]);					
		}

		private void runPac(Direction direction, Node nextNode) {
			if( !nextNode.Walkable ) {
				PacDanger[(int)direction] = -1.0f;
				PacDangerLimit[(int)direction] = -1;
				return;
			}
			// calculate different routes
			List<PredictPacman> pacmans = new List<PredictPacman>();
			pacmans.Add(new PredictPacman(nextNode, direction, null));
			for( int i = 1; i < dangerMaps.Length; i++ ) {
				List<PredictPacman> newPacmans = new List<PredictPacman>();
				foreach( PredictPacman p in pacmans ) {
					// no need to check further when danger is certain death
					if( p.Danger == 1.0f ) continue;			
					// if reached powerpill then just keep score
					if( p.Node.Type == Node.NodeType.PowerPill ) {
						//newPacmans.Add(p);
						continue;
					}
					// future prediction
					foreach( Node possibleNode in p.Node.GhostPossibles[(int)p.Direction] ) {
						if( dangerMaps[i].Danger[possibleNode.X, possibleNode.Y] > p.Danger ) {
							p.Danger = dangerMaps[i].Danger[possibleNode.X, possibleNode.Y];							
						}						
						if( possibleNode == p.Node.Up ) {
							newPacmans.Add(new PredictPacman(possibleNode, Direction.Up, p));
						} else if( possibleNode == p.Node.Down ) {
							newPacmans.Add(new PredictPacman(possibleNode, Direction.Down, p));
						} else if( possibleNode == p.Node.Left ) {
							newPacmans.Add(new PredictPacman(possibleNode, Direction.Left, p));
						} else if( possibleNode == p.Node.Right ) {
							newPacmans.Add(new PredictPacman(possibleNode, Direction.Right, p));
						}
					}
				}
				pacmans = newPacmans;
			}		
			// find least dangerous			
			float danger = 1.0f;
			int dangerLimit = 0;
			foreach( PredictPacman p in pacmans ) {
				//Console.Write(p.Danger + ",");
				if( p.Danger < danger ) {					
					danger = p.Danger;
				}
				if( p.DangerLimitIteration > dangerLimit ){
					dangerLimit = p.DangerLimitIteration;
				}
			}			
			// distance danger
			foreach( PredictEntity pg in curGhosts ) {
				Node.PathInfo shortestPath = pg.Node.ShortestPath[nextNode.X, nextNode.Y];
				if( shortestPath != null ) {
					float distDanger = 1.0f / shortestPath.Distance;					
					if( distDanger > danger ) {
						danger = distDanger;
					}
				}
			}
			//Console.WriteLine("");
			//Console.WriteLine(direction + ": " + danger + " -- " + pacmans.Count);
			// save results
			PacDanger[(int)direction] = danger;
			PacDangerLimit[(int)direction] = dangerLimit;
		}

		private void updateGhosts() {
			List<PredictEntity> newGhosts = new List<PredictEntity>();
			foreach( PredictEntity pg in ghosts ) {
				foreach( Node possibleNode in pg.Node.GhostPossibles[(int)pg.Direction] ) {					
					if( possibleNode == pg.Node.Up ){
						newGhosts.Add(new PredictEntity(possibleNode, Direction.Up));
					} else if( possibleNode == pg.Node.Down ){
						newGhosts.Add(new PredictEntity(possibleNode, Direction.Down));
					} else if( possibleNode == pg.Node.Left ){
						newGhosts.Add(new PredictEntity(possibleNode, Direction.Left));
					} else if( possibleNode == pg.Node.Right ){
						newGhosts.Add(new PredictEntity(possibleNode, Direction.Right));
					}
				}
			}
			ghosts = newGhosts;
		}

		private void insertGhostDanger(DangerMap dm) {			
			foreach( PredictEntity pg in ghosts ) {
				//Console.WriteLine("Ghost: " + pg.Node);
				ripple(dm, pg.Node, 0.8f, 2);
			}
		}

		private void ripple(DangerMap dm, Node node, float danger, int size) {
			dm.Danger[node.X, node.Y] = danger;
			if( size == 0 ) return;
			ripple(dm, node.Up, danger / rippleDevaluator, size - 1);
			ripple(dm, node.Down, danger / rippleDevaluator, size - 1);
			ripple(dm, node.Left, danger / rippleDevaluator, size - 1);
			ripple(dm, node.Right, danger / rippleDevaluator, size - 1);
		}

		private class DangerMap
		{
			public readonly float[,] Danger;

			public DangerMap(GameState gs) {				
				Danger = new float[Map.Width,Map.Height];
				for( int x = 0; x < Map.Width; x++ ) {
					for( int y = 0; y < Map.Height; y++ ) {
						Danger[x, y] = 0.0f;
					}
				}
			}

			public DangerMap(DangerMap dm) {
				Danger = new float[Map.Width, Map.Height];
				for( int x = 0; x < Map.Width; x++ ) {
					for( int y = 0; y < Map.Height; y++ ) {
						Danger[x, y] = dm.Danger[x, y] / iterationDevaluator;						
					}
				}
			}
		}

		private class PredictPacman : PredictEntity
		{
			public float Danger = 0.0f;
			public int DangerLimitIteration = 0;

			public PredictPacman(Node node, Direction direction, PredictPacman pacman)
				: base(node, direction) {
				if( pacman != null ) {
					Danger = pacman.Danger;
					if( Danger < dangerSaveLimit ) {
						DangerLimitIteration++;
					}	
				}				
			}
		}

		private class PredictEntity
		{
			public readonly Node Node;
			public readonly Direction Direction;

			public int X { get { return Node.X; } }
			public int Y { get { return Node.Y; } }
			
			public PredictEntity(Ghosts.Ghost ghost, GameState gs) {
				if( ghost.Entered ) {
					this.Node = ghost.Node;
					this.Direction = ghost.Direction;
				} else {
					this.Node = gs.Map.Nodes[13, 11];
					this.Direction = Direction.Up;
				}				
			}

			public PredictEntity(Node node, Direction direction) {
				this.Node = node;
				this.Direction = direction;
			}

			public PredictEntity Clone() {
				return new PredictEntity(Node, Direction);
			}
		}
	}
}
