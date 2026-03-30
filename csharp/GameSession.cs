using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ChessT1
{
    /// <summary>
    /// Game session management for chess-T1.
    /// Handles saving/restoring sessions, config, and move history.
    /// </summary>
    public class GameSession
    {
        private string _mode;               // null, "party", "analysis"
        private string _stage;              // "startup", "game", "setup_position"
        private bool _sessionActive;
        private bool _whiteTurn;
        private int _moveNumber;
        private bool _analysisFirstWhite;
        private string _partyFolder;
        private bool _boardReversed;
        private List<Dictionary<string, Piece>> _moveHistory;
        private int _historyIndex;

        private static string _configDir;
        private static string _configFile;
        private static string _sessionFile;

        static GameSession()
        {
            _configDir = GetAppDataDir();
            _configFile = Path.Combine(_configDir, "config.json");
            _sessionFile = Path.Combine(_configDir, "session.json");
        }

        public GameSession()
        {
            _mode = null;
            _stage = "startup";
            _sessionActive = false;
            _whiteTurn = true;
            _moveNumber = 1;
            _analysisFirstWhite = true;
            _partyFolder = null;
            _boardReversed = false;
            _moveHistory = new List<Dictionary<string, Piece>>();
            _historyIndex = -1;
        }

        #region Properties

        public string Mode
        {
            get { return _mode; }
            set { _mode = value; }
        }

        public string Stage
        {
            get { return _stage; }
            set { _stage = value; }
        }

        public bool SessionActive
        {
            get { return _sessionActive; }
            set { _sessionActive = value; }
        }

        public bool WhiteTurn
        {
            get { return _whiteTurn; }
            set { _whiteTurn = value; }
        }

        public int MoveNumber
        {
            get { return _moveNumber; }
            set { _moveNumber = value; }
        }

        public bool AnalysisFirstWhite
        {
            get { return _analysisFirstWhite; }
            set { _analysisFirstWhite = value; }
        }

        public string PartyFolder
        {
            get { return _partyFolder; }
            set { _partyFolder = value; }
        }

        public bool BoardReversed
        {
            get { return _boardReversed; }
            set { _boardReversed = value; }
        }

        public List<Dictionary<string, Piece>> MoveHistory
        {
            get { return _moveHistory; }
            set { _moveHistory = value; }
        }

        public int HistoryIndex
        {
            get { return _historyIndex; }
            set { _historyIndex = value; }
        }

        public static string ConfigDir
        {
            get { return _configDir; }
        }

        #endregion

        #region AppData path

        private static string GetAppDataDir()
        {
            // Windows: %APPDATA%\chesst1
            string appdata = Environment.GetEnvironmentVariable("APPDATA");
            if (appdata != null && appdata.Length > 0)
            {
                return Path.Combine(appdata, "chesst1");
            }
            // Fallback: user profile
            string home = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (home != null && home.Length > 0)
            {
                return Path.Combine(home, "chesst1");
            }
            // Last resort
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "chesst1");
        }

        private static void EnsureConfigDir()
        {
            if (!Directory.Exists(_configDir))
            {
                Directory.CreateDirectory(_configDir);
            }
        }

        #endregion

        #region Config (skip_intro etc.)

        /// <summary>
        /// Load application config. Returns a dictionary with config keys.
        /// </summary>
        public static Dictionary<string, object> LoadConfig()
        {
            EnsureConfigDir();
            Dictionary<string, object> config = new Dictionary<string, object>();
            config["skip_intro"] = false;

            try
            {
                if (File.Exists(_configFile))
                {
                    string json = ReadFileUtf8(_configFile);
                    Dictionary<string, object> parsed = SimpleJsonReader.Parse(json);
                    if (parsed != null)
                    {
                        foreach (KeyValuePair<string, object> kvp in parsed)
                        {
                            config[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Return defaults on any error
            }

            return config;
        }

        /// <summary>
        /// Save application config.
        /// </summary>
        public static void SaveConfig(Dictionary<string, object> config)
        {
            EnsureConfigDir();
            string json = SimpleJsonWriter.Write(config);
            WriteFileUtf8(_configFile, json);
        }

        #endregion

        #region Session save/load

        /// <summary>
        /// Save current session state for restoration on next launch.
        /// </summary>
        public void SaveSession()
        {
            EnsureConfigDir();

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["mode"] = _mode;
            data["stage"] = _stage;
            data["white_turn"] = _whiteTurn;
            data["move_number"] = _moveNumber;
            data["party_folder"] = _partyFolder;
            data["board_reversed"] = _boardReversed;
            data["analysis_first_white"] = _analysisFirstWhite;
            data["history_index"] = _historyIndex;

            // Serialize move history
            List<object> historyList = new List<object>();
            for (int h = 0; h < _moveHistory.Count; h++)
            {
                Dictionary<string, Piece> pos = _moveHistory[h];
                Dictionary<string, object> posData = new Dictionary<string, object>();
                foreach (KeyValuePair<string, Piece> kvp in pos)
                {
                    Dictionary<string, object> pieceData = new Dictionary<string, object>();
                    pieceData["color"] = kvp.Value.Color == PieceColor.White ? "white" : "black";
                    pieceData["type"] = PieceTypeToString(kvp.Value.Type);
                    posData[kvp.Key] = pieceData;
                }
                historyList.Add(posData);
            }
            data["move_history"] = historyList;

            string json = SimpleJsonWriter.Write(data);
            WriteFileUtf8(_sessionFile, json);
        }

        /// <summary>
        /// Load saved session. Returns true if session was loaded successfully.
        /// </summary>
        public bool LoadSession()
        {
            try
            {
                if (!File.Exists(_sessionFile))
                    return false;

                string json = ReadFileUtf8(_sessionFile);
                Dictionary<string, object> data = SimpleJsonReader.Parse(json);
                if (data == null)
                    return false;

                if (data.ContainsKey("mode"))
                    _mode = data["mode"] as string;
                if (data.ContainsKey("stage"))
                    _stage = data["stage"] as string;
                if (data.ContainsKey("white_turn"))
                    _whiteTurn = ToBool(data["white_turn"]);
                if (data.ContainsKey("move_number"))
                    _moveNumber = ToInt(data["move_number"]);
                if (data.ContainsKey("party_folder"))
                    _partyFolder = data["party_folder"] as string;
                if (data.ContainsKey("board_reversed"))
                    _boardReversed = ToBool(data["board_reversed"]);
                if (data.ContainsKey("analysis_first_white"))
                    _analysisFirstWhite = ToBool(data["analysis_first_white"]);
                if (data.ContainsKey("history_index"))
                    _historyIndex = ToInt(data["history_index"]);

                // Deserialize move history
                _moveHistory = new List<Dictionary<string, Piece>>();
                if (data.ContainsKey("move_history"))
                {
                    List<object> historyList = data["move_history"] as List<object>;
                    if (historyList != null)
                    {
                        for (int h = 0; h < historyList.Count; h++)
                        {
                            Dictionary<string, object> posData = historyList[h] as Dictionary<string, object>;
                            if (posData == null) continue;

                            Dictionary<string, Piece> pos = new Dictionary<string, Piece>();
                            foreach (KeyValuePair<string, object> kvp in posData)
                            {
                                Dictionary<string, object> pieceData = kvp.Value as Dictionary<string, object>;
                                if (pieceData == null) continue;

                                string colorStr = pieceData["color"] as string;
                                string typeStr = pieceData["type"] as string;
                                PieceColor color = (colorStr == "white") ? PieceColor.White : PieceColor.Black;
                                PieceType type = StringToPieceType(typeStr);
                                pos[kvp.Key] = new Piece(color, type);
                            }
                            _moveHistory.Add(pos);
                        }
                    }
                }

                _sessionActive = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Remove saved session file and reset state.
        /// </summary>
        public void ClearSession()
        {
            _mode = null;
            _stage = "startup";
            _sessionActive = false;
            _whiteTurn = true;
            _moveNumber = 1;
            _analysisFirstWhite = true;
            _partyFolder = null;
            _boardReversed = false;
            _moveHistory = new List<Dictionary<string, Piece>>();
            _historyIndex = -1;

            try
            {
                if (File.Exists(_sessionFile))
                {
                    File.Delete(_sessionFile);
                }
            }
            catch (Exception)
            {
                // Ignore deletion errors
            }
        }

        #endregion

        #region Move indicator and screenshot

        /// <summary>
        /// Get display text for the move indicator (used in UI).
        /// White turn: "N. __ хб"
        /// Black turn: "N … __ хч"
        /// </summary>
        public static string GetIndicatorText(int moveNumber, bool whiteTurn)
        {
            if (whiteTurn)
            {
                return moveNumber.ToString() + ". __ \u0445\u0431"; // хб
            }
            else
            {
                return moveNumber.ToString() + " \u2026 __ \u0445\u0447"; // … хч
            }
        }

        /// <summary>
        /// Generate screenshot filename based on move indicator text.
        /// Sanitizes characters forbidden in Windows filenames.
        /// </summary>
        public static string GetScreenshotName(int moveNumber, bool whiteTurn)
        {
            string name = GetIndicatorText(moveNumber, whiteTurn);

            // Replace Windows-forbidden and special characters
            name = name.Replace("\u2026", "..."); // ellipsis -> three dots
            name = name.Replace(":", "");
            name = name.Replace("<", "");
            name = name.Replace(">", "");
            name = name.Replace("\"", "");
            name = name.Replace("/", "");
            name = name.Replace("\\", "");
            name = name.Replace("|", "");
            name = name.Replace("?", "");
            name = name.Replace("*", "");

            return name + ".png";
        }

        #endregion

        #region Turn recalculation

        /// <summary>
        /// Recalculate whose turn it is and the move number based on history index.
        /// Returns whiteTurn and moveNumber via out parameters.
        /// </summary>
        public static void RecalculateTurn(int historyIndex, bool analysisFirstWhite, string mode,
            out bool whiteTurn, out int moveNumber)
        {
            if (historyIndex <= 0)
            {
                whiteTurn = analysisFirstWhite;
                moveNumber = 1;
                return;
            }

            // The first position (index 0) is the starting position before any move.
            // Each subsequent index represents one half-move.
            int halfMoves = historyIndex;

            if (analysisFirstWhite)
            {
                // Even half-moves: white's turn; odd: black's turn
                whiteTurn = (halfMoves % 2 == 0);
                moveNumber = (halfMoves / 2) + 1;
            }
            else
            {
                // First move is black's.
                // After black moves (index 1), it becomes white's turn, move 2.
                whiteTurn = (halfMoves % 2 != 0);
                if (halfMoves == 0)
                {
                    moveNumber = 1;
                }
                else
                {
                    moveNumber = ((halfMoves - 1) / 2) + 1;
                    if (!whiteTurn)
                        moveNumber = ((halfMoves) / 2) + 1;
                }
            }
        }

        #endregion

        #region PieceType <-> string conversion

        private static string PieceTypeToString(PieceType type)
        {
            switch (type)
            {
                case PieceType.King: return "king";
                case PieceType.Konnet: return "konnet";
                case PieceType.Prince: return "prince";
                case PieceType.Ritter: return "ritter";
                case PieceType.Knekht: return "knekht";
                case PieceType.VerKnekht: return "ver_knekht";
                case PieceType.Razvedchik: return "razvedchik";
                default: return "king";
            }
        }

        private static PieceType StringToPieceType(string s)
        {
            if (s == null) return PieceType.King;
            switch (s)
            {
                case "king": return PieceType.King;
                case "konnet": return PieceType.Konnet;
                case "prince": return PieceType.Prince;
                case "ritter": return PieceType.Ritter;
                case "knekht": return PieceType.Knekht;
                case "ver_knekht": return PieceType.VerKnekht;
                case "razvedchik": return PieceType.Razvedchik;
                default: return PieceType.King;
            }
        }

        #endregion

        #region Helpers

        private static bool ToBool(object obj)
        {
            if (obj is bool)
                return (bool)obj;
            if (obj is string)
                return ((string)obj).ToLower() == "true";
            return false;
        }

        private static int ToInt(object obj)
        {
            if (obj is int)
                return (int)obj;
            if (obj is long)
                return (int)(long)obj;
            if (obj is double)
                return (int)(double)obj;
            if (obj is string)
            {
                int result;
                if (int.TryParse((string)obj, out result))
                    return result;
            }
            return 0;
        }

        private static string ReadFileUtf8(string path)
        {
            using (StreamReader reader = new StreamReader(path, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private static void WriteFileUtf8(string path, string content)
        {
            using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                writer.Write(content);
            }
        }

        #endregion
    }

    #region Simple JSON reader/writer (.NET 3.5 compatible, no external dependencies)

    /// <summary>
    /// Minimal JSON parser. Produces Dictionary&lt;string, object&gt;, List&lt;object&gt;,
    /// string, double, bool, or null.
    /// </summary>
    internal static class SimpleJsonReader
    {
        public static Dictionary<string, object> Parse(string json)
        {
            if (json == null) return null;
            int index = 0;
            object result = ParseValue(json, ref index);
            return result as Dictionary<string, object>;
        }

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && (json[index] == ' ' || json[index] == '\t'
                || json[index] == '\r' || json[index] == '\n'))
            {
                index++;
            }
        }

        private static object ParseValue(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length) return null;

            char c = json[index];
            if (c == '{') return ParseObject(json, ref index);
            if (c == '[') return ParseArray(json, ref index);
            if (c == '"') return ParseString(json, ref index);
            if (c == 't' || c == 'f') return ParseBool(json, ref index);
            if (c == 'n') return ParseNull(json, ref index);
            return ParseNumber(json, ref index);
        }

        private static Dictionary<string, object> ParseObject(string json, ref int index)
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            index++; // skip '{'
            SkipWhitespace(json, ref index);
            if (index < json.Length && json[index] == '}')
            {
                index++;
                return obj;
            }

            while (index < json.Length)
            {
                SkipWhitespace(json, ref index);
                string key = ParseString(json, ref index);
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ':')
                    index++;
                object value = ParseValue(json, ref index);
                obj[key] = value;
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                }
                else
                {
                    break;
                }
            }
            SkipWhitespace(json, ref index);
            if (index < json.Length && json[index] == '}')
                index++;
            return obj;
        }

        private static List<object> ParseArray(string json, ref int index)
        {
            List<object> list = new List<object>();
            index++; // skip '['
            SkipWhitespace(json, ref index);
            if (index < json.Length && json[index] == ']')
            {
                index++;
                return list;
            }

            while (index < json.Length)
            {
                object value = ParseValue(json, ref index);
                list.Add(value);
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                }
                else
                {
                    break;
                }
            }
            SkipWhitespace(json, ref index);
            if (index < json.Length && json[index] == ']')
                index++;
            return list;
        }

        private static string ParseString(string json, ref int index)
        {
            if (index >= json.Length || json[index] != '"')
                return "";
            index++; // skip opening quote
            StringBuilder sb = new StringBuilder();
            while (index < json.Length)
            {
                char c = json[index];
                if (c == '\\')
                {
                    index++;
                    if (index >= json.Length) break;
                    char esc = json[index];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (index + 4 < json.Length)
                            {
                                string hex = json.Substring(index + 1, 4);
                                int code = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
                                sb.Append((char)code);
                                index += 4;
                            }
                            break;
                        default: sb.Append(esc); break;
                    }
                }
                else if (c == '"')
                {
                    index++;
                    return sb.ToString();
                }
                else
                {
                    sb.Append(c);
                }
                index++;
            }
            return sb.ToString();
        }

        private static object ParseNumber(string json, ref int index)
        {
            int start = index;
            if (index < json.Length && json[index] == '-')
                index++;
            while (index < json.Length && json[index] >= '0' && json[index] <= '9')
                index++;
            bool isFloat = false;
            if (index < json.Length && json[index] == '.')
            {
                isFloat = true;
                index++;
                while (index < json.Length && json[index] >= '0' && json[index] <= '9')
                    index++;
            }
            if (index < json.Length && (json[index] == 'e' || json[index] == 'E'))
            {
                isFloat = true;
                index++;
                if (index < json.Length && (json[index] == '+' || json[index] == '-'))
                    index++;
                while (index < json.Length && json[index] >= '0' && json[index] <= '9')
                    index++;
            }

            string numStr = json.Substring(start, index - start);
            if (isFloat)
            {
                double d;
                double.TryParse(numStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out d);
                return d;
            }
            else
            {
                int i;
                if (int.TryParse(numStr, out i))
                    return i;
                long l;
                if (long.TryParse(numStr, out l))
                    return l;
                double d;
                double.TryParse(numStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out d);
                return d;
            }
        }

        private static object ParseBool(string json, ref int index)
        {
            if (json.Substring(index).StartsWith("true"))
            {
                index += 4;
                return true;
            }
            if (json.Substring(index).StartsWith("false"))
            {
                index += 5;
                return false;
            }
            return false;
        }

        private static object ParseNull(string json, ref int index)
        {
            if (json.Substring(index).StartsWith("null"))
            {
                index += 4;
            }
            return null;
        }
    }

    /// <summary>
    /// Minimal JSON writer. Supports Dictionary&lt;string, object&gt;, List&lt;object&gt;,
    /// string, int, long, double, bool, and null.
    /// </summary>
    internal static class SimpleJsonWriter
    {
        public static string Write(object obj)
        {
            StringBuilder sb = new StringBuilder();
            WriteValue(sb, obj, 0);
            return sb.ToString();
        }

        private static void WriteValue(StringBuilder sb, object obj, int indent)
        {
            if (obj == null)
            {
                sb.Append("null");
            }
            else if (obj is string)
            {
                WriteString(sb, (string)obj);
            }
            else if (obj is bool)
            {
                sb.Append((bool)obj ? "true" : "false");
            }
            else if (obj is int)
            {
                sb.Append(((int)obj).ToString());
            }
            else if (obj is long)
            {
                sb.Append(((long)obj).ToString());
            }
            else if (obj is double)
            {
                sb.Append(((double)obj).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (obj is Dictionary<string, object>)
            {
                WriteObject(sb, (Dictionary<string, object>)obj, indent);
            }
            else if (obj is List<object>)
            {
                WriteArray(sb, (List<object>)obj, indent);
            }
            else
            {
                // Fallback: treat as string
                WriteString(sb, obj.ToString());
            }
        }

        private static void WriteObject(StringBuilder sb, Dictionary<string, object> dict, int indent)
        {
            sb.Append("{\n");
            int count = 0;
            int total = dict.Count;
            foreach (KeyValuePair<string, object> kvp in dict)
            {
                Indent(sb, indent + 1);
                WriteString(sb, kvp.Key);
                sb.Append(": ");
                WriteValue(sb, kvp.Value, indent + 1);
                count++;
                if (count < total)
                    sb.Append(",");
                sb.Append("\n");
            }
            Indent(sb, indent);
            sb.Append("}");
        }

        private static void WriteArray(StringBuilder sb, List<object> list, int indent)
        {
            sb.Append("[\n");
            for (int i = 0; i < list.Count; i++)
            {
                Indent(sb, indent + 1);
                WriteValue(sb, list[i], indent + 1);
                if (i < list.Count - 1)
                    sb.Append(",");
                sb.Append("\n");
            }
            Indent(sb, indent);
            sb.Append("]");
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u");
                            sb.Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
        }

        private static void Indent(StringBuilder sb, int level)
        {
            for (int i = 0; i < level; i++)
                sb.Append("  ");
        }
    }

    #endregion
}
