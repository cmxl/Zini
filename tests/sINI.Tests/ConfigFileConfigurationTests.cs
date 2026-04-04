using Microsoft.Extensions.Configuration;
using sINI.Configuration;

namespace sINI.Tests;

public class ConfigFileConfigurationTests
{
	private static IConfiguration BuildFromString(string content)
	{
		var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dir);
		var file = Path.Combine(dir, "test.config");
		File.WriteAllText(file, content);

		try
		{
			return new ConfigurationBuilder()
				.SetBasePath(dir)
				.AddConfigFile("test.config")
				.Build();
		}
		finally
		{
			File.Delete(file);
			Directory.Delete(dir);
		}
	}

	// ── Key flattening ─────────────────────────────────────────────

	[Fact]
	public void SectionKeys_FlattenedWithColon()
	{
		var config = BuildFromString("[Server]\nhost = localhost\nport = 8080");

		Assert.Equal("localhost", config["Server:host"]);
		Assert.Equal("8080", config["Server:port"]);
	}

	[Fact]
	public void GlobalKeys_NoPrefix()
	{
		var config = BuildFromString("app = MyApp\n[S]\nkey = val");

		Assert.Equal("MyApp", config["app"]);
		Assert.Equal("val", config["S:key"]);
	}

	[Fact]
	public void NestedSectionAccess_ViaGetSection()
	{
		var config = BuildFromString("[Database]\nhost = db.local\nport = 5432");

		var section = config.GetSection("Database");
		Assert.Equal("db.local", section["host"]);
		Assert.Equal("5432", section["port"]);
	}

	// ── Case insensitivity ─────────────────────────────────────────

	[Fact]
	public void Keys_CaseInsensitiveLookup()
	{
		var config = BuildFromString("[Server]\nHost = localhost");

		Assert.Equal("localhost", config["server:host"]);
		Assert.Equal("localhost", config["SERVER:HOST"]);
	}

	// ── Merged sections ────────────────────────────────────────────

	[Fact]
	public void DuplicateSections_MergedInConfiguration()
	{
		var config = BuildFromString("[S]\na = 1\n\n[S]\nb = 2\na = 3");

		Assert.Equal("3", config["S:a"]);
		Assert.Equal("2", config["S:b"]);
	}

	// ── Quoted values preserved ────────────────────────────────────

	[Fact]
	public void QuotedValues_PreservedThroughProvider()
	{
		var config = BuildFromString("[S]\npath = \"C:\\Program Files\"");

		Assert.Equal("C:\\Program Files", config["S:path"]);
	}

	// ── Empty values ───────────────────────────────────────────────

	[Fact]
	public void EmptyValue_ReturnsEmptyString()
	{
		var config = BuildFromString("[S]\nkey =");

		Assert.Equal("", config["S:key"]);
	}

	// ── Optional file ──────────────────────────────────────────────

	[Fact]
	public void OptionalFile_DoesNotThrowWhenMissing()
	{
		var config = new ConfigurationBuilder()
			.AddConfigFile("nonexistent.config", optional: true)
			.Build();

		Assert.Null(config["anything"]);
	}

	[Fact]
	public void RequiredFile_ThrowsWhenMissing()
	{
		Assert.Throws<FileNotFoundException>(() =>
			new ConfigurationBuilder()
				.AddConfigFile("nonexistent.config", optional: false)
				.Build());
	}

	// ── Multiple files ─────────────────────────────────────────────

	[Fact]
	public void MultipleFiles_LaterOverridesEarlier()
	{
		var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dir);
		File.WriteAllText(Path.Combine(dir, "base.config"), "[S]\nkey = base\nother = keep");
		File.WriteAllText(Path.Combine(dir, "override.config"), "[S]\nkey = override");

		try
		{
			var config = new ConfigurationBuilder()
				.SetBasePath(dir)
				.AddConfigFile("base.config")
				.AddConfigFile("override.config")
				.Build();

			Assert.Equal("override", config["S:key"]);
			Assert.Equal("keep", config["S:other"]);
		}
		finally
		{
			Directory.Delete(dir, recursive: true);
		}
	}
}
