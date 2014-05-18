using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Pacman.Simulator.Ghosts
{
    [Serializable()]
	public class Red : Ghost, ICloneable
	{
		public const int StartX = 111, StartY = 93;

		public Red(int x, int y, GameState gameState)
			: base(x, y, gameState) {
			this.name = "Red";
			ResetPosition();
		}

		public override void PacmanDead() {
			waitToEnter = 0;
			ResetPosition();			
		}

		public override void ResetPosition() {
			x = StartX;
			y = StartY;
			waitToEnter = 0;
			direction = Direction.Left;
			base.ResetPosition();
			entered = true;
		}

		public override void Move() {
			if( Distance(GameState.Pacman) > randomMoveDist && GameState.Random.Next(0, randomMove) == 0 ) {
				MoveRandom();
			} else {
				MoveAsRed();
			}
			base.Move();
		}

        #region ICloneable Members

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public Red Clone()
        {
            Red _temp = (Red)this.MemberwiseClone();
            _temp.Node = node.Clone();
         
            return _temp;
        }

        #endregion
    }
}
