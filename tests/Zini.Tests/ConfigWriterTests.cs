namespace Zini.Tests;

public class ConfigWriterTests
{
	// ── Basic writing ──────────────────────────────────────────────

	[Fact]
	public void Write_SimpleSection_ProducesExpectedOutput()
	{
		var doc = ConfigParser.Parse("[Server]\nhost = localhost\nport = 8080");
		var output = ConfigWriter.Write(doc);

		Assert.Contains("[Server]", output);
		Assert.Contains("host = localhost", output);
		Assert.Contains("port = 8080", output);
	}

	[Fact]
	public void Write_EmptyDocument_ReturnsEmptyString()
	{
		var doc = ConfigParser.Parse("");
		var output = ConfigWriter.Write(doc);

		Assert.Equal("", output);
	}

	[Fact]
	public void Write_GlobalKeys_WrittenBeforeSections()
	{
		var doc = ConfigParser.Parse("app = MyApp\n\n[Server]\nhost = localhost");
		var output = ConfigWriter.Write(doc);

		var appIndex = output.IndexOf("app = MyApp");
		var serverIndex = output.IndexOf("[Server]");
		Assert.True(appIndex < serverIndex);
	}

	[Fact]
	public void Write_EmptyValue_WrittenWithoutQuotes()
	{
		var doc = ConfigParser.Parse("[S]\nkey =");
		var output = ConfigWriter.Write(doc);

		Assert.Contains("key =", output);
		Assert.DoesNotContain("\"", output);
	}

	// ── Quoting ────────────────────────────────────────────────────

	[Fact]
	public void Write_SimpleValue_NotQuoted()
	{
		var doc = ConfigParser.Parse("[S]\nhost = localhost");
		var output = ConfigWriter.Write(doc);

		Assert.Contains("host = localhost", output);
		Assert.DoesNotContain("\"", output);
	}

	[Fact]
	public void Write_ValueWithHashComment_Quoted()
	{
		var doc = ConfigParser.Parse("[S]\npath = \"value # not a comment\"");
		var output = ConfigWriter.Write(doc);

		Assert.Contains("path = \"value # not a comment\"", output);
	}

	[Fact]
	public void Write_ValueWithSemicolonComment_Quoted()
	{
		var doc = ConfigParser.Parse("[S]\npath = \"value ; not a comment\"");
		var output = ConfigWriter.Write(doc);

		Assert.Contains("path = \"value ; not a comment\"", output);
	}

	[Fact]
	public void Write_ValueWithLeadingWhitespace_Quoted()
	{
		var doc = ConfigParser.Parse("[S]\nmsg = \"  hello\"");
		var output = ConfigWriter.Write(doc);

		Assert.Contains("msg = \"  hello\"", output);
	}

	[Fact]
	public void Write_ValueWithTrailingWhitespace_Quoted()
	{
		var doc = ConfigParser.Parse("[S]\nmsg = \"hello  \"");
		var output = ConfigWriter.Write(doc);

		Assert.Contains("msg = \"hello  \"", output);
	}

	[Fact]
	public void Write_ValueWithQuotes_EscapedAndQuoted()
	{
		var doc = ConfigParser.Parse("[S]\ngreeting = \"she said \"\"hello\"\" to me\"");
		var output = ConfigWriter.Write(doc);

		Assert.Contains("greeting = \"she said \"\"hello\"\" to me\"", output);
	}

	// ── Blank line separation ──────────────────────────────────────

	[Fact]
	public void Write_MultipleSections_SeparatedByBlankLines()
	{
		var doc = ConfigParser.Parse("[A]\nx = 1\n[B]\ny = 2");
		var output = ConfigWriter.Write(doc);

		var lines = output.TrimEnd().Split(Environment.NewLine);
		var blankIndex = Array.IndexOf(lines, "");
		Assert.True(blankIndex > 0, "Expected a blank line between sections");
		Assert.Contains("x = 1", lines[blankIndex - 1]);
		Assert.Equal("[B]", lines[blankIndex + 1]);
	}

