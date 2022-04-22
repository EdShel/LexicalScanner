using LabScanner;
using System.Text.RegularExpressions;

string inputFile = args.Length > 0 ? args[0] : "input.txt";
if (!File.Exists(inputFile))
{
    throw new FileNotFoundException($"File {inputFile} could not be located.");
}
string input = File.ReadAllText(inputFile);

try
{
    IEnumerable<Token> parsedTokens = new Scanner(input).Scan();
    IEnumerable<Token> parsedTokens2 = new ScannerRegex(input).Scan();

    var presentationRegex = new Regex(@"((terminator|blockBegin|blockEnd)\([^ ]+\)) ?");
    Console.WriteLine(presentationRegex.Replace(
        string.Join(" ", parsedTokens.Select(t => $"{t.Kind}({t.Value})")), "$1\n"));

    Console.WriteLine();

    Console.WriteLine(presentationRegex.Replace(
        string.Join(" ", parsedTokens2.Select(t => $"{t.Kind}({t.Value})")), "$1\n"));
}
catch (InvalidOperationException ex)
{
    Console.Error.Write(ex.Message);
}
