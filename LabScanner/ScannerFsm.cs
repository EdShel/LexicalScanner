using System.Text.RegularExpressions;

namespace LabScanner
{
    public class ScannerFsm
    {
        private readonly string input;
        private int pos = -1;
        private char symbol;

        private string? error;

        private readonly List<Token> tokens = new();

        public ScannerFsm(string input)
        {
            this.input = input;
        }

        private int NextSymbol()
        {
            int initPos = this.pos;
            do
            {
                if (this.pos == this.input.Length - 1)
                {
                    break;
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
            var linesMatches = new Regex("^.+", RegexOptions.Multiline).Matches(this.input[..this.pos]);
            int lineNumber = linesMatches.Count;
            int columnNumber = linesMatches.LastOrDefault()?.Length ?? 1;
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

        public IEnumerable<Token> Scan()
        {
            NextSymbol();
            Block();

            if (this.pos != this.input.Length - 1)
            {
                Error("Unexpected token");
            }

            if (this.error != null)
            {
                throw new InvalidOperationException(this.error);
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
                && While()
                && Terminator();
        }

        private bool BlockBegin()
        {
            return Accept('{') && ProduceToken("blockBegin", "{");
        }

        private bool BlockEnd()
        {
            return Accept('}') && ProduceToken("blockEnd", "}");
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
            else if (Accept('=') && Accept('='))
            {
                return ProduceToken("relOp", "==");
            }
            else if (Accept('!') && Accept('='))
            {
                return ProduceToken("relOp", "!=");
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

            string word = this.input[beginPos..(endPos + 1)];
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
                Error("Expected non-zero digit in number");
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
                    Error("Expected at least one digit after period in number");
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
                Error($"Unexpected letter '{this.symbol}' in number");
            }
            return ProduceToken("number", this.input[beginPos..(endPos + 1)]);
        }

        private bool Terminator()
        {
            return Accept(';') && ProduceToken("terminator", ";");
        }
    }
}
