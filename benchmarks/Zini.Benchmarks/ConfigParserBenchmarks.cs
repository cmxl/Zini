using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using IniParser.Model;
using IniParser.Parser;
using Zini;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class ConfigParserBenchmarks
{
	private string _smallConfig = null!;
	private string _mediumConfig = null!;
	private string _largeConfig = null!;
	private IniDataParser _iniParser = null!;

	[GlobalSetup]
	public void Setup()
	{
		_smallConfig = GenerateConfig(sections: 2, keysPerSection: 3, commentFrequency: 2);
		_mediumConfig = GenerateConfig(sections: 20, keysPerSection: 15, commentFrequency: 3);
		_largeConfig = GenerateConfig(sections: 100, keysPerSection: 50, commentFrequency: 4);
		_iniParser = new IniDataParser();
		_iniParser.Configuration.AllowKeysWithoutSection = true;
		_iniParser.Configuration.CommentString = "#";
	}

	// --- Small config (typical single-component config) ---

	[Benchmark(Description = "Zini"), BenchmarkCategory("Small")]
	public ConfigDocument Small_Zini()
		=> ConfigParser.Parse(_smallConfig);

	[Benchmark(Description = "ini-parser", Baseline = true), BenchmarkCategory("Small")]
	public IniData Small_IniParser()
		=> _iniParser.Parse(_smallConfig);

	// --- Medium config (realistic application config) ---

	[Benchmark(Description = "Zini"), BenchmarkCategory("Medium")]
	public ConfigDocument Medium_Zini()
		=> ConfigParser.Parse(_mediumConfig);

	[Benchmark(Description = "ini-parser", Baseline = true), BenchmarkCategory("Medium")]
	public IniData Medium_IniParser()
		=> _iniParser.Parse(_mediumConfig);

	// --- Large config (stress test) ---

	[Benchmark(Description = "Zini"), BenchmarkCategory("Large")]
	public ConfigDocument Large_Zini()
		=> ConfigParser.Parse(_largeConfig);

	[Benchmark(Description = "ini-parser", Baseline = true), BenchmarkCategory("Large")]
	public IniData Large_IniParser()
		=> _iniParser.Parse(_largeConfig);

	private static string GenerateConfig(int sections, int keysPerSection, int commentFrequency)
	{
		var sb = new StringBuilder();
		sb.AppendLine("# Generated config for benchmarking");
		sb.AppendLine();

		for (var s = 0; s < sections; s++)
		{
			sb.AppendLine($"[Section{s}]");
			for (var k = 0; k < keysPerSection; k++)
			{
				if (k % commentFrequency == 0)
					sb.AppendLine($"# This is a comment for key {k} in section {s}");

				if (k % 7 == 0)
					sb.AppendLine($"quoted_key_{k} = \"value with spaces and quotes\"");
				else
					sb.AppendLine($"key_{k} = value_{k}_in_section_{s}");
			}
			sb.AppendLine();
		}

		return sb.ToString();
	}
}
