namespace LabScanner
{
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
}
