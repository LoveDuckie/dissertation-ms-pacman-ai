using System;
using System.Collections.Generic;
using System.Text;
using Pacman.Simulator;

namespace Pacman.Simulator
{
    /// <summary>
    /// A pacman controller that is intended to make random moves
    /// just so that there is something being simulated in the given
    /// game state
    /// </summary>
    public class SimRandomPac : BasePacman
    {
        public SimRandomPac()
            : base("SimRandomPac")
        {
        }

        public override Direction Think(GameState gs)
        {
            List<Direction> possible = gs.Pacman.PossibleDirections();
            if (possible.Count > 0)
            {
                int select = GameState.Random.Next(0, possible.Count);
                if (possible[select] != gs.Pacman.InverseDirection(gs.Pacman.Direction))
                    return possible[select];
            }
            return Direction.None;
        }
    }
}
