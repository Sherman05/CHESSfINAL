using System;
using System.Collections.Generic;
using System.Text;

namespace ChessT1
{
    /// <summary>
    /// Piece types in chess-T1.
    /// </summary>
    public enum PieceType
    {
        King,
        Konnet,
        Prince,
        Ritter,
        Knekht,
        VerKnekht,
        Razvedchik
    }

    /// <summary>
    /// Piece colors.
    /// </summary>
    public enum PieceColor
    {
        White,
        Black
    }

    /// <summary>
    /// Represents a single game piece on the board.
    /// </summary>
    public class Piece
    {
        private PieceColor _color;
        private PieceType _type;

        public Piece(PieceColor color, PieceType type)
        {
            _color = color;
            _type = type;
        }

        public PieceColor Color
        {
            get { return _color; }
            set { _color = value; }
        }

        public PieceType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public string ShortName
        {
            get { return PieceData.ShortNames[_type]; }
        }

        public string FullName
        {
            get { return PieceData.FullNames[_type]; }
        }

        public Piece Copy()
        {
            return new Piece(_color, _type);
        }

        public override string ToString()
        {
            string c = (_color == PieceColor.White) ? "\u0411" : "\u0427"; // Б or Ч
            return c + ShortName;
        }

        public override bool Equals(object obj)
        {
            Piece other = obj as Piece;
            if (other == null)
                return false;
            return _color == other._color && _type == other._type;
        }

        public override int GetHashCode()
        {
            return _color.GetHashCode() * 397 ^ _type.GetHashCode();
        }
    }

    /// <summary>
    /// Static piece data: names, initial position, castle cells, promotion logic.
    /// </summary>
    public static class PieceData
    {
        private static Dictionary<PieceType, string> _shortNames;
        private static Dictionary<PieceType, string> _fullNames;
        private static List<string> _whiteCastle;
        private static List<string> _blackCastle;
        private static List<string> _allCastle;

        static PieceData()
        {
            // Short names (Russian abbreviations)
            _shortNames = new Dictionary<PieceType, string>();
            _shortNames[PieceType.King] = "\u041A\u0440";           // Кр
            _shortNames[PieceType.Konnet] = "\u041A\u0442";         // Кт
            _shortNames[PieceType.Prince] = "\u041F\u0440";         // Пр
            _shortNames[PieceType.Ritter] = "\u0420\u0442";         // Рт
            _shortNames[PieceType.Knekht] = "\u041A\u043D";         // Кн
            _shortNames[PieceType.VerKnekht] = "\u0412\u041A";      // ВК
            _shortNames[PieceType.Razvedchik] = "\u0420\u043A";     // Рк

            // Full names (Russian)
            _fullNames = new Dictionary<PieceType, string>();
            _fullNames[PieceType.King] = "\u041A\u043E\u0440\u043E\u043B\u044C";                    // Король
            _fullNames[PieceType.Konnet] = "\u041A\u043E\u043D\u043D\u0435\u0442";                  // Коннет
            _fullNames[PieceType.Prince] = "\u041F\u0440\u0438\u043D\u0446";                        // Принц
            _fullNames[PieceType.Ritter] = "\u0420\u0438\u0442\u0442\u0435\u0440";                  // Риттер
            _fullNames[PieceType.Knekht] = "\u041A\u043D\u0435\u0445\u0442";                        // Кнехт
            _fullNames[PieceType.VerKnekht] = "\u0412\u0435\u0440 \u041A\u043D\u0435\u0445\u0442";  // Вер Кнехт
            _fullNames[PieceType.Razvedchik] = "\u0420\u0430\u0437\u0432\u0435\u0434\u0447\u0438\u043A"; // Разведчик

            // Castle cells
            _whiteCastle = new List<string>();
            _whiteCastle.Add("c1");
            _whiteCastle.Add("d1");
            _whiteCastle.Add("e1");
            _whiteCastle.Add("f1");

            _blackCastle = new List<string>();
            _blackCastle.Add("c8");
            _blackCastle.Add("d8");
            _blackCastle.Add("e8");
            _blackCastle.Add("f8");

            _allCastle = new List<string>();
            _allCastle.AddRange(_whiteCastle);
            _allCastle.AddRange(_blackCastle);
        }

