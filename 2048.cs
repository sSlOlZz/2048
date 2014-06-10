/* 
    2048 Game implementation by Andrii Zhuk
    
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace _2048
{
    public class TileColorizer
    {
        private readonly Dictionary<int, ConsoleColor> colors = new Dictionary<int, ConsoleColor>
        {
            {2, ConsoleColor.Black},
            {4, ConsoleColor.Gray},
            {8, ConsoleColor.Green},
            {16, ConsoleColor.DarkGreen},
            {32, ConsoleColor.Magenta},
            {64, ConsoleColor.DarkMagenta},
            {128, ConsoleColor.Cyan},
            {256, ConsoleColor.DarkCyan},
            {512, ConsoleColor.Red},
            {1024, ConsoleColor.DarkRed},
            {2048, ConsoleColor.Blue},
            {4096, ConsoleColor.DarkBlue},
            {8192, ConsoleColor.Yellow},
            {16384, ConsoleColor.DarkYellow}
        };

        public ConsoleColor GetColorByValue(int value)
        {
            ConsoleColor color;
            if (colors.TryGetValue(value, out color))
                return color;
            return ConsoleColor.Black;
        }
    }

    public class Tile
    {
        public int Value { get; set; }
        public SByte X { get; set; }
        public SByte Y { get; set; }
    }

    public class Board
    {
        private readonly SByte m_size;
        private List<Tile> m_board;
        private readonly Random m_random = new Random();
        private readonly Stack<List<Tile>> m_undoboards = new Stack<List<Tile>>();

        public int Score { get; private set; }
        public int StepsCount { get; private set; }

        public Board(SByte size = 4)
        {
            m_size = size;
            m_board = new List<Tile>(m_size * m_size);
            sbyte y = 0;
            for (SByte x = 0; x < m_size * m_size; x++)
            {
                if ((x % m_size == 0) && (x > 0))
                    y++;

                m_board.Add(new Tile { X = y, Y = (sbyte)(x - (sbyte)(y * m_size)) });
            }
            NextFill();
        }

        public List<Tile> DeepCopy()
        {
            var board = new List<Tile>(m_size * m_size);
            foreach (var tile in m_board)
                board.Add(new Tile() { X = tile.X, Y = tile.Y, Value = tile.Value });
            return board;
        }

        bool HasEqualInVector(Func<Tile, int> predicate)
        {
            for (int c = 0; c < m_size; c++)
            {
                var col1 = new List<Tile>(m_board.Where(x => predicate(x) == c));
                for (int i = 0; i < m_size - 1; i++)
                    if (col1[i].Value == col1[i + 1].Value)
                        return true;
            }
            return false;
        }

        public bool NextStepAvailable()
        {
            return GetEmptyTiles().Any() || HasEqualInVector(x => x.Y) || HasEqualInVector(x => x.X);
        }

        IEnumerable<Tile> GetEmptyTiles()
        {
            return m_board.Where(x => x.Value == 0);
        }

        bool NextFill()
        {
            var emptyTiles = GetEmptyTiles();
            int emptyCount = emptyTiles.Count();
            if (emptyCount == 0)
                return false;
            int cellNumber = m_random.Next(emptyCount);
            var tile = emptyTiles.Skip(cellNumber).First();
            tile.Value = 2;
            return true;
        }

        public Tile[,] To2DArray()
        {
            var output = new Tile[m_size, m_size];

            foreach (var tile in m_board)
            {
                output[tile.X, tile.Y] = tile;
            }
            return output;
        }

        private void Move(Func<Tile, int> predicate, bool up)
        {
            m_undoboards.Push(DeepCopy());
            StepsCount = StepsCount + 1;
            for (int c = 0; c < m_size; c++)
            {
                var col1 = new List<Tile>(m_board.Where(x => predicate(x) == c));

                for (int i = 0; i < m_size - 1; i++)
                    if (up)
                        MoveP(col1);
                    else
                        MoveN(col1);
            }
            NextFill();
        }


        private void MoveP(List<Tile> col1)
        {
            for (sbyte y1 = 0; y1 < m_size - 1; y1++)
            {
                if (col1[y1].Value == col1[y1 + 1].Value || col1[y1].Value == 0)
                {
                    if (col1[y1].Value == col1[y1 + 1].Value)
                        Score += col1[y1].Value;
                    col1[y1].Value = col1[y1].Value + col1[y1 + 1].Value;
                    col1[y1 + 1].Value = 0;
                }
            }
        }

        private void MoveN(List<Tile> col1)
        {
            for (sbyte y1 = (sbyte)(m_size - 1); y1 > 0; y1--)
            {
                if (col1[y1].Value == col1[y1 - 1].Value || col1[y1].Value == 0)
                {
                    if (col1[y1].Value == col1[y1 - 1].Value)
                        Score += col1[y1].Value;
                    col1[y1].Value = col1[y1].Value + col1[y1 - 1].Value;
                    col1[y1 - 1].Value = 0;
                }
            }
        }

        public void MoveUp()
        {
            Move(x => x.Y, true);
        }

        public void MoveDown()
        {
            Move(x => x.Y, false);
        }

        public void MoveLeft()
        {
            Move(x => x.X, true);
        }

        public void MoveRight()
        {
            Move(x => x.X, false);
        }


        public void Undo()
        {
            if (StepsCount < 1)
                return;
            m_board = new List<Tile>(m_undoboards.Pop().ToArray());
            StepsCount = StepsCount - 1;
        }
    }

    public enum NextStepCommand
    {
        Up,
        Down,
        Right,
        Left,
        Break,
        Undo,
        Nop
    }

    public interface IGameEngine
    {
        bool IsAI();
	NextStepCommand GetNextStep(Tile[,] board);
    }

    public sealed class ConsoleUserEngine : IGameEngine
    {
	public bool IsAI()
	{
	    return false;
	}

        public NextStepCommand GetNextStep(Tile[,] board)
        {
            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    return NextStepCommand.Up;
                case ConsoleKey.DownArrow:
                    return NextStepCommand.Down;
                case ConsoleKey.LeftArrow:
                    return NextStepCommand.Left;
                case ConsoleKey.RightArrow:
                    return NextStepCommand.Right;
                case ConsoleKey.Q:
                    return NextStepCommand.Break;
                case ConsoleKey.Z:
                    if ((key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control)
                        return NextStepCommand.Break;
                    break;
            }
            return NextStepCommand.Nop;
        }
    }

    public class AINaiveEngine : IGameEngine
    {
        private Random m_random;
        public AINaiveEngine()
        {
            m_random = new Random();
        }

	public bool IsAI()
	{
	   return true;
	}

        public NextStepCommand GetNextStep(Tile[,] board)
        {
            
            var command = NextStepCommand.Nop;
            switch (m_random.Next() % 4)
            {
                case 0 : command = NextStepCommand.Up;
                    break;
                case 1: command = NextStepCommand.Down;
                    break;
                case 2: command = NextStepCommand.Right;
                    break;
                case 3: command = NextStepCommand.Left;
                    break;
            }
            Thread.Sleep(300);
            return command;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            IGameEngine engine = null;
            while (engine == null)
            {
                Console.Clear();
                Console.WriteLine("Select mode:");
                Console.WriteLine("1. User game");
                Console.WriteLine("2. AI game");
                var key = Console.ReadKey(true).KeyChar.ToString();
                if (key == "1")
                    engine = new ConsoleUserEngine();
                if (key == "2")
                    engine = new AINaiveEngine();
            }
            Console.Clear();
            var board = new Board();
            var colorizer = new TileColorizer();
            bool running = true;
            Console.BackgroundColor = ConsoleColor.White;
            PrintBoard(board, colorizer);

            while (running)
            {
                NextStepCommand command = engine.GetNextStep(board.To2DArray());

                switch (command)
                {
                    case NextStepCommand.Up : 
                        board.MoveUp();
                        break;
                    case NextStepCommand.Down:
                        board.MoveDown();
                        break;
                    case NextStepCommand.Right:
                        board.MoveRight();
                        break;
                    case NextStepCommand.Left:
                        board.MoveLeft();
                        break;
                    case NextStepCommand.Undo:
                        if (!engine.IsAI())
				board.Undo();
                        break;
                    case NextStepCommand.Break:
                        running = false;
                        break;
                }

                if (running)
                    PrintBoard(board, colorizer);
                if (!board.NextStepAvailable())
                {
                    Console.WriteLine("Game over!");
                    running = false;
                }
		if (running && engine.IsAI() && board.StepsCount > 1000 )
		{	
		    Console.WriteLine("Halt! 1000 step limit reached!");
		    running = false;
		}
            }
        }

        private static void PrintBoard(Board board, TileColorizer colorizer)
        {
            Console.Clear();
            Console.WriteLine("Used {0,4} steps, score {1,5}", board.StepsCount, board.Score);
            var arr = board.To2DArray();
            int rowLength = arr.GetLength(0);
            int colLength = arr.GetLength(1);

            for (int i = 0; i < rowLength; i++)
            {
                for (int j = 0; j < colLength; j++)
                {
                    Console.ForegroundColor = colorizer.GetColorByValue(arr[i, j].Value);
                    Console.Write("{0,4} ", arr[i, j].Value);
                }
                Console.Write(Environment.NewLine + Environment.NewLine);
            }
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("Press q to exit, use arrow keys for game, ctrl+z to undo");
        }
    }
}
