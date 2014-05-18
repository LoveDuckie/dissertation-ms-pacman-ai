using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Pacman.Simulator
{
    [Serializable()]
	public abstract class Entity
    {
        #region Members
        public const int Width = 14;
		public const int Height = 14;

		protected float x, y;
		public float Speed = 0.0f;		
		protected Direction direction;
		protected Direction lastNoneDirection;
		
		private Direction nextDirection;
        private GameState gameState;

		protected Node node;
        #endregion

        #region Properties
        public GameState GameState
        {
            get { return gameState; }
            set { gameState = value; }
        }
        public Node Node 
        { 
            get { return node; }
            set { node = value; }
        }

        public Direction Direction
        {
            get { return direction; }
        }
        #endregion

        
		//private Point lastPosition; // for use with setPosition
		public void SetPosition(int x, int y) {
			node = gameState.Map.GetNode(x, y);
			this.x = x;
			this.y = y;
			/*if (lastPosition != null) {
				if (Math.Abs(x - lastPosition.X) > 4.0) {
					if (x < lastPosition.X) {
						this.direction = Direction.Left;
						//Console.WriteLine("left");
					} else {
						this.direction = Direction.Right;
						//Console.WriteLine("right");
					}
					//Console.WriteLine("X trigger: " + x + " : " + lastPosition.X + " ... " + y + " : " + lastPosition.Y);
					lastPosition = new Point(x, y);
				}
				if (Math.Abs(y - lastPosition.Y) > 4.0) {
					if (y < lastPosition.Y) {
						this.direction = Direction.Up;
						//Console.WriteLine("up");
					} else {
						this.direction = Direction.Down;
						//Console.WriteLine("down");
					}
					//Console.WriteLine("Y trigger: " + x + " : " + lastPosition.X + " ... " + y + " : " + lastPosition.Y);
					lastPosition = new Point(x, y);
				}
			}*/
		}

		Point lastPosition = new Point(-1000,0);
		public void SetRoadPosition(int x, int y) {
			node = gameState.Map.GetNodeNonWall(x, y);
			this.x = x;
			this.y = y;
			if (lastPosition.X != -1000) {
				if (Math.Abs(x - lastPosition.X) > 4.0) {
					if (x < lastPosition.X) {
						this.direction = Direction.Left;
						//Console.WriteLine("left");
					} else {
						this.direction = Direction.Right;
						//Console.WriteLine("right");
					}
					//Console.WriteLine("X trigger: " + x + " : " + lastPosition.X + " ... " + y + " : " + lastPosition.Y);
				}
				if (Math.Abs(y - lastPosition.Y) > 4.0) {
					if (y < lastPosition.Y) {
						this.direction = Direction.Up;
						//Console.WriteLine("up");
					} else {
						this.direction = Direction.Down;
						//Console.WriteLine("down");
					}
					//Console.WriteLine("Y trigger: " + x + " : " + lastPosition.X + " ... " + y + " : " + lastPosition.Y);
				}
			}
			lastPosition = new Point(x, y);
		}

		public Direction NextDirection { get { return nextDirection; } set { nextDirection = value; } }
		
		public int X { get { return (int)Math.Round(x); } }
		public int Y { get { return (int)Math.Round(y); } }
		public int ImgX { get { return X - 7; } }
		public int ImgY { get { return Y - 7; } }
		public float Xf { get { return x; } }
		public float Yf { get { return y; } }

		public Entity(int x, int y, GameState gameState) {
			this.direction = Direction.Left;
			this.NextDirection = Direction.Left;
			this.x = x;
			this.y = y;
			this.gameState = gameState;
			this.node = gameState.Map.GetNode(x, y);			
		}

		protected bool checkDirection(Direction checkDirection){
			switch( checkDirection ) {
				case Direction.Up:
					if( node.Up.Type != Node.NodeType.Wall ) {
						return true;
					}
					break;
				case Direction.Down:
					if( node.Down.Type != Node.NodeType.Wall ) {						
						return true;
					}
					break;
				case Direction.Left:
					if( node.Left.Type != Node.NodeType.Wall ) {						
						return true;
					}
					break;
				case Direction.Right:
					if( node.Right.Type != Node.NodeType.Wall ) {						
						return true;
					}
					break;
			}
			return false;
		}

		protected bool setNextDirection() {
			if( nextDirection == direction )
				return false;
			switch( nextDirection ) {
				case Direction.Up: 
					if( node.Up.Type != Node.NodeType.Wall ) { 
						direction = nextDirection; 
						this.x = node.CenterX; 
						this.y = node.CenterY; 
						node = node.Up; 
						return true; 
					}
					break;
				case Direction.Down: 
					if( node.Down.Type != Node.NodeType.Wall ) { 
						direction = nextDirection; 
						this.x = node.CenterX; 
						this.y = node.CenterY; 
						node = node.Down; 
						return true; 
					} 
					break;
				case Direction.Left: 
					if( node.Left.Type != Node.NodeType.Wall ) { 
						direction = nextDirection; 
						this.x = node.CenterX; 
						this.y = node.CenterY; 
						node = node.Left; 
						return true; 
					} 
					break;
				case Direction.Right: 
					if( node.Right.Type != Node.NodeType.Wall ) { 
						direction = nextDirection; 
						this.x = node.CenterX; 
						this.y = node.CenterY; 
						node = node.Right; 
						return true; 
					} 
					break;
			}
			return false;
		}

		protected virtual void ProcessNode() { }

        protected virtual void ProcessNodeSimulated() { }

        public virtual void MoveSimulated()
        {
            float curSpeed = Speed;
            Ghosts.Ghost ghost = this as Ghosts.Ghost;
            if (ghost != null)
            {
                if (!ghost.Entered)
                {
                    // going back speed
                }
                else if (gameState.Map.Tunnels[node.Y] && (node.X <= 1 || node.X >= Map.Width - 2))
                {
                    curSpeed = Ghosts.Ghost.TunnelSpeed;
                }
                else if (!ghost.Chasing)
                {
                    curSpeed = Ghosts.Ghost.FleeSpeed;
                }
            }

            //Console.WriteLine(direction + " << going " + node.X + "," + node.Y + ", " + node.Type + " ::: " + X + "," + Y + " : " + node.CenterX + "," + node.CenterY);
            switch (direction)
            {
                case Direction.Up:
                    if (this.y > node.CenterY)
                    { // move towards target node if we haven't reached it yet
                        this.y -= curSpeed;
                    }
                    if (this.y <= node.CenterY)
                    {
                        ProcessNodeSimulated();
                        if (!setNextDirection())
                        { // try to change direction
                            if (node.Up.Type == Node.NodeType.Wall)
                            {
                                this.y = node.CenterY;
                            }
                            else
                            {
                                node = node.Up;
                            }
                        };
                    }
                    break;
                case Direction.Down:
                    if (this.y < node.CenterY)
                    {
                        this.y += curSpeed;
                    }
                    if (this.y >= node.CenterY)
                    {
                        ProcessNodeSimulated();
                        if (!setNextDirection())
                        {
                            if (node.Down.Type == Node.NodeType.Wall)
                            {
                                this.y = node.CenterY;
                            }
                            else
                            {
                                node = node.Down;
                            }
                        };
                    }
                    break;
                case Direction.Left:
                    if (node.X == Map.Width - 1 && this.x < 10)
                    { // check wrapping round // buggy on map 3
                        this.x -= curSpeed;
                        if (this.x < 0)
                            this.x = gameState.Map.PixelWidth + this.x;
                    }
                    else
                    {
                        if (this.x > node.CenterX)
                        {
                            this.x -= curSpeed;
                        }
                        if (this.x <= node.CenterX)
                        {
                            ProcessNodeSimulated();
                            if (!setNextDirection())
                            {
                                if (node.Left.Type == Node.NodeType.Wall)
                                {
                                    this.x = node.CenterX;
                                }
                                else
                                {
                                    node = node.Left;
                                }
                            };
                        }
                    }
                    break;
                case Direction.Right:
                    if (node.X == 0 && this.x > gameState.Map.PixelWidth - 10)
                    { // check wrapping round // buggy on map 3
                        this.x += curSpeed;
                        if (this.x > gameState.Map.PixelWidth)
                            this.x = this.x - gameState.Map.PixelWidth;
                    }
                    else
                    {
                        if (this.x < node.CenterX)
                        {
                            this.x += curSpeed;
                        }
                        if (this.x >= node.CenterX)
                        {
                            ProcessNodeSimulated();
                            if (!setNextDirection())
                            {
                                if (node.Right.Type == Node.NodeType.Wall)
                                {
                                    this.x = node.CenterX;
                                }
                                else
                                {
                                    node = node.Right;
                                }
                            };
                        }
                    }
                    break;
                case Direction.None:
                    setNextDirection();
                    break;
            }
        }

		public virtual void Move() {
			float curSpeed = Speed;
			Ghosts.Ghost ghost = this as Ghosts.Ghost;
			if( ghost != null ){
				if( !ghost.Entered ) {
					// going back speed
				}
				else if( gameState.Map.Tunnels[node.Y] && (node.X <= 1 || node.X >= Map.Width - 2) ) {
					curSpeed = Ghosts.Ghost.TunnelSpeed;
				} else if( !ghost.Chasing ) {
					curSpeed = Ghosts.Ghost.FleeSpeed;
				}
			}

			//Console.WriteLine(direction + " << going " + node.X + "," + node.Y + ", " + node.Type + " ::: " + X + "," + Y + " : " + node.CenterX + "," + node.CenterY);
			switch( direction ) {				
				case Direction.Up:
					if( this.y > node.CenterY ) { // move towards target node if we haven't reached it yet
						this.y -= curSpeed;
					}
					if( this.y <= node.CenterY ) {
						ProcessNode();
						if( !setNextDirection() ) { // try to change direction
							if( node.Up.Type == Node.NodeType.Wall ) { 
								this.y = node.CenterY;
							} else { 
								node = node.Up;
							}
						};	
					}
					break;
				case Direction.Down:
					if( this.y < node.CenterY ) {
						this.y += curSpeed;
					}
					if( this.y >= node.CenterY ) {
						ProcessNode();
						if( !setNextDirection() ) {
							if( node.Down.Type == Node.NodeType.Wall ) {
								this.y = node.CenterY;
							} else {
								node = node.Down;
							}
						};							
					}
					break;
				case Direction.Left:
					if( node.X == Map.Width - 1 && this.x < 10 ) { // check wrapping round // buggy on map 3
						this.x -= curSpeed;
						if( this.x < 0 )
							this.x = gameState.Map.PixelWidth + this.x;
					} else {
						if( this.x > node.CenterX ) {
							this.x -= curSpeed;
						}
						if( this.x <= node.CenterX ) {
							ProcessNode();
							if( !setNextDirection() ){
								if( node.Left.Type == Node.NodeType.Wall ) {
									this.x = node.CenterX;
								} else {
									node = node.Left;
								}
							};
						}
					}
					break;
				case Direction.Right:					
					if( node.X == 0 && this.x > gameState.Map.PixelWidth - 10 ) { // check wrapping round // buggy on map 3
						this.x += curSpeed;
						if( this.x > gameState.Map.PixelWidth )
							this.x = this.x - gameState.Map.PixelWidth;
					} else {
						if( this.x < node.CenterX ) {
							this.x += curSpeed;
						}
						if( this.x >= node.CenterX ) {
							ProcessNode();
							if( !setNextDirection() ) {
								if( node.Right.Type == Node.NodeType.Wall ) {
									this.x = node.CenterX;
								} else {
									node = node.Right;
								}
							};
						}
					}
					break;
				case Direction.None:
					setNextDirection();
					break;
			}
		}

		public float Distance(Entity entity) {
			return (float)Math.Sqrt(Math.Pow(X - entity.X, 2) + Math.Pow(Y - entity.Y, 2));
		}

		public bool IsBelow(Entity entity){
			if( Y <= entity.Y ) 
				return true;
			return false;
		}

		public bool IsAbove(Entity entity) {
			if( Y >= entity.Y )
				return true;
			return false;
		}

		public bool IsLeft(Entity entity) {
			if( X >= entity.X )
				return true;
			return false;
		}

		public bool IsRight(Entity entity) {
			if( X <= entity.X )
				return true;
			return false;
		}

		public List<Direction> PossibleDirections() {
			List<Direction> possible = new List<Direction>();
			if( Node.Up.Type != Node.NodeType.Wall ) possible.Add(Direction.Up);
			if( Node.Down.Type != Node.NodeType.Wall ) possible.Add(Direction.Down);
			if( Node.Left.Type != Node.NodeType.Wall ) possible.Add(Direction.Left);
			if( Node.Right.Type != Node.NodeType.Wall ) possible.Add(Direction.Right);
			return possible;
		}

		public bool PossibleDirection(Direction d) {
			switch(d){
				case Direction.Up: return Node.Up.Type != Node.NodeType.Wall;
				case Direction.Down: return Node.Down.Type != Node.NodeType.Wall;
				case Direction.Left: return Node.Left.Type != Node.NodeType.Wall;
				case Direction.Right: return Node.Right.Type != Node.NodeType.Wall;
			}
			return false;
		}

		public Direction InverseDirection(Direction d) {
			switch( d ) {
				case Direction.Up: return Direction.Down;
				case Direction.Down: return Direction.Up;
				case Direction.Left: return Direction.Right;
				case Direction.Right: return Direction.Left;
			}
			return Direction.None;
		}

		public virtual void Draw(Graphics g, Image sprites) {
			g.DrawRectangle(new Pen(Brushes.Red), new Rectangle(ImgX, ImgY, Width, Height));
		}
	}

	public enum Direction { Up = 0, Down = 1, Left = 2, Right = 3, None = 4, Stall = 5 };
}