        public static Dictionary<PieceType, string> ShortNames
        {
            get { return _shortNames; }
        }

        public static Dictionary<PieceType, string> FullNames
        {
            get { return _fullNames; }
        }

        public static List<string> WhiteCastle
        {
            get { return _whiteCastle; }
        }

        public static List<string> BlackCastle
        {
            get { return _blackCastle; }
        }

        public static List<string> AllCastle
        {
            get { return _allCastle; }
        }

        /// <summary>
        /// Returns the initial board position as a dictionary mapping cell name to Piece.
        /// Row 1 (white): a1-Рт, b1-Рк, c1-Пр, d1-Кт, e1-Кр, f1-Пр, g1-Рк, h1-Рт
        /// Row 2 (white): 8 Knechts
        /// Row 7 (black): 8 Knechts
        /// Row 8 (black): same layout as row 1 but black
        /// </summary>
        public static Dictionary<string, Piece> GetInitialPosition()
        {
            Dictionary<string, Piece> position = new Dictionary<string, Piece>();

            // Row 1 (white back rank)
            PieceType[] row1Types = new PieceType[] {
                PieceType.Ritter, PieceType.Razvedchik, PieceType.Prince, PieceType.Konnet,
                PieceType.King, PieceType.Prince, PieceType.Razvedchik, PieceType.Ritter
            };
            for (int i = 0; i < 8; i++)
            {
                string col = ((char)('a' + i)).ToString();
                position[col + "1"] = new Piece(PieceColor.White, row1Types[i]);
            }

            // Row 2 (white knechts)
            for (int i = 0; i < 8; i++)
            {
                string col = ((char)('a' + i)).ToString();
                position[col + "2"] = new Piece(PieceColor.White, PieceType.Knekht);
            }

            // Row 7 (black knechts)
            for (int i = 0; i < 8; i++)
            {
                string col = ((char)('a' + i)).ToString();
                position[col + "7"] = new Piece(PieceColor.Black, PieceType.Knekht);
            }

            // Row 8 (black back rank) - same layout as row 1
            PieceType[] row8Types = new PieceType[] {
                PieceType.Ritter, PieceType.Razvedchik, PieceType.Prince, PieceType.Konnet,
                PieceType.King, PieceType.Prince, PieceType.Razvedchik, PieceType.Ritter
            };
            for (int i = 0; i < 8; i++)
            {
                string col = ((char)('a' + i)).ToString();
                position[col + "8"] = new Piece(PieceColor.Black, row8Types[i]);
            }

            return position;
        }

        /// <summary>
        /// Convert cell name like "a1" to (col, row) 0-based indices.
        /// </summary>
        public static void CellToCoords(string cell, out int col, out int row)
        {
            col = (int)(cell[0] - 'a');
            row = int.Parse(cell[1].ToString()) - 1;
        }

        /// <summary>
        /// Convert (col, row) 0-based indices to cell name like "a1".
        /// </summary>
        public static string CoordsToCell(int col, int row)
        {
            return ((char)('a' + col)).ToString() + (row + 1).ToString();
        }

        /// <summary>
        /// Check if a Knekht should auto-promote to VerKnekht.
        /// White Knekht on row 6 -> White VerKnekht.
        /// Black Knekht on row 3 -> Black VerKnekht.
        /// Returns the new Piece or null if no promotion.
        /// </summary>
        public static Piece CheckKnechtPromotion(string cell, Piece piece)
        {
            if (piece.Type != PieceType.Knekht)
                return null;

            int row = int.Parse(cell[1].ToString());
            if (piece.Color == PieceColor.White && row == 6)
                return new Piece(PieceColor.White, PieceType.VerKnekht);
            if (piece.Color == PieceColor.Black && row == 3)
                return new Piece(PieceColor.Black, PieceType.VerKnekht);

            return null;
        }

