using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Pacman.Simulator.Ghosts;

namespace Pacman.Simulator
{
	public static class StateInfo
	{
		public static bool IsInverse(Direction d1, Direction d2) {
			if( d1 == Direction.Up && d2 == Direction.Down ) return true;
			if( d1 == Direction.Down && d2 == Direction.Up ) return true;
			if( d1 == Direction.Left && d2 == Direction.Right ) return true;
			if( d1 == Direction.Right && d2 == Direction.Left ) return true;
			return false;
		}

		public static Direction GetInverse(Direction d) {
			switch( d ) {
				case Direction.Up: return Direction.Down;
				case Direction.Down: return Direction.Up;
				case Direction.Left: return Direction.Right;
				case Direction.Right: return Direction.Left;
			}
			return Direction.None;
		}

		public static PillPath NearestPill(Node currentNode, GameState gs) {
			Node bestNode = null;
			Node.PathInfo bestPath = null;
			foreach( Node node in gs.Map.Nodes ) {
				if( node.Walkable ) {					
					if( node.Type == Node.NodeType.Pill || node.Type == Node.NodeType.PowerPill ) {
						if( bestPath == null ) {
							bestNode = node;
							bestPath = gs.Pacman.Node.ShortestPath[node.X, node.Y];
							continue;
						}
						Node.PathInfo curPath = currentNode.ShortestPath[node.X, node.Y];
						if( curPath != null && curPath.Distance < bestPath.Distance ) {
							bestNode = node;
							bestPath = curPath;
						}
					}
				}
			}
			return new PillPath(bestNode, bestPath);			
		}

        // Return the nearest edible ghost in the game.
        public static Ghost NearestEdibleGhost(GameState gs)
        {
            Ghost _nearestGhost = null;

            foreach (var ghost in gs.Ghosts)
            {
                if (_nearestGhost == null)
                {
                    if (ghost.Fleeing && ghost.Entered)
                    {
                        _nearestGhost = ghost;
                    }
                }
                else if (_nearestGhost.Node.ManhattenDistance(gs.Pacman.Node) > ghost.Node.ManhattenDistance(gs.Pacman.Node)
                    && ghost.Fleeing && ghost.Entered)
                {
                    _nearestGhost = ghost;
                }
            }

            return _nearestGhost;
        }

        // Return the nearest ghost to Pacman.
        public static Ghost NearestGhost(GameState gs)
        {
            Ghost _nearestGhost = null;

            foreach (var ghost in gs.Ghosts)
            {
                // If no ghost has been assigned the make it the first one
                if (_nearestGhost == null)
                {
                    if (ghost.Entered)
                    {
                        _nearestGhost = ghost;
                    }
                }
                else if (_nearestGhost.Node.ManhattenDistance(gs.Pacman.Node) >
                         ghost.Node.ManhattenDistance(gs.Pacman.Node))
                {
                    _nearestGhost = ghost;
                }
            }

            return _nearestGhost;
        }

        public static PillPath NearestPowerPill(Node currentNode, GameState gs)
        {
            Node bestNode = null;
            Node.PathInfo bestPath = null;
            
            foreach (Node node in gs.Map.Nodes)
            {
                if (node.Walkable)
                {
                    if (node.Type == Node.NodeType.PowerPill)
                    {
                        if (bestPath == null)
                        {
                            bestNode = node;
                            bestPath = gs.Pacman.Node.ShortestPath[node.X, node.Y];
                            continue;
                        }

                        Node.PathInfo curPath = currentNode.ShortestPath[node.X, node.Y];
                        
                        if (curPath != null && curPath.Distance < bestPath.Distance)
                        {
                            bestNode = node;
                            bestPath = curPath;
                        }
                    }
                }
            }
            return new PillPath(bestNode, bestPath);		
        }

		public static PillPath FurthestPill(Node currentNode, GameState gs) {
			Node bestNode = null;
			Node.PathInfo bestPath = null;
			foreach( Node node in gs.Map.Nodes ) {
				if( node.Walkable ) {
					if( node.Type == Node.NodeType.Pill || node.Type == Node.NodeType.PowerPill ) {
						if( bestPath == null ) {
							bestNode = node;
							bestPath = gs.Pacman.Node.ShortestPath[node.X, node.Y];
							continue;
						}
						Node.PathInfo curPath = currentNode.ShortestPath[node.X, node.Y];
						if( curPath != null && curPath.Distance > bestPath.Distance ) {
							bestNode = node;
							bestPath = curPath;
						}
					}
				}
			}
			return new PillPath(bestNode, bestPath);
		}

        // Return the remaining amount of power pills in the map
        public static int RemainingPowerPills(GameState gs)
        {
            return (gs.Map.PillNodes.Where(n => n.Type == Node.NodeType.PowerPill).Count());
        }

		public class PillPath
		{
			public Node Target;
			public Node.PathInfo PathInfo;

			public PillPath(Node target, Node.PathInfo pathInfo) {
				this.Target = target;
				this.PathInfo = pathInfo;
			}
		}
	}
}
