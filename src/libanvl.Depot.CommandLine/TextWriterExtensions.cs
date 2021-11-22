namespace libanvl;

internal static class TextWriterExtensions
{
    public static void Write(this TextWriter writer, ConsoleColor fgColor, object? value, ConsoleColor bgColor)
    {
        Console.BackgroundColor = bgColor;
        Write(writer, fgColor, value);
    }

    public static void Write(this TextWriter writer, ConsoleColor fgColor, object? value) =>
        ForegroundColorActor(writer.Write, value, fgColor);

    public static void WriteInverted(this TextWriter writer, object? value) =>
        Write(writer, Console.BackgroundColor, value, Console.ForegroundColor);

    public static void WriteLine(this TextWriter writer, ConsoleColor fgColor, object? value, ConsoleColor bgColor)
    {
        Console.BackgroundColor = bgColor;
        WriteLine(writer, fgColor, value);
    }

    public static void WriteLine(this TextWriter writer, ConsoleColor fgColor, object? value) =>
        ForegroundColorActor(writer.WriteLine, value, fgColor);

    public static void WriteLineInverted(this TextWriter writer, object? value) =>
        WriteLine(writer, Console.BackgroundColor, value, Console.ForegroundColor);

    public static void WriteHeader(this TextWriter writer, string value, char header, ConsoleColor valueColor, ConsoleColor headerColor, bool expand = false)
    {
        WriteLine(writer, valueColor, value);
        if (expand)
        {
            writer.WriteLine();
        }

        WriteLine(writer, headerColor, new string(header, value.Length));
    }

    public static void WriteWrappedHeader(this TextWriter writer, string value, char header, ConsoleColor valueColor, ConsoleColor headerColor, bool expand = false)
    {
        WriteLine(writer, headerColor, new string(header, value.Length));
        if (expand)
        {
            writer.WriteLine();
        }

        WriteHeader(writer, value, header, valueColor, headerColor, expand);
    }

    public static string PadCenter(this string value, int totalWidth)
    {
        if (value.Length > totalWidth)
        {
            return value;
        }

        int left = (int)Math.Ceiling((totalWidth - value.Length) / 2d);
        return value.PadLeft(value.Length + left).PadRight(totalWidth);
    }

    private static void ForegroundColorActor<T>(Action<T> action, T value, ConsoleColor fgColor)
    {
        Console.ForegroundColor = fgColor;
        action(value);
        Console.ResetColor();
    }
}