	[Fact]
	public void Write_GlobalKeysAndSection_SeparatedByBlankLine()
	{
		var doc = ConfigParser.Parse("app = MyApp\n[S]\nkey = val");
		var output = ConfigWriter.Write(doc);

		var lines = output.Split('\n');
		// Find the blank line between global keys and section
		var blankLineFound = false;
		for (var i = 1; i < lines.Length - 1; i++)
		{
			if (lines[i].Trim().Length == 0 && lines[i - 1].Contains("MyApp"))
				blankLineFound = true;
		}
		Assert.True(blankLineFound, "Expected blank line between global keys and first section");
	}

	// ── TextWriter overload ────────────────────────────────────────

	[Fact]
	public void Write_TextWriter_ProducesSameOutputAsStringOverload()
	{
		var doc = ConfigParser.Parse("[Server]\nhost = localhost\nport = 8080");

		var stringResult = ConfigWriter.Write(doc);

		using var sw = new StringWriter();
		ConfigWriter.Write(sw, doc);
		var writerResult = sw.ToString();

		Assert.Equal(stringResult, writerResult);
	}

	// ── Async overload ─────────────────────────────────────────────

	[Fact]
	public async Task WriteAsync_ProducesSameOutputAsSyncOverload()
	{
		var doc = ConfigParser.Parse("app = MyApp\n[Server]\nhost = localhost");

		var syncResult = ConfigWriter.Write(doc);

		using var sw = new StringWriter();
		await ConfigWriter.WriteAsync(sw, doc);
		var asyncResult = sw.ToString();

		Assert.Equal(syncResult, asyncResult);
	}

	// ── Round-trip: Parse → Write → Parse ──────────────────────────

	[Fact]
	public void RoundTrip_SimpleValues_PreservesData()
	{
		var original = ConfigParser.Parse("[Server]\nhost = localhost\nport = 8080");
		var written = ConfigWriter.Write(original);
		var reparsed = ConfigParser.Parse(written);

		Assert.Equal(original["Server"]["host"], reparsed["Server"]["host"]);
		Assert.Equal(original["Server"]["port"], reparsed["Server"]["port"]);
	}

	[Fact]
	public void RoundTrip_GlobalKeys_PreservesData()
	{
		var original = ConfigParser.Parse("app = MyApp\nversion = 2.0");
		var written = ConfigWriter.Write(original);
		var reparsed = ConfigParser.Parse(written);

		Assert.Equal("MyApp", reparsed.GetGlobalValue("app"));
		Assert.Equal("2.0", reparsed.GetGlobalValue("version"));
	}

	[Fact]
	public void RoundTrip_QuotedValueWithWhitespace_PreservesData()
	{
		var original = ConfigParser.Parse("[S]\nmsg = \"  hello world  \"");
		var written = ConfigWriter.Write(original);
		var reparsed = ConfigParser.Parse(written);

		Assert.Equal("  hello world  ", reparsed["S"]["msg"]);
	}

	[Fact]
	public void RoundTrip_QuotedValueWithCommentChars_PreservesData()
	{
		var original = ConfigParser.Parse("[S]\npath = \"value # not a comment\"");
		var written = ConfigWriter.Write(original);
		var reparsed = ConfigParser.Parse(written);

		Assert.Equal("value # not a comment", reparsed["S"]["path"]);
	}

	[Fact]
	public void RoundTrip_EscapedQuotes_PreservesData()
	{
		var original = ConfigParser.Parse("[S]\ngreeting = \"she said \"\"hello\"\" to me\"");
		var written = ConfigWriter.Write(original);
		var reparsed = ConfigParser.Parse(written);

		Assert.Equal("she said \"hello\" to me", reparsed["S"]["greeting"]);
	}

