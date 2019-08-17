using System;
using System.Collections.Generic;
using System.Text;
using Pacman.Simulator;

namespace PacmanAI
{
	public class RandomPac : BasePacman
	{
		public RandomPac() : base("RandomPac") {			
		}

		public override Direction Think(GameState gs) {
			List<Direction> possible = gs.Pacman.PossibleDirections();			
			if( possible.Count > 0 ) {
				int select = GameState.Random.Next(0, possible.Count);
				if( possible[select] != gs.Pacman.InverseDirection(gs.Pacman.Direction) )
					return possible[select];
			}
			return Direction.None;
		}
	}
}
