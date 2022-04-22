using System.Text.RegularExpressions;

namespace LabScanner
{
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
            ["relOp"] = ">=|<=|>|<|==|!=",
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
            var linesMatches = new Regex("^.+", RegexOptions.Multiline).Matches(input[..pos]);
            int lineNumber = linesMatches.Count;
            int columnNumber = linesMatches.LastOrDefault()?.Length ?? 1;
            var error = $"Unexpected token, line {lineNumber}, column {columnNumber}.";
            return new InvalidOperationException(error);
        }
    }
}
