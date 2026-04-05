using System.Collections.Frozen;

namespace Zini.Tests;

public class ConfigParserTests
{
	// ── Basic parsing ──────────────────────────────────────────────

	[Fact]
	public void Parse_SimpleSection_ReturnsKeyValues()
	{
		var result = ConfigParser.Parse("[Server]\nhost = localhost\nport = 8080");

		Assert.Equal("localhost", result["Server"]["host"]);
		Assert.Equal("8080", result["Server"]["port"]);
	}

	[Fact]
	public void Parse_EmptyInput_ReturnsEmptyDictionary()
	{
		var result = ConfigParser.Parse("");
		Assert.Equal(0, result.Count);
	}

	[Fact]
	public void Parse_WhitespaceOnly_ReturnsEmptyDictionary()
	{
		var result = ConfigParser.Parse("   \n\t\n  ");
		Assert.Equal(0, result.Count);
	}

	// ── String overload (#11) ──────────────────────────────────────

	[Fact]
	public void Parse_StringOverload_ProducesSameResult()
	{
		const string input = "[DB]\nhost = localhost";
		var fromString = ConfigParser.Parse(input);
		var fromSpan = ConfigParser.Parse(input.AsSpan());

		Assert.Equal(fromSpan["DB"]["host"], fromString["DB"]["host"]);
	}

	// ── Global keys ────────────────────────────────────────────────

	[Fact]
	public void Parse_GlobalKeys_StoredUnderEmptyString()
	{
		var result = ConfigParser.Parse("app = MyApp\nversion = 2");

		Assert.Equal("MyApp", result[""]["app"]);
		Assert.Equal("2", result[""]["version"]);
	}

	[Fact]
	public void Parse_NoGlobalKeys_OmitsEmptySection()
	{
		var result = ConfigParser.Parse("[Section]\nkey = val");

		Assert.False(result.ContainsSection(""));
	}

	// ── Lazy global section (#8) ───────────────────────────────────

	[Fact]
	public void Parse_GlobalSectionCreatedOnDemand_NotPreAllocated()
	{
		// When no global keys exist, the empty section should not appear
		var result = ConfigParser.Parse("[A]\nx = 1\n[B]\ny = 2");

		Assert.False(result.ContainsSection(""));
		Assert.Equal(2, result.Count);
	}

	// ── Section merging ────────────────────────────────────────────

	[Fact]
	public void Parse_DuplicateSections_MergesKeys()
	{
		var result = ConfigParser.Parse("[DB]\nhost = alpha\n\n[DB]\nhost = beta\nport = 5432");

		Assert.Equal("beta", result["DB"]["host"]);
		Assert.Equal("5432", result["DB"]["port"]);
	}

	[Fact]
	public void Parse_SectionsAreCaseInsensitive()
	{
		var result = ConfigParser.Parse("[server]\nhost = a\n\n[Server]\nport = 80");

		Assert.Equal(1, result.Count);
		Assert.Equal("a", result["server"]["host"]);
		Assert.Equal("80", result["server"]["port"]);
	}

	// ── Case insensitivity ─────────────────────────────────────────

	[Fact]
	public void Parse_KeysAreCaseInsensitive()
	{
		var result = ConfigParser.Parse("[S]\nMyKey = first\nmykey = second");

		Assert.Single(result["S"]);
		Assert.Equal("second", result["S"]["mykey"]);
	}

	// ── Comments ───────────────────────────────────────────────────

	[Fact]
	public void Parse_FullLineComments_Ignored()
	{
		var result = ConfigParser.Parse("# comment\n; also comment\n[S]\nkey = val");

		Assert.Equal("val", result["S"]["key"]);
	}

	[Fact]
	public void Parse_InlineComments_StrippedFromValues()
	{
		var result = ConfigParser.Parse("[S]\nhost = localhost  # inline");

		Assert.Equal("localhost", result["S"]["host"]);
	}

	[Fact]
	public void Parse_InlineCommentWithSemicolon_Stripped()
	{
		var result = ConfigParser.Parse("[S]\nhost = localhost  ; inline");

		Assert.Equal("localhost", result["S"]["host"]);
	}

	// ── Whitespace handling ────────────────────────────────────────

	[Fact]
	public void Parse_WhitespaceAroundKeysAndValues_Trimmed()
	{
		var result = ConfigParser.Parse("[S]\n  key  =  value  ");

		Assert.Equal("value", result["S"]["key"]);
	}

