using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Othello2xd
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int BoardSize = 8;
        private const int CellSize = 50;
        private string[,] boardState = new string[BoardSize, BoardSize];
        private string currentPlayer = "White";

        public MainWindow()
        {
            InitializeComponent();
            DrawBoard();
            InitializeBoard();
        }
        private void InitializeBoard()
        {
            // Inicializar el estado del tablero con las fichas iniciales
            for (int i = 0; i < BoardSize; i++)
                for (int j = 0; j < BoardSize; j++)
                    boardState[i, j] = null;

            boardState[3, 3] = "White";
            boardState[4, 4] = "White";
            boardState[3, 4] = "Black";
            boardState[4, 3] = "Black";
        }

        private void DrawBoard()
        {
            GameCanvas.Children.Clear();

            // Dibujar el tablero y las fichas
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    var rect = new Rectangle
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Stroke = Brushes.Black,
                        Fill = Brushes.Green
                    };
                    Canvas.SetLeft(rect, col * CellSize);
                    Canvas.SetTop(rect, row * CellSize);
                    GameCanvas.Children.Add(rect);

                    // Dibujar las fichas si existen
                    if (boardState[row, col] != null)
                    {
                        DrawPiece(row, col, boardState[row, col]);
                    }
                }
            }
        }

        private void DrawPiece(int row, int col, string color)
        {
            var piece = new Ellipse
            {
                Width = CellSize - 10,
                Height = CellSize - 10,
                Fill = color == "Black" ? Brushes.Black : Brushes.White
            };
            Canvas.SetLeft(piece, col * CellSize + 5);
            Canvas.SetTop(piece, row * CellSize + 5);
            GameCanvas.Children.Add(piece);
        }

        private void GameCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(GameCanvas);
            int col = (int)(position.X / CellSize);
            int row = (int)(position.Y / CellSize);

            if (IsValidMove(row, col, currentPlayer))
            {
                MakeMove(row, col, currentPlayer);
                currentPlayer = "Black"; // Cambia el turno a la IA
                DrawBoard();

                // Movimiento de la IA usando Minimax
                AIPlay();
                currentPlayer = "White"; // Cambia el turno al jugador
                DrawBoard();
            }
        }

        private bool IsValidMove(int row, int col, string player)
        {
            if (row < 0 || row >= BoardSize || col < 0 || col >= BoardSize || boardState[row, col] != null)
                return false;

            foreach (var direction in GetDirections())
            {
                if (IsDirectionValid(row, col, direction[0], direction[1], player))
                    return true;
            }

            return false;
        }

        private bool IsDirectionValid(int row, int col, int dRow, int dCol, string player)
        {
            int i = row + dRow, j = col + dCol;
            bool hasOpponentPiece = false;
            string opponent = player == "White" ? "Black" : "White";

            while (i >= 0 && i < BoardSize && j >= 0 && j < BoardSize)
            {
                if (boardState[i, j] == null) return false;

                if (boardState[i, j] == opponent)
                {
                    hasOpponentPiece = true;
                }
                else if (boardState[i, j] == player)
                {
                    return hasOpponentPiece;
                }

                i += dRow;
                j += dCol;
            }

            return false;
        }

        private void MakeMove(int row, int col, string player)
        {
            boardState[row, col] = player;

            foreach (var direction in GetDirections())
            {
                if (IsDirectionValid(row, col, direction[0], direction[1], player))
                {
                    FlipPieces(row, col, direction[0], direction[1], player);
                }
            }
        }

        private void FlipPieces(int row, int col, int dRow, int dCol, string player)
        {
            int i = row + dRow, j = col + dCol;
            string opponent = player == "White" ? "Black" : "White";

            while (i >= 0 && i < BoardSize && j >= 0 && j < BoardSize && boardState[i, j] == opponent)
            {
                boardState[i, j] = player;
                i += dRow;
                j += dCol;
            }
        }

        private void AIPlay()
        {
            var bestMove = GetBestMove("Black");
            if (bestMove != null)
            {
                MakeMove(bestMove.Row, bestMove.Col, "Black");
            }
        }

        private Move GetBestMove(string player)
        {
            int depth = 3;
            var validMoves = GetValidMoves(player);
            Move bestMove = null;
            int bestScore = int.MinValue;

            foreach (var move in validMoves)
            {
                string[,] tempBoard = CopyBoard();
                MakeMove(move.Row, move.Col, player);
                int score = Minimax(tempBoard, depth - 1, false, player);
                UndoMove(move);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove;
        }

        private int Minimax(string[,] board, int depth, bool isMaximizing, string player)
        {
            if (depth == 0)
                return EvaluateBoard(board);

            string opponent = player == "White" ? "Black" : "White";
            var moves = GetValidMoves(isMaximizing ? player : opponent);

            if (moves.Count == 0)
                return EvaluateBoard(board);

            int bestScore = isMaximizing ? int.MinValue : int.MaxValue;

            foreach (var move in moves)
            {
                MakeMove(move.Row, move.Col, isMaximizing ? player : opponent);
                int score = Minimax(board, depth - 1, !isMaximizing, player);
                UndoMove(move);

                bestScore = isMaximizing ? Math.Max(bestScore, score) : Math.Min(bestScore, score);
            }

            return bestScore;
        }

        private int EvaluateBoard(string[,] board)
        {
            int score = 0;

            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    if (board[row, col] == "Black")
                        score += IsCorner(row, col) ? 5 : 1;
                    else if (board[row, col] == "White")
                        score -= IsCorner(row, col) ? 5 : 1;
                }
            }

            return score;
        }

        private bool IsCorner(int row, int col) =>
            (row == 0 && col == 0) || (row == 0 && col == BoardSize - 1) ||
            (row == BoardSize - 1 && col == 0) || (row == BoardSize - 1 && col == BoardSize - 1);

        private List<Move> GetValidMoves(string player)
        {
            var moves = new List<Move>();
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    if (IsValidMove(row, col, player))
                        moves.Add(new Move(row, col));
                }
            }
            return moves;
        }

        private string[,] CopyBoard()
        {
            var newBoard = new string[BoardSize, BoardSize];
            Array.Copy(boardState, newBoard, boardState.Length);
            return newBoard;
        }

        private void UndoMove(Move move) => boardState[move.Row, move.Col] = null;

        private IEnumerable<int[]> GetDirections() =>
            new List<int[]> { new int[] { -1, -1 }, new int[] { -1, 0 }, new int[] { -1, 1 },
                              new int[] { 0, -1 },                  new int[] { 0, 1 },
                              new int[] { 1, -1 }, new int[] { 1, 0 }, new int[] { 1, 1 } };

        private class Move
        {
            public int Row { get; }
            public int Col { get; }
            public Move(int row, int col)
            {
                Row = row;
                Col = col;
            }
        }
    }
}