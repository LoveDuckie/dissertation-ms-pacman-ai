using System;
using System.Collections.Generic;
using System.Text;

namespace Pacman.Simulator
{
	public class Prediction
	{
		private GameState gs;
		public readonly int Iterations;
		public DangerMap[] DangerMaps;
		private List<PredictGhost> ghosts;
		private List<PredictGhost> tempGhosts;
		public List<Direction> PossibleDirections;
		private const int fleeLength = 7;
		private const bool debug = false;

		public Prediction(GameState gs, int iterations) {
			this.gs = gs;
			this.Iterations = iterations;

			ghosts = new List<PredictGhost>(4);
			tempGhosts = new List<PredictGhost>(4);
			foreach( Ghosts.Ghost ghost in gs.Ghosts ) {
				/*ghost.Enabled = false;
				if( ghost == gs.Red ){
					ghost.Enabled = true;*/
				if( ghost.Chasing ) { //  || ghost.RemainingFlee < 800
					ghosts.Add(new PredictGhost(ghost, gs));
					tempGhosts.Add(new PredictGhost(ghost, gs));
				}
			}			
			DangerMaps = new DangerMap[iterations + 1];
			DangerMaps[0] = new DangerMap();
			run();
			//Console.WriteLine("ghosts: " + tempGhosts.Count);
		}

		private void run() {
			// generate danger maps
			insertGhostDanger(DangerMaps[0]);
			for( int i = 1; i < DangerMaps.Length; i++ ) {
				DangerMaps[i] = new DangerMap(DangerMaps[i-1]); // wooot
				updateGhosts();
				insertGhostDanger(DangerMaps[i]);
				//Console.WriteLine(i + ": " + dangerMaps[i].Danger[13, 23]);				
				//Console.WriteLine(i + ": " + dangerMaps[i].Danger[gs.Pacman.Node.X, gs.Pacman.Node.Y]);
			}
		}

		private void updateGhosts() {
			List<PredictGhost> newGhosts = new List<PredictGhost>();
			foreach( PredictGhost pg in tempGhosts ) {
				List<Node> possibles = pg.Node.GhostPossibles[(int)pg.Direction];
				foreach( Node possibleNode in possibles ) {
					if( possibleNode == pg.Node.Up ) {
						newGhosts.Add(new PredictGhost(possibleNode, Direction.Up, pg.Danger / possibles.Count, pg.Chasing));
					} else if( possibleNode == pg.Node.Down ) {
						newGhosts.Add(new PredictGhost(possibleNode, Direction.Down, pg.Danger / possibles.Count, pg.Chasing));
					} else if( possibleNode == pg.Node.Left ) {
						newGhosts.Add(new PredictGhost(possibleNode, Direction.Left, pg.Danger / possibles.Count, pg.Chasing));
					} else if( possibleNode == pg.Node.Right ) {
						newGhosts.Add(new PredictGhost(possibleNode, Direction.Right, pg.Danger / possibles.Count, pg.Chasing));
					}
				}
			}
			tempGhosts = newGhosts;
		}

		private void insertGhostDanger(DangerMap dm) {
			foreach( PredictGhost pg in tempGhosts ) {
				dm.Danger[pg.Node.X, pg.Node.Y] += pg.Danger;
				if( dm.Danger[pg.Node.X, pg.Node.Y] > 1.0f ) {
					dm.Danger[pg.Node.X, pg.Node.Y] = 1.0f;
				}
			}
		}

		public class DangerMap
		{
			public readonly float[,] Danger;

			public DangerMap() {
				Danger = new float[Map.Width, Map.Height];
			}

			public DangerMap(DangerMap dm) {
				Danger = new float[Map.Width, Map.Height];
				for( int x = 0; x < Map.Width; x++ ) {
					for( int y = 0; y < Map.Height; y++ ) {
						Danger[x, y] = dm.Danger[x, y];// / 2.0f;
					}
				}
			}
		}

		private class PredictPacman : PredictEntity
		{
			public float Danger = 0.0f;

			public PredictPacman(Node node, Direction direction, PredictPacman pacman)
				: base(node, direction) {
				if( pacman != null ) {
					Danger = pacman.Danger;
				}
			}
		}

		private class PredictGhost : PredictEntity
		{
			public readonly bool Chasing;
			public readonly float Danger = 1.0f;

			public PredictGhost(Ghosts.Ghost ghost, GameState gs)
				: base(ghost.Node, ghost.Direction) {
				if( ghost.Entered ) {
					this.Chasing = (ghost.Chasing || ghost.RemainingFlee < 200);					
				} else {
					this.Chasing = true;
					this.Node = gs.Map.Nodes[13, 11];
					this.Direction = Direction.Up; 		
				}
			}

			public PredictGhost(Node node, Direction direction, float danger, bool chasing)
				: base(node, direction) {
				this.Chasing = chasing;
				this.Danger = danger;
			}
		}

		private class PredictEntity
		{
			public Node Node;
			public Direction Direction;

			public int X { get { return Node.X; } }
			public int Y { get { return Node.Y; } }
			
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
