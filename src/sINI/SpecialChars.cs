using System.Buffers;

internal static class SpecialChars
{
	public static readonly SearchValues<char> Comment = SearchValues.Create(['#', ';']);
	public const char SectionOpen = '[';
	public const char SectionClose = ']';
	public const char Delimiter = '=';
	public const char Quote = '"';
}