	[Fact]
	public void RoundTrip_EmptyValue_PreservesData()
	{
		var original = ConfigParser.Parse("[S]\nkey =");
		var written = ConfigWriter.Write(original);
		var reparsed = ConfigParser.Parse(written);

		Assert.Equal("", reparsed["S"]["key"]);
	}

	[Fact]
	public void RoundTrip_MultipleSections_PreservesAllData()
	{
		var original = ConfigParser.Parse("[A]\nx = 1\n[B]\ny = 2\n[C]\nz = 3");
		var written = ConfigWriter.Write(original);
		var reparsed = ConfigParser.Parse(written);

		Assert.Equal("1", reparsed["A"]["x"]);
		Assert.Equal("2", reparsed["B"]["y"]);
		Assert.Equal("3", reparsed["C"]["z"]);
	}

	[Fact]
	public void RoundTrip_GlobalKeysAndSections_PreservesAllData()
	{
		var original = ConfigParser.Parse("app = MyApp\n\n[Server]\nhost = localhost\nport = 8080\n\n[Database]\ntimeout =");
		var written = ConfigWriter.Write(original);
		var reparsed = ConfigParser.Parse(written);

		Assert.Equal("MyApp", reparsed.GetGlobalValue("app"));
		Assert.Equal("localhost", reparsed["Server"]["host"]);
		Assert.Equal("8080", reparsed["Server"]["port"]);
		Assert.Equal("", reparsed["Database"]["timeout"]);
	}

	[Fact]
	public void RoundTrip_CompleteSpecExample_PreservesAllValues()
	{
		var input =
			"app_name = MyApp\n" +
			"version = 2.0\n" +
			"\n" +
			"[Server]\n" +
			"host = localhost\n" +
			"port = 9090\n" +
			"ssl = true\n" +
			"\n" +
			"[Database]\n" +
			"connection = \"Server=db;Port=5432;User=\"\"admin\"\"\"\n" +
			"timeout =\n" +
			"\n" +
			"[display]\n" +
			"theme = dark\n" +
			"font_size = 14\n";

		var original = ConfigParser.Parse(input);
		var written = ConfigWriter.Write(original);
		var reparsed = ConfigParser.Parse(written);

		Assert.Equal("MyApp", reparsed.GetGlobalValue("app_name"));
		Assert.Equal("2.0", reparsed.GetGlobalValue("version"));
		Assert.Equal("localhost", reparsed["Server"]["host"]);
		Assert.Equal("9090", reparsed["Server"]["port"]);
		Assert.Equal("true", reparsed["Server"]["ssl"]);
		Assert.Equal("Server=db;Port=5432;User=\"admin\"", reparsed["Database"]["connection"]);
		Assert.Equal("", reparsed["Database"]["timeout"]);
		Assert.Equal("dark", reparsed["display"]["theme"]);
		Assert.Equal("14", reparsed["display"]["font_size"]);
	}

	[Fact]
	public void RoundTrip_ValueWithOnlyQuote_PreservesData()
	{
		var original = ConfigParser.Parse("[S]\nkey = \"\"\"\"");
		var written = ConfigWriter.Write(original);
		var reparsed = ConfigParser.Parse(written);

		Assert.Equal("\"", reparsed["S"]["key"]);
	}

	[Fact]
	public void RoundTrip_ValueWithEqualsSign_PreservesData()
	{
		var original = ConfigParser.Parse("[S]\nkey = a=b=c");
		var written = ConfigWriter.Write(original);
		var reparsed = ConfigParser.Parse(written);

		Assert.Equal("a=b=c", reparsed["S"]["key"]);
	}

	[Fact]
	public void RoundTrip_DoubleRoundTrip_Stable()
	{
		var input = "app = MyApp\n[S]\npath = \"C:\\temp # files\"\nplain = hello\nempty =\nquoted = \"she said \"\"hi\"\"\"";

		var first = ConfigWriter.Write(ConfigParser.Parse(input));
		var second = ConfigWriter.Write(ConfigParser.Parse(first));

		Assert.Equal(first, second);
	}
}
