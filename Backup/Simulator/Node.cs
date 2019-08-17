using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

// Used for serialization
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Pacman.Simulator
{
    [Serializable()]
	public class Node : ICloneable, IEquatable<Node>
	{
		public readonly int X, Y;
		public Node Up, Down, Left, Right;
		public List<Node> PossibleDirections;
		public List<List<Node>> GhostPossibles = new List<List<Node>>(4);
		public NodeType Type;		
		private readonly Rectangle rectangle;		
		
		public PathInfo[,] ShortestPath = new PathInfo[Map.Width, Map.Height];		

		public int CenterX { get { return Map.NodeLeftDistance + X * Map.NodeDistance; } }
		public int CenterY { get { return Map.NodeTopDistance + Y * Map.NodeDistance; } }

		public bool Walkable { get { return Type != NodeType.Wall; } }

		public Node(int x, int y, NodeType type) {
			this.X = x;
			this.Y = y;
			this.Type = type;
			this.rectangle = new Rectangle(CenterX - 4, CenterY - 4, 9, 9);
        }

        #region Required Operator Overloads

        
        #endregion

        // Enable me to get all the nodes from that given direction
        public static List<Direction> GetAllPossibleDirections(Node pNode)
        {
            List<Direction> _totaldirections = new List<Direction>();

            // Add all the directions that are not going to be a problem to the
            // controller
            if (pNode.Up.Type != NodeType.Wall)
                _totaldirections.Add(Direction.Up);

            if (pNode.Down.Type != NodeType.Wall)
                _totaldirections.Add(Direction.Down);

            if (pNode.Left.Type != NodeType.Wall)
                _totaldirections.Add(Direction.Left);

            if (pNode.Right.Type != NodeType.Wall)
                _totaldirections.Add(Direction.Right);

            return _totaldirections;
        }

        // Get the neighbouring node from the given position in the provided direction
        public Node GetNeighbourGameState(int x, int y, GameState pGameState, Direction pDirection)
        {
            return pGameState.Map.GetNodeDirection(x, y, pDirection);
        }

        /// <summary>
        /// Get the neighbour to the node that we are looking at with the provided direction
        /// </summary>
        /// <param name="pDirection">The direction that we want to get the node from</param>
        /// <returns>Returns the node in the given direction from this one</returns>
        public Node GetNeighbour(Direction pDirection)
        {   
            switch (pDirection)
            {
                case Direction.Down:
                    return Down;
                break;

                case Direction.Left:
                    return Left;
                break;

                case Direction.Up:
                    return Up;
                break;

                case Direction.Right:
                    return Right;
                break;
            }

            return null;
        }

		public void Draw(Graphics g) {
			if( Type == NodeType.None ) {
				g.FillRectangle(Brushes.Black, rectangle);
			}/* else if( Type == NodeType.Wall ) {
				g.FillRectangle(Brushes.Red, rectangle);
			}*/
		}

		public void Draw(Graphics g, Brush brush) {
			g.FillRectangle(brush, rectangle);
		}

		public Node GetNode(Direction direction) {
			switch( direction ) {
				case Direction.Up: return Up;
				case Direction.Down: return Down;
				case Direction.Left: return Left;
				case Direction.Right: return Right;
			}
			return null;
		}

		public Direction GetDirection(Node node) {
			if( node == Up ) return Direction.Up;
			if( node == Down ) return Direction.Down;
			if( node == Left ) return Direction.Left;
			if( node == Right ) return Direction.Right;
			return Direction.None;
		}

		public bool IsSame(Node node) {
			if( node.X == X && node.Y == Y ) {
				return true;
			}
			return false;
		}

		public int ManhattenDistance(Node node) {
			return Math.Abs(X - node.X) + Math.Abs(Y - node.Y);
		}

		public enum NodeType { Pill, PowerPill, None, Wall };
        [Serializable]
		public class PathInfo
		{
			public readonly Direction Direction;
			public readonly int Distance;

			public PathInfo(Direction direction, int distance) {
				this.Direction = direction;
				this.Distance = distance;
			}

			public PathInfo Clone() {
				return new PathInfo(Direction, Distance);
			}
		}

		public Node Clone() {
			Node n = new Node(X, Y, Type);
            n.Up = Up;
            n.Down = Down;
            n.Left = Left;
            n.Right = Right;

            // Loop through this and clone all of the items in the array
            //n.ShortestPath = new PathInfo[Map.Width,Map.Height];

            n.GhostPossibles = GhostPossibles;
			n.ShortestPath = ShortestPath;
            n.PossibleDirections = PossibleDirections;
			return n;
		}

		public override string ToString() {
			return "[" + X + "," + Y + "]";
		}

        #region ICloneable Members

        object ICloneable.Clone()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEquatable<Node> Members

        public bool Equals(Node other)
        {
            return this.X == other.X && this.Y == other.Y;
        }

        #endregion
    }
}
