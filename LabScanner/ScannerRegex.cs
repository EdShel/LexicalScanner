using System.Text.RegularExpressions;

namespace LabScanner
{
    public record Token(string Value, string Kind);

    public class Scanner
    {
        private int pos = -1;
        private string input;
        private char symbol;

        private string error;

        private List<Token> tokens = new List<Token>();

        public Scanner(string input)
        {
            this.input = input;
        }

        //https://en.wikipedia.org/wiki/Recursive_descent_parser
        private int NextSymbol()
        {
            int initPos = this.pos;
            do
            {
                if (this.pos == this.input.Length - 1)
                {
                    throw new InvalidOperationException("End of sequence");
                }
                this.pos++;
                this.symbol = this.input[this.pos];
            } while (Chars.Space(this.symbol));

            return this.pos - initPos;
        }

        private bool Error(string message)
        {
            if (this.error != null)
            {
                return false;
            }
            var linesMatches = new Regex("^(.+)", RegexOptions.Multiline).Matches(this.input.Substring(0, this.pos));
            int lineNumber = linesMatches.Count;
            int columnNumber = linesMatches.Last().Groups[1].Length;
            this.error = $"{message}, line {lineNumber}, column {columnNumber}.";
            return false;
        }

        private bool ProduceToken(string kind, string value)
        {
            this.tokens.Add(new Token(Kind: kind, Value: value));
            return true;
        }

        private bool Accept(char s)
        {
            if (this.symbol == s)
            {
                NextSymbol();
                return true;
            }
            return false;
        }

        private bool AcceptWord(string word)
        {
            foreach (char letter in word)
            {
                if (!Accept(letter))
                {
                    return false;
                }
            }
            return true;
        }

        public IEnumerable<Token> Scan()
        {
            NextSymbol();
            Block();

            if (this.error != null)
            {
                Console.Error.WriteLine(this.error);
                //throw new InvalidOperationException(this.error);
            }
            return this.tokens;
        }
        private bool Block()
        {
            while (Identifier() && (DoWhileLoop() || (((IndexerBegin() && Expression() && IndexerEnd()) || true)
                    && Equals()
                    && Expression()
                    && Terminator())))
            {

            }
            return true;
        }

        private bool DoWhileLoop()
        {
            return BlockBegin()
                && Block()
                && BlockEnd()
                && While();
        }

        private bool Do()
        {
            return Identifier();
        }

        private bool BlockBegin()
        {
            return Accept('{') && ProduceToken("blockBegin", "{");
        }
        private bool BlockEnd()
        {
            return Accept('}') && ProduceToken("blockEnd", "}");
        }

        private bool Statement()
        {
            return Variable()
                && Equals()
                && Expression()
                && Terminator();
        }

        private bool Variable()
        {
            return Identifier()
                && ((IndexerBegin() && Expression() && IndexerEnd()) || true);
        }

        private bool IndexerBegin()
        {
            return Accept('[') && ProduceToken("indexerBegin", "[");
        }

        private bool IndexerEnd()
        {
            return Accept(']') && ProduceToken("indexerEnd", "]");
        }

        private bool Equals()
        {
            return Accept('=') && ProduceToken("equals", "=");
        }

        private bool While()
        {
            return Identifier()
                && ParBegin()
                && Condition()
                && ParEnd();
        }

        private bool ParBegin()
        {
            return Accept('(') && ProduceToken("parBegin", "(");
        }

        private bool ParEnd()
        {
            return Accept(')') && ProduceToken("parEnd", ")");
        }

        private bool Condition()
        {
            return Expression() && RelOp() && Expression();
        }

        private bool Expression()
        {
            return (Variable() || Number())
                && ((ArithmOp() && Expression()) || true);
        }

        private bool ArithmOp()
        {
            return (Accept('+') && ProduceToken("arithmOp", "+"))
                || (Accept('-') && ProduceToken("arithmOp", "-"))
                || (Accept('/') && ProduceToken("arithmOp", "/"))
                || (Accept('*') && ProduceToken("arithmOp", "*"));
        }

        private bool RelOp()
        {
            if (Accept('>'))
            {
                if (Accept('='))
                {
                    return ProduceToken("relOp", ">=");
                }
                return ProduceToken("relOp", ">");
            }
            else if (Accept('<'))
            {
                if (Accept('='))
                {
                    return ProduceToken("relOp", "<=");
                }
                return ProduceToken("relOp", "<");
            }
            return false;
        }

