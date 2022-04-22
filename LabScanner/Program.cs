using LabScanner;
using System.Text.RegularExpressions;

string inputFile = args.Length > 0 ? args[0] : "input.txt";
if (!File.Exists(inputFile))
{
    throw new FileNotFoundException($"File {inputFile} could not be located.");
}
string input = File.ReadAllText(inputFile);

var presentationRegex = new Regex(@"((terminator|blockBegin|blockEnd)\([^ ]+\)) ?");

try
{
    Console.WriteLine("Regex:");
    IEnumerable<Token> parsedTokensRegex = new ScannerRegex(input).Scan();

    Console.WriteLine(presentationRegex.Replace(
        string.Join(" ", parsedTokensRegex.Select(t => $"{t.Kind}({t.Value})")), "$1\n"));
}
catch (InvalidOperationException ex)
{
    Console.Error.Write(ex.Message);
}

try
{
    Console.WriteLine("\nFSM:");
    IEnumerable<Token> parsedTokensFsm = new ScannerFsm(input).Scan();

    Console.WriteLine(presentationRegex.Replace(
        string.Join(" ", parsedTokensFsm.Select(t => $"{t.Kind}({t.Value})")), "$1\n"));
}
catch (InvalidOperationException ex)
{
    Console.Error.Write(ex.Message);
}
