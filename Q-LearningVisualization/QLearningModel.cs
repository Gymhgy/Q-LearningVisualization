using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_LearningVisualization {
    class QLearningModel {

        public double Discount { get; }
        public double LearningRate { get; }

        public QLearningModel(double discount, double learningRate, double livingPenalty) {
            Discount = discount;
            LearningRate = learningRate;
            for(int i = 0; i < environment.Length; i++) {
                for(int j = 0; j < environment[0].Length; j++) {
                    if (environment[i][j] == 0)
                        environment[i][j] = -livingPenalty;
                }
            }
        }

        //our environment
        //each index represents a state and the reward you get for performing a certain action in the state
        //nth index in this array represents the cell new[] {n/4, n%4} in the maze counting from top left
        public double[][] environment = new double[][]
        {
            //(0,0) - (0,2)
            new double[] {0,0,0,0},
            new double[] {0,0,0,0},
            new double[] {0,0,1,0},
            //Terminal state (0,3) end
            new double[] {0,0,0,0},

            //(1,0) - (1,2)
            new double[] {0,0,0,0},
            new double[] {0,0,0,0}, //wall
            new double[] {0,0,-1,0},
            //Terminal state (1,3) firepit
            new double[] {0,0,0,0},

            //(2,0) - (2,3)
            new double[] {0,0,0,0},
            new double[] {0,0,0,0},
            new double[] {0,0,0,0},
            new double[] {0,-1,0,0}
        };

        //bottom left corner, can be anywhere i set it
        public int Position { get; private set; } = START;
        public (int oldState, int newState, double reward, Action action, double newQ) Iterate() {
            if (!GameOver()) {
                Action[] actions = new[] { Action.Left, Action.Up, Action.Right, Action.Down };
                Action action = actions[rng.Next(4)];
                int next = TakeAction(Position, action);
                int idx = Array.IndexOf(actions, action);
                double reward = environment[Position][idx];
                //Recalculate Q-Value for this action in this state
                qtable[Position][idx] = qtable[Position][idx] +
                    LearningRate * (reward + Discount * qtable[next].Max() - qtable[Position][idx]);
                var newQ = qtable[Position][idx];
                var oldPos = Position;
                Position = next;
                return (oldPos, next, reward, action, newQ);
            }
            else throw new GameOverException();
        }

        public void Reset() {
            Position = START;
        }

        const int FIREPIT = 7;
        const int GOAL = 3;
        const int WALL = 5;
        const int START = 8;

        private static readonly Random rng = new Random();

        //With stochatisity built in!
        private static int TakeAction(int state, Action action) {
            int luck = rng.Next(10);
            List<Action> accidents = new List<Action>();
            int oldState = state;
            if(action == Action.Down || action == Action.Up) {
                accidents.Add(Action.Left);
                accidents.Add(Action.Right);
            }
            if (action == Action.Left || action == Action.Right) {
                accidents.Add(Action.Down);
                accidents.Add(Action.Up);
            }
            //80% of times
            if (luck < 8) {
                state += (int)action;
            }
            //10%
            if(luck == 8) {
                state += (int)accidents[0];
            }
            //10%
            if(luck == 9) {
                state += (int)accidents[1];
            }
            //make sure we're not in invalid state
            if(state == WALL || state < 0 || state > 11) {
                state = oldState;
            }
            //Make sure he doesn't go off the edge of the map trying to go right or left
            if(oldState / 4 != state / 4 && (oldState - 1 == state || oldState + 1 == state)) {
                state = oldState;
            }
            return state;
        }

        //Each index in the q-table corresponds to a state
        public double[][] qtable = new double[][] {
            new double[] {0,0,0,0},
            new double[] {0,0,0,0},
            new double[] {0,0,0,0},
            new double[] {0,0,0,0},
            new double[] {0,0,0,0},
            new double[] {0,0,0,0},
            new double[] {0,0,0,0},
            new double[] {0,0,0,0},
            new double[] {0,0,0,0},
            new double[] {0,0,0,0},
            new double[] {0,0,0,0},
            new double[] {0,0,0,0}
        };

        public bool GameOver() {
            return Position == FIREPIT || Position == GOAL;
        }
    }

    //Values are to transition between states
    public enum Action { Left = -1, Up = -4, Right = +1, Down = +4 }

    public class GameOverException : Exception { }
}