	[Fact]
	public void Parse_WhitespaceInsideSectionBrackets_Trimmed()
	{
		var result = ConfigParser.Parse("[  Spaced  ]\nk = v");

		Assert.Equal("v", result["Spaced"]["k"]);
	}

	// ── Empty values ───────────────────────────────────────────────

	[Fact]
	public void Parse_EmptyValue_ReturnsEmptyString()
	{
		var result = ConfigParser.Parse("[S]\nkey =");

		Assert.Equal("", result["S"]["key"]);
	}

	[Fact]
	public void Parse_EmptyValueWithWhitespace_ReturnsEmptyString()
	{
		var result = ConfigParser.Parse("[S]\nkey =   ");

		Assert.Equal("", result["S"]["key"]);
	}

	// ── Quoted values ──────────────────────────────────────────────

	[Fact]
	public void Parse_QuotedValue_PreservesWhitespace()
	{
		var result = ConfigParser.Parse("[S]\nmsg = \"  hello world  \"");

		Assert.Equal("  hello world  ", result["S"]["msg"]);
	}

	[Fact]
	public void Parse_QuotedValue_PreservesCommentChars()
	{
		var result = ConfigParser.Parse("[S]\npath = \"value # not a comment\"");

		Assert.Equal("value # not a comment", result["S"]["path"]);
	}

	[Fact]
	public void Parse_EscapedQuotes_UnescapedCorrectly()
	{
		var result = ConfigParser.Parse("[S]\ngreeting = \"she said \"\"hello\"\" to me\"");

		Assert.Equal("she said \"hello\" to me", result["S"]["greeting"]);
	}

	[Fact]
	public void Parse_QuotedEmptyValue_ReturnsEmptyString()
	{
		var result = ConfigParser.Parse("[S]\nkey = \"\"");

		Assert.Equal("", result["S"]["key"]);
	}

	[Fact]
	public void Parse_QuotedEscapedQuoteOnly_ReturnsSingleQuote()
	{
		var result = ConfigParser.Parse("[S]\nkey = \"\"\"\"");

		Assert.Equal("\"", result["S"]["key"]);
	}

	[Fact]
	public void Parse_QuotedValueFollowedByComment_Works()
	{
		var result = ConfigParser.Parse("[S]\nval = \"quoted\" # comment");

		Assert.Equal("quoted", result["S"]["val"]);
	}

	[Fact]
	public void Parse_QuotedValueFollowedByWhitespace_Works()
	{
		var result = ConfigParser.Parse("[S]\nval = \"quoted\"   ");

		Assert.Equal("quoted", result["S"]["val"]);
	}

	// ── Equals in values ───────────────────────────────────────────

	[Fact]
	public void Parse_MultipleEqualsInValue_TreatedAsLiteral()
	{
		var result = ConfigParser.Parse("[S]\nkey = a=b=c");

		Assert.Equal("a=b=c", result["S"]["key"]);
	}

	// ── Keys without = ─────────────────────────────────────────────

	[Fact]
	public void Parse_LineWithoutEquals_SilentlySkipped()
	{
		var result = ConfigParser.Parse("[S]\njusttext\nkey = val");

		Assert.Single(result["S"]);
		Assert.Equal("val", result["S"]["key"]);
	}

	// ── EOF handling ───────────────────────────────────────────────

	[Fact]
	public void Parse_ValueAtEofWithoutNewline_Captured()
	{
		var result = ConfigParser.Parse("[S]\nkey = value");

		Assert.Equal("value", result["S"]["key"]);
	}

	[Fact]
	public void Parse_SectionAtEofWithNoKeys_RegisteredEmpty()
	{
		// Section header with ] but no keys after it
		var result = ConfigParser.Parse("[Empty]");

		// The section was registered during parsing but has no keys
		// With lazy creation in ConfigSectionOpen, it IS registered
		Assert.True(result.ContainsSection("Empty"));
		Assert.Empty(result["Empty"]);
	}

	// ── CRLF line endings ──────────────────────────────────────────

	[Fact]
	public void Parse_CrLfLineEndings_WorksCorrectly()
	{
		var result = ConfigParser.Parse("[S]\r\nkey = value\r\nother = data\r\n");

		Assert.Equal("value", result["S"]["key"]);
		Assert.Equal("data", result["S"]["other"]);
	}

	// ── FrozenDictionary immutability (#12) ────────────────────────

	[Fact]
	public void Parse_ReturnsConfigDocument_WithFrozenSections()
	{
		var result = ConfigParser.Parse("[S]\nk = v");

		Assert.IsType<ConfigDocument>(result);
		Assert.IsAssignableFrom<FrozenDictionary<string, string>>(result["S"]);
	}

