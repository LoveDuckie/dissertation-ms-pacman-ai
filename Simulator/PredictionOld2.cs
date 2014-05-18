using System;
using System.Collections.Generic;
using System.Text;

namespace Pacman.Simulator
{
	public class Prediction2
	{
		private GameState gs;
		public readonly int Iterations;
		private DangerMap[] dangerMaps;
		private List<PredictGhost> ghosts;
		private List<PredictGhost> tempGhosts;
		public List<Direction> PossibleDirections;
		private const int fleeLength = 7;
		private const bool debug = true;

		// Status
		// Short range: Good
		// Medium range: Good
		// Long range: Poor

		// Bugs
		// - Long range should be based on how much the ghosts can actually spread out
		// - Not eating pill very well (probably related to long range issues)
		// - Problem with warp tunnels

		public Prediction2(GameState gs, int iterations) {
			this.gs = gs;
			this.Iterations = iterations;

			ghosts = new List<PredictGhost>(4);
			tempGhosts = new List<PredictGhost>(4);
			foreach( Ghosts.Ghost ghost in gs.Ghosts ) {
				/*ghost.Enabled = false;
				if( ghost == gs.Red ){
					ghost.Enabled = true;*/
				if( ghost.Chasing || ghost.RemainingFlee < 200 ) {
					ghosts.Add(new PredictGhost(ghost, gs));
					tempGhosts.Add(new PredictGhost(ghost, gs));
				}
			}
			dangerMaps = new DangerMap[iterations + 1];
			dangerMaps[0] = new DangerMap(gs);
			run();
			//Console.WriteLine("ghosts: " + tempGhosts.Count);
		}

		private void run() {
			// generate danger maps
			insertGhostDanger(dangerMaps[0]);
			for( int i = 1; i < dangerMaps.Length; i++ ) {
				dangerMaps[i] = new DangerMap(gs);
				updateGhosts();
				insertGhostDanger(dangerMaps[i]);
				//Console.WriteLine(i + ": " + dangerMaps[i].Danger[13, 23]);
				//Console.WriteLine(i + ": " + dangerMaps[i].Danger[gs.Pacman.Node.X, gs.Pacman.Node.Y]);
			}			
			// calculate danger for pacman
			PossibleDirections = new List<Direction>();
			foreach( Node node in gs.Pacman.Node.PossibleDirections ) {
				PossibleDirections.Add(gs.Pacman.Node.GetDirection(node));				
			}
			// eliminate directions where ghosts close
			for( int i = 1; i < 2; i++ ) {
				foreach( PredictGhost ghost in ghosts ) {
					if( !ghost.Chasing ) {
						continue;
					}
					Node.PathInfo shortestPath = gs.Pacman.Node.ShortestPath[ghost.Node.X, ghost.Node.Y];
					if( shortestPath != null ) {
						if( i == 1 ) {
							//Console.WriteLine("shortest: " + shortestPath.Direction + " @ " + shortestPath.Distance + " : " + ghost.Node);
						}
						if( shortestPath.Distance < i ) {
							// remove
							PossibleDirections.Remove(shortestPath.Direction);
							if( PossibleDirections.Count == 1 ) {
								if( debug ) Console.WriteLine(":: Close danger decision: " + PossibleDirections[0]);
								goto OneDirectionLeft;
							}
						}
					} else if( gs.Pacman.Node == ghost.Node ) {
						List<Direction> newPossibles = new List<Direction>();
						foreach( Direction d in PossibleDirections ) {
							newPossibles.Add(d);
						}
						newPossibles.Remove(StateInfo.GetInverse(ghost.Direction));
						if( newPossibles.Count == 1 ) {
							PossibleDirections = newPossibles;
							if( debug ) Console.WriteLine(":: Extreme danger decision: " + PossibleDirections[0]);
							goto OneDirectionLeft;
						}						
					}
				}
			}
			// circular run ...
			for( int i = 3; i < 10; i++ ) {
				int result;
				if( i < 6 ){
					result = circularTest(i, i - 2);
				} else{
					result = circularTest(i, i);
				}
				if( result == 0 ) {
					break;
				}
				// warp hack
				if( gs.Map.Tunnels[gs.Pacman.Node.Y] ) {
					if( gs.Pacman.Node.X < 2 || gs.Pacman.Node.X > Map.Width - 3  ) {
						PossibleDirections.Remove(Direction.Left);
						PossibleDirections.Remove(Direction.Right);
						PossibleDirections.Add(Direction.Left);
						PossibleDirections.Add(Direction.Right);
					}
				}
				if( PossibleDirections.Count == 1 ) {
					if( debug ) Console.WriteLine(":: Circular test decision: " + PossibleDirections[0]);
					goto OneDirectionLeft;
				}
			}			
			/*
			// try and predict safe route
			List<Direction> Safe = new List<Direction>();
			List<UnsafeDirection> Unsafe = new List<UnsafeDirection>();	
			foreach( Direction d in PossibleDirections ) {
				int result = evasionPac(d);
				Console.WriteLine(d + ": " + result);
				if( result >= Iterations ) {					
					Safe.Add(d);
				} else {
					Unsafe.Add(new UnsafeDirection(d,result));
				}
			}
			if( Safe.Count > 0 ) {
				PossibleDirections = Safe;
			} else{			
				Unsafe.Sort(new Comparison<UnsafeDirection>(delegate(UnsafeDirection u1, UnsafeDirection u2){
					if( u1.Safety == u2.Safety ) return 0;
					if( u1.Safety > u2.Safety ) return -1;
					return 1;
				}));
				foreach( UnsafeDirection u in Unsafe ) {
					Console.WriteLine(" - " + u.Direction + ": " + u.Safety);
				}
				PossibleDirections = new List<Direction>();
				int best = Unsafe[0].Safety;
				foreach( UnsafeDirection u in Unsafe ) {
					if( u.Safety == best ) {
						PossibleDirections.Add(Unsafe[0].Direction);
					}
				}
			}
			if( PossibleDirections.Count == 1 ) {
				Console.WriteLine(":: Unsafe decision: " + PossibleDirections[0]);
			}
			 */
			// finished
		OneDirectionLeft:			
			//Console.WriteLine("------------------");
			return;
		}

