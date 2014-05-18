using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Pacman.Simulator.Ghosts
{
    [Serializable()]
	public class Pink : Ghost, ICloneable
	{
		public const int StartX = 111, StartY = 118;
		private const int firstWaitToEnter = -2, secondWaitToEnter = 9;

		public Pink(int x, int y, GameState gameState)
			: base(x, y, gameState) {
			this.name = "Pink";
			this.drawOffset = Height;
			ResetPosition();
		}

		public override void PacmanDead() {			
			ResetPosition();
			waitToEnter = secondWaitToEnter;
		}

		public override void ResetPosition() {
			x = StartX;
			y = StartY;
			waitToEnter = firstWaitToEnter;
			direction = Direction.Down;
			base.ResetPosition();			
		}

		public override void  Move()
		{
			if( Distance(GameState.Pacman) > randomMoveDist && GameState.Random.Next(0, randomMove) == 0 ) {
				MoveRandom();
			} else {
				// testing pinky (stupid and mostly a bother?)
				if( Distance(GameState.Pacman) > 120 || GameState.Pacman.Direction == Direction.None ) {
					// should probably do something else for none! (read gamefaq, but good enough for now)
					MoveAsRed();
				} else {
					// this is pretty stupid ... basicly we just always try to get in front
					switch( GameState.Pacman.Direction ) {
						case Direction.Up:
							if( IsAbove(GameState.Pacman) ) {
								TryGo(Direction.Down);
								if( IsLeft(GameState.Pacman) ) {
									TryGo(Direction.Right);
									TryGo(Direction.Left);
								} else {
									TryGo(Direction.Left);
									TryGo(Direction.Right);
								}
								TryGo(Direction.Up);
							} else {
								TryGo(Direction.Up);
								if( IsLeft(GameState.Pacman) ) {
									TryGo(Direction.Right);
									TryGo(Direction.Left);
								} else {
									TryGo(Direction.Left);
									TryGo(Direction.Right);
								}
								TryGo(Direction.Down);
							}
							break;
						case Direction.Down:
							if( IsBelow(GameState.Pacman) ) {
								TryGo(Direction.Up);
								if( IsLeft(GameState.Pacman) ) {
									TryGo(Direction.Right);
									TryGo(Direction.Left);
								} else {
									TryGo(Direction.Left);
									TryGo(Direction.Right);
								}
								TryGo(Direction.Down);
							} else {
								TryGo(Direction.Down);
								if( IsLeft(GameState.Pacman) ) {
									TryGo(Direction.Right);
									TryGo(Direction.Left);
								} else {
									TryGo(Direction.Left);
									TryGo(Direction.Right);
								}
								TryGo(Direction.Up);
							}
							break;
						case Direction.Left:
							if( IsLeft(GameState.Pacman) ) {
								TryGo(Direction.Right);
								if( IsBelow(GameState.Pacman) ) {
									TryGo(Direction.Up);
									TryGo(Direction.Down);
								} else {
									TryGo(Direction.Down);
									TryGo(Direction.Up);
								}
								TryGo(Direction.Left);
							} else {
								TryGo(Direction.Left);
								if( IsBelow(GameState.Pacman) ) {
									TryGo(Direction.Up);
									TryGo(Direction.Down);
								} else {
									TryGo(Direction.Down);
									TryGo(Direction.Up);
								}
								TryGo(Direction.Right);
							}
							break;
						case Direction.Right:
							if( IsRight(GameState.Pacman) ) {
								TryGo(Direction.Left);
								if( IsBelow(GameState.Pacman) ) {
									TryGo(Direction.Up);
									TryGo(Direction.Down);
								} else {
									TryGo(Direction.Down);
									TryGo(Direction.Up);
								}
								TryGo(Direction.Right);
							} else {
								TryGo(Direction.Right);
								if( IsBelow(GameState.Pacman) ) {
									TryGo(Direction.Up);
									TryGo(Direction.Down);
								} else {
									TryGo(Direction.Down);
									TryGo(Direction.Up);
								}
								TryGo(Direction.Left);
							}
							break;
					}
				}
			}
			base.Move();
		}

        #region ICloneable Members

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public Pink Clone()
        {
            Pink _temp = (Pink)this.MemberwiseClone();
            _temp.Node = node.Clone();

            return _temp;
        }

        #endregion
    }
}
