using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Pacman.Simulator
{
    [Serializable()]
	public class Map : ICloneable
	{
		public const int Width = 28;
		public const int Height = 31;
		public const int NodeTopDistance = 5;
		public const int NodeLeftDistance = 3;
		public const int NodeDistance = 8;
		public readonly int PixelWidth;
		public readonly int PixelHeight;
		public static readonly Color BackColor = Color.FromArgb(0,0,0);
		
        [NonSerialized()]
        private Image image;
		public int PillsLeft = 0;
		public readonly Maze Maze;

		private Node[,] BackupNodes = new Node[Width,Height];
		public List<Node> PillNodes = new List<Node>();

		public Node[,] Nodes = new Node[Width, Height];

        // Generate the new map use for holding the weightings
        public Node.NodeType[,] NodeData = new Node.NodeType[Width, Height];

		public bool[] Tunnels = new bool[Height];

		public Map(Bitmap map, Maze maze) {
			this.Maze = maze;
			image = map;
			PixelWidth = image.Width;
			PixelHeight = image.Height;
			analyzeMap(map);
			analyzeNodes();
			analyzeShortestPath();
			foreach( Node n in Nodes ) {
				BackupNodes[n.X, n.Y] = n.Clone();
			}
		}

		public void Reset(){
			PillsLeft = PillNodes.Count;
			foreach( Node n in PillNodes ) {
				n.Type = BackupNodes[n.X,n.Y].Type;
			}
		}

        /// <summary>
        /// Set the value of the node at the given x & y coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="pType">What we are setting it to.</param>
        public void SetNode(int x, int y, Node.NodeType pType)
        {
            foreach (var n in Nodes)
            {
                if (n.X == x && n.Y == y)
                    n.Type = pType;
            }
        }

        public Node GetNodeDirection(int x, int y, Direction pDirection)
        {
            int _tempX = x, _tempY = y;

            // Based on the direction that is provided, return the node that is required.
            switch (pDirection)
            {
                case Direction.Left:
                    _tempX--;
                    break;
                case Direction.Right:
                    _tempX++;
                    break;
                case Direction.Up:
                    _tempY--;
                    break;
                case Direction.Down:
                    _tempY++;
                    break;
            }

            if (_tempX < Map.Width && _tempX >= 0 &&
                _tempY < Map.Height && _tempY >= 0)
            {
                return Nodes[_tempX, _tempY];
            }
            else
            {
                return null;
            }
        }

		public Node GetNode(int x, int y) {
			int xPos = (int)Math.Floor((x - NodeLeftDistance) / (float)NodeDistance);
			if( xPos < 0 ) xPos = 0; if( xPos >= Width ) xPos = Width - 1;
			int yPos = (int)Math.Floor((y - NodeTopDistance) / (float)NodeDistance);
			return Nodes[xPos, yPos];
		}

		public Node GetNodeNonWall(int x, int y) {
			Node n = GetNode(x, y);
			if( n.Type == Node.NodeType.Wall ) {
				if( n.Up.Type != Node.NodeType.Wall ) return n.Up;
				if( n.Down.Type != Node.NodeType.Wall ) return n.Down;
				if( n.Left.Type != Node.NodeType.Wall ) return n.Left;
				if( n.Right.Type != Node.NodeType.Wall ) return n.Right;
			}
			return n;
		}

		public List<Node> GetRoute(Node start, Node end) {
			return GetRoute(start.X, start.Y, end.X, end.Y);
		}

		public List<Node> GetRoute(int startX, int startY, int endX, int endY) {
			Node.PathInfo pathInfo = Nodes[startX, startY].ShortestPath[endX, endY];
			if( pathInfo == null ) {
				//Console.WriteLine("No path from " + startX + "," + startY + " to " + endX + "," + endY);
				return null;
			}			
			List<Node> path = new List<Node>();
			Node curNode = Nodes[startX,startY];
			while( !(curNode.X == endX && curNode.Y == endY) ) {
				curNode = curNode.GetNode(curNode.ShortestPath[endX, endY].Direction);
				path.Add(curNode);				
			}
			return path;
		}

		private void analyzeShortestPath() {			
			foreach( Node n in Nodes ) {
				if( n.Type == Node.NodeType.Wall )
					continue;
				//if( !(n.X == 13 && n.Y == 23) ) // test only pac position
				//	continue;
				LinkedList<LinkedListNode<Node>> openList = new LinkedList<LinkedListNode<Node>>();
				LinkedListNode<LinkedListNode<Node>> startNode = new LinkedListNode<LinkedListNode<Node>>(new LinkedListNode<Node>(n));
				n.ShortestPath[n.X, n.Y] = new Node.PathInfo(Direction.None, 0);
				shortestPathAddNode(startNode.Value, startNode.Value.Value.Up, Direction.Up, openList);
				shortestPathAddNode(startNode.Value, startNode.Value.Value.Down, Direction.Down, openList);
				shortestPathAddNode(startNode.Value, startNode.Value.Value.Left, Direction.Left, openList);
				shortestPathAddNode(startNode.Value, startNode.Value.Value.Right, Direction.Right, openList);
				while( openList.Count > 0 ) {
					shortestPathProcessNode(n, openList.First, openList);
				}
				foreach( Node nClean in Nodes ) {
					nClean.ShortestPath[nClean.X, nClean.Y] = null;
				}
			}
		}

		private void shortestPathProcessNode(Node curNode, LinkedListNode<LinkedListNode<Node>> n, LinkedList<LinkedListNode<Node>> openList) {
			openList.Remove(n);
			Node removeNode = n.Value.Value;
			curNode.ShortestPath[removeNode.X, removeNode.Y] = removeNode.ShortestPath[removeNode.X, removeNode.Y];
			Direction fromDir = removeNode.ShortestPath[removeNode.X, removeNode.Y].Direction;
			shortestPathAddNode(n.Value, n.Value.Value.Up, fromDir, openList);
			shortestPathAddNode(n.Value, n.Value.Value.Down, fromDir, openList);
			shortestPathAddNode(n.Value, n.Value.Value.Left, fromDir, openList);
			shortestPathAddNode(n.Value, n.Value.Value.Right, fromDir, openList);
		}

		private void shortestPathAddNode(LinkedListNode<Node> n, Node nDir, Direction dir, LinkedList<LinkedListNode<Node>> openList) {
			if( nDir.Type == Node.NodeType.Wall || nDir.ShortestPath[nDir.X,nDir.Y] != null )
				return;
			nDir.ShortestPath[nDir.X, nDir.Y] = new Node.PathInfo(dir, n.Value.ShortestPath[n.Value.X, n.Value.Y].Distance + 1);
			bool added = false;
			foreach( LinkedListNode<Node> existingNode in openList ) {
				if( existingNode.Value.ShortestPath[existingNode.Value.X, existingNode.Value.Y].Distance >
					nDir.ShortestPath[nDir.X, nDir.Y].Distance ) {
					openList.AddBefore(new LinkedListNode<LinkedListNode<Node>>(existingNode), new LinkedListNode<LinkedListNode<Node>>(new LinkedListNode<Node>(nDir)));					
					added = true;
					break;
				}
			}
			if( !added ) {
				openList.AddLast(new LinkedListNode<Node>(nDir));
			}
		}

		public void analyzeNodes() {
			for( int x = 0; x < Width; x++ ) {
				for( int y = 0; y < Height; y++ ) {
					// up
					if( y == 0 ) {
						Nodes[x, y].Up = Nodes[x, Height - 1];
					} else {
						Nodes[x, y].Up = Nodes[x, y - 1];
					}
					// down
					if( y == Height - 1 ) {
						Nodes[x, y].Down = Nodes[x, 0];
					} else {
						Nodes[x, y].Down = Nodes[x, y + 1];
					}
					// left
					if( x == 0 ) {
						Nodes[x, y].Left = Nodes[Width - 1, y];
						// tunnels
						if( Nodes[x, y].Walkable ) { 
							Tunnels[y] = true;							
						} else {
							Tunnels[y] = false;
						}
					} else {
						Nodes[x, y].Left = Nodes[x - 1, y];
					}
					// right
					if( x == Width - 1 ) {
						Nodes[x, y].Right = Nodes[0, y];
					} else {
						Nodes[x, y].Right = Nodes[x + 1, y];
					}
				}
			}
			// close center
			Nodes[13, 12].Type = Node.NodeType.Wall;
			Nodes[14, 12].Type = Node.NodeType.Wall;
			// set possible directions
			for( int x = 0; x < Width; x++ ) {
				for( int y = 0; y < Height; y++ ) {
					Nodes[x, y].PossibleDirections = new List<Node>();
					if( Nodes[x, y].Up.Walkable ) {
						Nodes[x, y].PossibleDirections.Add(Nodes[x, y].Up);
					}
					if( Nodes[x, y].Down.Walkable ) {
						Nodes[x, y].PossibleDirections.Add(Nodes[x, y].Down);
					}
					if( Nodes[x, y].Left.Walkable ) {
						Nodes[x, y].PossibleDirections.Add(Nodes[x, y].Left);
					}
					if( Nodes[x, y].Right.Walkable ) {
						Nodes[x, y].PossibleDirections.Add(Nodes[x, y].Right);
					}
					// set ghost possible			
					Nodes[x, y].GhostPossibles.Add(new List<Node>(4));
					foreach( Node n in Nodes[x, y].PossibleDirections ) {
						if( n != Nodes[x, y].Down ) {
							Nodes[x, y].GhostPossibles[0].Add(n);
						}
					}
					Nodes[x, y].GhostPossibles.Add(new List<Node>(4));
					foreach( Node n in Nodes[x, y].PossibleDirections ) {
						if( n != Nodes[x, y].Up ) {
							Nodes[x, y].GhostPossibles[1].Add(n);
						}
					}
					Nodes[x, y].GhostPossibles.Add(new List<Node>(4));
					foreach( Node n in Nodes[x, y].PossibleDirections ) {
						if( n != Nodes[x, y].Right ) {
							Nodes[x, y].GhostPossibles[2].Add(n);
						}
					}
					Nodes[x, y].GhostPossibles.Add(new List<Node>(4));
					foreach( Node n in Nodes[x, y].PossibleDirections ) {
						if( n != Nodes[x, y].Left ) {
							Nodes[x, y].GhostPossibles[3].Add(n);
						}
					}
				}
			}
		}

		private void analyzeMap(Bitmap map){			
			for( int x = 0; x < Width; x++ ) {
				for( int y = 0; y < Height; y++ ) {
					if( gamePosition(map, x, y) ) {
						if( getPositionColor(map, x, y, 0, 0) == BackColor ) {
							Nodes[x, y] = new Node(x, y, Node.NodeType.None);
						} else if( getPositionColor(map, x, y, -1, -1) == BackColor ) {
							Nodes[x, y] = new Node(x, y, Node.NodeType.Pill);
							PillNodes.Add(Nodes[x, y]);
							PillsLeft++;
						} else {
							Nodes[x, y] = new Node(x, y, Node.NodeType.PowerPill);
							PillNodes.Add(Nodes[x, y]);
							PillsLeft++;
						}
					} else {
						Nodes[x, y] = new Node(x, y, Node.NodeType.Wall);
					}
				}
			}
		}		

		private bool gamePosition(Bitmap map, int x, int y) {
			if( getPositionColor(map, x, y, -3, -3) != BackColor )
				return false;
			if( getPositionColor(map, x, y, -3, +4) != BackColor )
				return false;
			if( getPositionColor(map, x, y, +4, -3) != BackColor )
				return false;
			if( getPositionColor(map, x, y, +4, +4) != BackColor )
				return false;
			return true;
		}

		private Color getPositionColor(Bitmap map, int x, int y, int xOffset, int yOffset) {
			return map.GetPixel(NodeLeftDistance + x * NodeDistance + xOffset, NodeTopDistance + y * NodeDistance + yOffset);
		}

		public void Draw(Graphics g) {
			g.DrawImageUnscaled(image, new Point(0, 0));
			foreach( Node node in Nodes ) {
				node.Draw(g);				
			}
		}

        #region ICloneable Members
        public object Clone()
        {
            // Clone the values that we require
            // and regenerate all the reference values.
            Map _newmap = (Map)this.MemberwiseClone();
            _newmap.PillNodes = new List<Node>();
            _newmap.PillNodes.Clear();

            // Loop through the pill nodes and add them as well
            //for (int i = 0; i < PillNodes.Count; i++)
            //{
            //    _newmap.PillNodes.Add(this.PillNodes[i].Clone());
            //}


            // Create a new array and then add them to the 2D grid.
            _newmap.BackupNodes = new Node[BackupNodes.GetLength(0),
                                           BackupNodes.GetLength(1)];

            for (int x = 0; x < BackupNodes.GetLength(0); x++)
            {
                for (int y = 0; y < BackupNodes.GetLength(1); y++)
                {
                    _newmap.BackupNodes[x, y] = this.BackupNodes[x, y].Clone();
                }
            }

            _newmap.Nodes = new Node[BackupNodes.GetLength(0),
                                     BackupNodes.GetLength(1)];

            // Loop through the nodes and copy them appropriately
            for (int x = 0; x < Nodes.GetLength(0); x++)
            {
                for (int y = 0; y < Nodes.GetLength(1); y++)
                {
                    _newmap.Nodes[x, y] = Nodes[x, y].Clone();

                    if (_newmap.Nodes[x, y].Type == Node.NodeType.PowerPill ||
                        _newmap.Nodes[x, y].Type == Node.NodeType.Pill)
                    {
                        // Add the node
                        _newmap.PillNodes.Add(_newmap.Nodes[x, y]);

                    }

                    
                }
            }

            // Copy over the node data.
            _newmap.NodeData = new Node.NodeType[Width, Height];
            foreach (var n in Nodes)
            {
                _newmap.NodeData[n.X, n.Y] = n.Type;
            }

            //_newmap.analyzeNodes();

            return _newmap;
        }

        #endregion
    }
}