		private class UnsafeDirection
		{
			public readonly Direction Direction;
			public readonly int Safety;

			public UnsafeDirection(Direction direction, int safety) {
				this.Direction = direction;
				this.Safety = safety;
			}
		}

		private int circularTest(int radius, int check) {
			int startX = gs.Pacman.Node.X - radius;
			int endX = gs.Pacman.Node.X + radius;
			int startY = gs.Pacman.Node.Y - radius;
			int endY = gs.Pacman.Node.Y + radius;
			List<Node> circularNodes = new List<Node>();
			for( int y = startY; y <= gs.Pacman.Node.Y + radius; y++ ) {
				for( int x = startX; x <= gs.Pacman.Node.X + radius; x++ ) {
					int testY = y; if( testY < 1 ) testY = 1; if( testY >= Map.Height ) testY = Map.Height - 2;
					int testX = x;
					if( gs.Map.Tunnels[testY] ) {
						if( testX < 0 ) testX = 0; if( testX >= Map.Width ) testX = Map.Width - 1;
					} else {
						if( testX < 1 ) testX = 1; if( testX >= Map.Width ) testX = Map.Width - 2;
					}
					if( y == startY || y == endY || x == startX || x == endX || 
						testX == 1 || testY == 1 || testX == Map.Width - 2 || testY == Map.Height - 2 ) {
						if( gs.Map.Nodes[testX, testY].Walkable ) {
							circularNodes.Add(gs.Map.Nodes[testX, testY]);
						}
						/*if( x >= 0 && x < Map.Width &&
							y >= 0 && y < Map.Height ) {
								if( gs.Map.Nodes[testX, testY].Walkable ) {
								circularNodes.Add(gs.Map.Nodes[testX, testY]);
							}
						}*/
					}
				}
			}
			bool[] possibles = { false, false, false, false };
			bool[] allowed = { false, false, false, false };
			foreach( Direction d in PossibleDirections ) {
				allowed[(int)d] = true;
			}
			foreach( Node node in circularNodes ) {
				Node.PathInfo path = gs.Pacman.Node.ShortestPath[node.X, node.Y];
				if( path != null && possibles[(int)path.Direction] == false && allowed[(int)path.Direction] ) {
					List<Node> route = gs.Map.GetRoute(gs.Pacman.Node, node);
					possibles[(int)path.Direction] = true;
					foreach( Node rNode in route ) {
						if( dangerMaps[check].Danger[rNode.X, rNode.Y] == 1.0f ) {
							possibles[(int)path.Direction] = false;
							break;
						}
						if( rNode.Type == Node.NodeType.PowerPill ) {
							break;
						}						
					}
				}
			}
			if( debug ) Console.Write(radius + ": ");
			List<Direction> newPossibleDirections = new List<Direction>();
			for( int i = 0; i < possibles.Length; i++ ) {
				if( possibles[i] ) {
					newPossibleDirections.Add((Direction)i);
				}
				if( debug ) Console.Write(possibles[i] + ",");
			}
			if( newPossibleDirections.Count > 0 ) {
				PossibleDirections = newPossibleDirections;
			}
			if( debug ) Console.WriteLine("");
			return newPossibleDirections.Count;
		}

