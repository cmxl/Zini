using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using IniParser.Model;
using IniParser.Parser;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
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

	[Benchmark(Description = "Small - SIMD")]
	public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Small_Simd()
		=> ConfigParser.Parse(_smallConfig);

	[Benchmark(Description = "Small - Original", Baseline = true)]
	public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Small_Original()
		=> ConfigParserOriginal.Parse(_smallConfig);

	[Benchmark(Description = "Small - ini-parser")]
	public IniData Small_IniParser()
		=> _iniParser.Parse(_smallConfig);

	// --- Medium config (realistic application config) ---

	[Benchmark(Description = "Medium - SIMD")]
	public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Medium_Simd()
		=> ConfigParser.Parse(_mediumConfig);

	[Benchmark(Description = "Medium - Original")]
	public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Medium_Original()
		=> ConfigParserOriginal.Parse(_mediumConfig);

	[Benchmark(Description = "Medium - ini-parser")]
	public IniData Medium_IniParser()
		=> _iniParser.Parse(_mediumConfig);

	// --- Large config (stress test) ---

	[Benchmark(Description = "Large - SIMD")]
	public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Large_Simd()
		=> ConfigParser.Parse(_largeConfig);

	[Benchmark(Description = "Large - Original")]
	public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Large_Original()
		=> ConfigParserOriginal.Parse(_largeConfig);

	[Benchmark(Description = "Large - ini-parser")]
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
