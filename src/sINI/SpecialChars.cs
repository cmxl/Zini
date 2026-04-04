using System.Buffers;

namespace sINI;

internal static class SpecialChars
{
	public const char SectionOpen = '[';
	public const char SectionClose = ']';
	public const char Delimiter = '=';
	public const char Quote = '"';

	private static readonly char[] _commentChars = ['#', ';'];

	public static readonly SearchValues<char> Comment = SearchValues.Create(_commentChars);
	public static readonly SearchValues<char> Whitespace = SearchValues.Create([' ', '\t', '\r', '\n']);
	public static readonly SearchValues<char> SectionEnd = SearchValues.Create([SectionClose, .. _commentChars]);
	public static readonly SearchValues<char> KeyDelimiters = SearchValues.Create([Delimiter, '\n', .. _commentChars]);
	public static readonly SearchValues<char> ValueDelimiters = SearchValues.Create(['\n', Quote, .. _commentChars]);
}