		private int evasionPac(Direction direction) {
			// calculate different routes
			int best = 0;
			List<PredictPacman> pacmans = new List<PredictPacman>();
			pacmans.Add(new PredictPacman(gs.Pacman.Node, direction, null));
			for( int i = 0; i < 10; i++ ) {
				List<PredictPacman> newPacmans = new List<PredictPacman>();
				foreach( PredictPacman p in pacmans ) {
					// future prediction
					foreach( Node possibleNode in p.Node.GhostPossibles[(int)p.Direction] ) {
						if( possibleNode.Type == Node.NodeType.PowerPill ) {
							return Iterations;
						}
						if( i > 0 && dangerMaps[0].Danger[possibleNode.X, possibleNode.Y] == 1.0f ) {
							break;
						}
						if( i > best ) {
							best = i;
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
			return best;
		}

		private int runPac(Direction direction) {
			// calculate different routes
			int best = 0;
			List<PredictPacman> pacmans = new List<PredictPacman>();
			pacmans.Add(new PredictPacman(gs.Pacman.Node, direction, null));
			for( int i = 0; i < dangerMaps.Length; i++ ) {
				List<PredictPacman> newPacmans = new List<PredictPacman>();
				foreach( PredictPacman p in pacmans ) {					
					// future prediction
					foreach( Node possibleNode in p.Node.GhostPossibles[(int)p.Direction] ) {
						if( possibleNode.Type == Node.NodeType.PowerPill ) {
							return Iterations;
						}
						if( i > 5 && dangerMaps[i].Danger[possibleNode.X, possibleNode.Y] == 1.0f ) {
							break;
						}
						if( i > best ) {
							best = i;
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
			return best;
		}

		private void updateGhosts() {
			List<PredictGhost> newGhosts = new List<PredictGhost>();
			foreach( PredictGhost pg in tempGhosts ) {
				foreach( Node possibleNode in pg.Node.GhostPossibles[(int)pg.Direction] ) {
					if( possibleNode == pg.Node.Up ) {
						newGhosts.Add(new PredictGhost(possibleNode, Direction.Up, pg.Chasing));
					} else if( possibleNode == pg.Node.Down ) {
						newGhosts.Add(new PredictGhost(possibleNode, Direction.Down, pg.Chasing));
					} else if( possibleNode == pg.Node.Left ) {
						newGhosts.Add(new PredictGhost(possibleNode, Direction.Left, pg.Chasing));
					} else if( possibleNode == pg.Node.Right ) {
						newGhosts.Add(new PredictGhost(possibleNode, Direction.Right, pg.Chasing));
					}
				}
			}
			tempGhosts = newGhosts;
		}

		private void insertGhostDanger(DangerMap dm) {
			foreach( PredictEntity pg in tempGhosts ) {				
				ripple(dm, pg.Node, 1.0f, 0);
			}
		}

		private void ripple(DangerMap dm, Node node, float danger, int size) {
			dm.Danger[node.X, node.Y] = danger;
			if( size == 0 ) return;
			ripple(dm, node.Up, danger, size - 1);
			ripple(dm, node.Down, danger, size - 1);
			ripple(dm, node.Left, danger, size - 1);
			ripple(dm, node.Right, danger, size - 1);
		}

		private class DangerMap
		{
			public readonly float[,] Danger;

			public DangerMap(GameState gs) {
				Danger = new float[Map.Width, Map.Height];				
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

			public PredictGhost(Ghosts.Ghost ghost, GameState gs)
				: base(ghost.Node, ghost.Direction) {
				if( ghost.Entered ) {
					this.Chasing = (ghost.Chasing || ghost.RemainingFlee < 200);					
				} else {
					this.Chasing = false;
					this.Node = gs.Map.Nodes[13, 11];
					this.Direction = Direction.Up; 		
				}
			}

			public PredictGhost(Node node, Direction direction, bool chasing)
				: base(node, direction) {
				this.Chasing = chasing;
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