        private bool Identifier()
        {
            if (!Chars.Letter(this.symbol))
            {
                return false;
            }
            int beginPos = this.pos;
            int endPos;
            do
            {
                endPos = this.pos;
                if (NextSymbol() != 1)
                {
                    break;
                }

            }
            while (Chars.Letter(this.symbol) || Chars.Digit(this.symbol));

            string word = this.input.Substring(beginPos, endPos + 1 - beginPos);
            switch (word)
            {
                case "do": return ProduceToken("do", word);
                case "while": return ProduceToken("while", word);
                default: return ProduceToken("identifier", word);
            }
        }

        private bool Number()
        {
            if (Accept('0'))
            {
                return ProduceToken("number", "0");
            }
            int beginPos = this.pos;
            if (this.symbol == '+' || this.symbol == '-')
            {
                NextSymbol();
            }
            if (!Chars.DigitNotZero(this.symbol))
            {
                Error("Expected non-zero digit");
                return false;
            }
            int endPos = this.pos;
            NextSymbol();
            while (Chars.Digit(this.symbol))
            {
                endPos = this.pos;
                NextSymbol();
            }
            if (this.symbol == '.')
            {
                NextSymbol();
                if (!Chars.Digit(this.symbol))
                {
                    Error("Expected at least one digit after period");
                    return false;
                }
                endPos = this.pos;
                NextSymbol();
                while (Chars.Digit(this.symbol))
                {
                    endPos = this.pos;
                    NextSymbol();
                }
            }
            if (Chars.Letter(this.symbol))
            {
                Error($"Unexpected letter '{this.symbol}'");
            }
            return ProduceToken("number", this.input.Substring(beginPos, endPos + 1 - beginPos));
        }

        private bool Terminator()
        {
            return Accept(';') && ProduceToken("terminator", ";");
        }
    }

    public static class Chars
    {
        public static bool Digit(char x)
        {
            return '0' <= x && x <= '9';
        }

        public static bool DigitNotZero(char x)
        {
            return '1' <= x && x <= '9';
        }

        public static bool Letter(char x)
        {
            return 'A' <= x && x <= 'Z'
                || 'a' <= x && x <= 'z'
                || x == '_';
        }

        public static bool Space(char x)
        {
            return x == ' '
                || x == '\t'
                || x == '\r'
                || x == '\n';
        }
    }

    public class ScannerRegex
    {
        // Characters
        private const string charDigit = "[0-9]";
        private const string charDigitNotZero = "[1-9]";
        private const string charLetter = "[A-Za-z_]";
        private const string charSpace = "[ \r\n\t]";
        private const string intNum = $"([+-]?{charDigitNotZero}{charDigit}*|0)";
        private const string floatNum = $@"({intNum}\.{charDigit}+)";

        // Tokens
        private readonly Dictionary<string, string> tokens = new()
        {
            ["ignore"] = $"{charSpace}+|$",
            ["terminator"] = $";",
            ["do"] = $"do(?!{charLetter}|{charDigit})",
            ["while"] = $"while(?!{charLetter}|{charDigit})",
            ["identifier"] = $"{charLetter}({charLetter}|{charDigit})*",
            ["number"] = $"({floatNum}|{intNum})(?!{charLetter})",
            ["blockBegin"] = "\\{",
            ["blockEnd"] = "\\}",
            ["indexerBegin"] = "\\[",
            ["indexerEnd"] = "\\]",
            ["parBegin"] = "\\(",
            ["parEnd"] = "\\)",
            ["relOp"] = ">=|<=|>|<",
            ["equals"] = "=",
            ["arithmOp"] = "[-+*/]",
        };

        private readonly string input;

        public ScannerRegex(string input)
        {
            this.input = input;
        }

        public IEnumerable<Token> Scan()
        {
            List<Token> parsedTokens = new List<Token>();
            for (int i = 0; i < input.Length;)
            {
                Match? successfulMatch = null;
                string? successfulToken = null;

                foreach (KeyValuePair<string, string> token in this.tokens)
                {
                    var regex = new Regex($"^({token.Value})");
                    Match match = regex.Match(input.Substring(i));
                    if (match.Success)
                    {
                        successfulMatch = match;
                        successfulToken = token.Key;
                        break;
                    }
                }

                if (successfulMatch == null)
                {
                    throw Error(input, i);
                }

                i += successfulMatch.Length;

                if (successfulToken == "ignore")
                {
                    continue;
                }

                Token parsedToken = new Token(Value: successfulMatch.Value, Kind: successfulToken!);
                parsedTokens.Add(parsedToken);
            }

            return parsedTokens;
        }

        private static InvalidOperationException Error(string input, int pos)
        {
            string tokenNextWord = new Regex(@"\S+").Match(input, pos).Value;
            return new InvalidOperationException(
                $"Unknown token at position {pos}: {tokenNextWord}");
        }
    }
}
