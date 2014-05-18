using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Linq;

using Pacman.Simulator;
using Pacman.Simulator.Ghosts;

using System.Reflection;
using System.Threading;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Utilities;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Bson;

namespace Pacman.Simulator
{
    public class SimGreedyRandom : BasePacman
    {
        public SimGreedyRandom()
            : base("SimGreedyRandom")
        {

        }

        public override void EatenByGhost()
        {
            base.EatenByGhost();
        }

        public override void EatGhost()
        {
            base.EatGhost();
        }

        public override void LevelCleared()
        {
            base.LevelCleared();
        }

        /// <summary>
        /// Called on every tick
        /// </summary>
        /// <param name="gs">The game state of object that is passed to it</param>
        /// <returns>The low level direction that we are heading in</returns>
        public override Direction Think(GameState gs)
        {
            // Determine if the current position is a junction
            if (IsJunction(gs.Pacman.Node.X, gs.Pacman.Node.Y, gs))
            {
                return GetHighestPillCountDirection(gs);
            }
            else
            {
                return Direction.None;
            }
        }

        /// <summary>
        /// Return the direction that has the highest amount of pills
        /// </summary>
        /// <param name="gs">The current gamestate object.</param>
        /// <returns>Returns the direction that we should follow.</returns>
        public static Direction GetHighestPillCountDirection(GameState gs)
        {
            int _highestDirectionCount = 0;
            Direction _bestDirection = Direction.None;

            // Loop through the possible directions and determine which one has the highest pills on the tunnels
            foreach (var direction in gs.Pacman.PossibleDirections())
            {
                if (CountPillsDirection(direction, gs) > _highestDirectionCount)
                {
                    _highestDirectionCount = CountPillsDirection(direction, gs);
                    _bestDirection = direction;
                }
            }

            return _bestDirection;
        }

        /// <summary>
        /// For determining which direction will give the highest score to the agent in question
        /// </summary>
        /// <param name="pDirection">The direction that we are going to be looking</param>
        /// <param name="pGameState">The current game state in question</param>
        /// <returns>Returns how many pills are in the given direction until we hit a wall.</returns>
        public static int CountPillsDirection(Direction pDirection, GameState pGameState)
        {
            Node _currentPosition = pGameState.Pacman.Node;
            int _pillCount = 0;

            // Keep going in a certain direction until we determine whether or not that we have hit a wall.
            while (!HitWall(_currentPosition, pGameState, pDirection))
            {
                _currentPosition = _currentPosition.GetNeighbour(pDirection);

                // Determine whether or not the pill that we are looking at is either a power pill
                // or just a normal pill
                if (_currentPosition.Type == Node.NodeType.Pill ||
                    _currentPosition.Type == Node.NodeType.PowerPill)
                {
                    _pillCount++;
                }
            }

            return _pillCount;
        }
     
        // Determine whether or not the Pacman agent has hit a wall on the current version of the GameState object.
        public static bool HitWall(Node pCurrentPosition, GameState pGameState, Direction pDirection)
        {
            // Loop through the possible directions at the give node
            // If a direction is the same as the one that Pacman is going in
            // then we've hit a wall
            foreach (var item in Node.GetAllPossibleDirections(pCurrentPosition))
            {
                if (item == pDirection)
                    return false;
            }
            return true;
        }

        public static bool IsJunction(int pX, int pY, GameState pGameState)
        {
            // Check that the coordinates are valid
            if (pX < pGameState.Map.Nodes.GetLength(0) && pX > 0 &&
                pY < pGameState.Map.Nodes.GetLength(1) && pY > 0)
            {
                return pGameState.Map.Nodes[pX, pY].PossibleDirections.Count > 2;
            }

            return false;
        }
    
    }
}
