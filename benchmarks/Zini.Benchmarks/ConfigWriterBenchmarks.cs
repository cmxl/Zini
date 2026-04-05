using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Zini;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class ConfigWriterBenchmarks
{
	private ConfigDocument _smallDoc = null!;
	private ConfigDocument _mediumDoc = null!;
	private ConfigDocument _largeDoc = null!;

	[GlobalSetup]
	public void Setup()
	{
		_smallDoc = ConfigParser.Parse(GenerateConfig(sections: 2, keysPerSection: 3));
		_mediumDoc = ConfigParser.Parse(GenerateConfig(sections: 20, keysPerSection: 15));
		_largeDoc = ConfigParser.Parse(GenerateConfig(sections: 100, keysPerSection: 50));
	}

	// --- Small ---

	[Benchmark(Description = "ToString"), BenchmarkCategory("Small")]
	public string Small_ToString() => ConfigWriter.Write(_smallDoc);

	[Benchmark(Description = "TextWriter"), BenchmarkCategory("Small")]
	public void Small_TextWriter() => ConfigWriter.Write(TextWriter.Null, _smallDoc);

	// --- Medium ---

	[Benchmark(Description = "ToString"), BenchmarkCategory("Medium")]
	public string Medium_ToString() => ConfigWriter.Write(_mediumDoc);

	[Benchmark(Description = "TextWriter"), BenchmarkCategory("Medium")]
	public void Medium_TextWriter() => ConfigWriter.Write(TextWriter.Null, _mediumDoc);

	// --- Large ---

	[Benchmark(Description = "ToString"), BenchmarkCategory("Large")]
	public string Large_ToString() => ConfigWriter.Write(_largeDoc);

	[Benchmark(Description = "TextWriter"), BenchmarkCategory("Large")]
	public void Large_TextWriter() => ConfigWriter.Write(TextWriter.Null, _largeDoc);

	private static string GenerateConfig(int sections, int keysPerSection)
	{
		var sb = new StringBuilder();

		sb.AppendLine("app_name = MyApp");
		sb.AppendLine("version = 2.0");
		sb.AppendLine();

		for (var s = 0; s < sections; s++)
		{
			sb.AppendLine($"[Section{s}]");
			for (var k = 0; k < keysPerSection; k++)
			{
				if (k % 7 == 0)
					sb.AppendLine($"quoted_key_{k} = \"value with spaces and #comments\"");
				else if (k % 11 == 0)
					sb.AppendLine($"escaped_key_{k} = \"she said \"\"hello\"\"\"");
				else
					sb.AppendLine($"key_{k} = value_{k}_in_section_{s}");
			}
			sb.AppendLine();
		}

		return sb.ToString();
	}
}