	// ── Correctness fix #1: text after closing quote ───────────────

	[Fact]
	public void Parse_TextAfterClosingQuote_ThrowsFormatException()
	{
		var ex = Assert.Throws<FormatException>(() =>
			ConfigParser.Parse("[S]\nkey = \"value\"garbage"));

		Assert.Contains("after closing quote", ex.Message);
	}

	[Fact]
	public void Parse_TextAfterClosingQuoteBeforeNewline_ThrowsFormatException()
	{
		var ex = Assert.Throws<FormatException>(() =>
			ConfigParser.Parse("[S]\nkey = \"value\"extra\nother = 1"));

		Assert.Contains("after closing quote", ex.Message);
	}

	// ── Correctness fix #2: unterminated quoted value ──────────────

	[Fact]
	public void Parse_UnterminatedQuote_ThrowsFormatException()
	{
		var ex = Assert.Throws<FormatException>(() =>
			ConfigParser.Parse("[S]\nkey = \"unterminated value"));

		Assert.Contains("Unterminated quoted value", ex.Message);
	}

	[Fact]
	public void Parse_UnterminatedQuoteMultiline_ThrowsFormatException()
	{
		var ex = Assert.Throws<FormatException>(() =>
			ConfigParser.Parse("[S]\nkey = \"no end\nanother = val"));

		Assert.Contains("Unterminated quoted value", ex.Message);
	}

	// ── Correctness fix #3: unclosed section bracket ───────────────

	[Fact]
	public void Parse_UnclosedSectionBracket_ThrowsFormatException()
	{
		var ex = Assert.Throws<FormatException>(() =>
			ConfigParser.Parse("[Unclosed"));

		Assert.Contains("Unterminated section header", ex.Message);
	}

	[Fact]
	public void Parse_UnclosedSectionBracketWithContent_ThrowsFormatException()
	{
		var ex = Assert.Throws<FormatException>(() =>
			ConfigParser.Parse("[Unclosed\nkey = val"));

		// No ], #, or ; in remaining content, so scanner reaches EOF
		Assert.Contains("Unterminated section header", ex.Message);
	}

	// ── Existing error: comment char in section name ───────────────

	[Fact]
	public void Parse_CommentCharInSectionName_ThrowsFormatException()
	{
		Assert.Throws<FormatException>(() =>
			ConfigParser.Parse("[Bad#Name]"));
	}

	[Fact]
	public void Parse_SemicolonInSectionName_ThrowsFormatException()
	{
		Assert.Throws<FormatException>(() =>
			ConfigParser.Parse("[Bad;Name]"));
	}

	// ── Existing error: mid-value quote ────────────────────────────

	[Fact]
	public void Parse_MidValueQuote_ThrowsFormatException()
	{
		Assert.Throws<FormatException>(() =>
			ConfigParser.Parse("[S]\nkey = abc\"def\""));
	}

	// ── Complete example from spec ─────────────────────────────────

	[Fact]
	public void Parse_CompleteSpecExample_CorrectResults()
	{
		var input =
			"# Global settings\n" +
			"app_name = MyApp\n" +
			"version = 2.0\n" +
			"\n" +
			"[Server]\n" +
			"host = localhost   # inline comment stripped\n" +
			"port = 8080\n" +
			"\n" +
			"[Server]\n" +
			"port = 9090        # overwrites previous port\n" +
			"ssl = true         # added to [Server]\n" +
			"\n" +
			"[Database]\n" +
			"connection = \"Server=db;Port=5432;User=\"\"admin\"\"\"\n" +
			"timeout =\n" +
			"\n" +
			"[display]\n" +
			"theme = dark\n" +
			"\n" +
			"[Display]\n" +
			"font_size = 14     # merges with [display] (case-insensitive)\n";

		var result = ConfigParser.Parse(input);

		// Global section
		Assert.Equal("MyApp", result[""]["app_name"]);
		Assert.Equal("2.0", result[""]["version"]);

		// Server section (merged)
		Assert.Equal("localhost", result["Server"]["host"]);
		Assert.Equal("9090", result["Server"]["port"]);
		Assert.Equal("true", result["Server"]["ssl"]);

		// Database section
		Assert.Equal("Server=db;Port=5432;User=\"admin\"", result["Database"]["connection"]);
		Assert.Equal("", result["Database"]["timeout"]);

		// Display section (case-insensitive merge)
		Assert.Equal("dark", result["display"]["theme"]);
		Assert.Equal("14", result["display"]["font_size"]);
	}
}