        /// <summary>
        /// Check if VerKnekht needs promotion choice.
        /// Returns array of possible promotion types, or null if no promotion.
        ///
        /// White VK on a8,b8,g8,h8 -> [Konnet, Prince, Ritter, Razvedchik]
        /// White VK on c8,d8,e8,f8 -> [Konnet, Prince]
        /// Black VK on a1,b1,g1,h1 -> [Konnet, Prince, Ritter, Razvedchik]
        /// Black VK on c1,d1,e1,f1 -> [Konnet, Prince]
        /// </summary>
        public static PieceType[] CheckVerKnechtPromotion(string cell, Piece piece)
        {
            if (piece.Type != PieceType.VerKnekht)
                return null;

            char col = cell[0];
            int row = int.Parse(cell[1].ToString());

            if (piece.Color == PieceColor.White && row == 8)
            {
                if (col == 'c' || col == 'd' || col == 'e' || col == 'f')
                    return new PieceType[] { PieceType.Konnet, PieceType.Prince };
                if (col == 'a' || col == 'b' || col == 'g' || col == 'h')
                    return new PieceType[] { PieceType.Konnet, PieceType.Prince, PieceType.Ritter, PieceType.Razvedchik };
            }

            if (piece.Color == PieceColor.Black && row == 1)
            {
                if (col == 'c' || col == 'd' || col == 'e' || col == 'f')
                    return new PieceType[] { PieceType.Konnet, PieceType.Prince };
                if (col == 'a' || col == 'b' || col == 'g' || col == 'h')
                    return new PieceType[] { PieceType.Konnet, PieceType.Prince, PieceType.Ritter, PieceType.Razvedchik };
            }

            return null;
        }

        /// <summary>
        /// Check if Prince needs promotion choice.
        /// White Prince on c8,d8,e8,f8 -> [Konnet, Prince]
        /// Black Prince on c1,d1,e1,f1 -> [Konnet, Prince]
        /// Returns array of possible promotion types, or null if no promotion.
        /// </summary>
        public static PieceType[] CheckPrincePromotion(string cell, Piece piece)
        {
            if (piece.Type != PieceType.Prince)
                return null;

            char col = cell[0];
            int row = int.Parse(cell[1].ToString());

            if (piece.Color == PieceColor.White && row == 8
                && (col == 'c' || col == 'd' || col == 'e' || col == 'f'))
            {
                return new PieceType[] { PieceType.Konnet, PieceType.Prince };
            }

            if (piece.Color == PieceColor.Black && row == 1
                && (col == 'c' || col == 'd' || col == 'e' || col == 'f'))
            {
                return new PieceType[] { PieceType.Konnet, PieceType.Prince };
            }

            return null;
        }

        /// <summary>
        /// Check if this is a Razvedchik "capture with exchange" special move.
        /// Conditions: piece is Razvedchik, target cell is any castle cell,
        /// target has opponent's piece.
        /// </summary>
        public static bool IsRazvedchikExchange(Piece piece, string targetCell, Piece targetPiece)
        {
            if (piece.Type != PieceType.Razvedchik)
                return false;
            if (!_allCastle.Contains(targetCell))
                return false;
            if (targetPiece == null)
                return false;
            if (targetPiece.Color == piece.Color)
                return false;
            return true;
        }

        /// <summary>
        /// Deep-copy a position dictionary.
        /// </summary>
        public static Dictionary<string, Piece> CopyPosition(Dictionary<string, Piece> position)
        {
            Dictionary<string, Piece> copy = new Dictionary<string, Piece>();
            foreach (KeyValuePair<string, Piece> kvp in position)
            {
                copy[kvp.Key] = kvp.Value.Copy();
            }
            return copy;
        }
    }
}
