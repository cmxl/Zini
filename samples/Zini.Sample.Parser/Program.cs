using Zini;

var configContent = await File.ReadAllTextAsync("sample.ini");

var config = ConfigParser.Parse(configContent);

if (config.HasGlobalKeys)
{
	Console.WriteLine("[Global]");
	foreach (var kvp in config.GetSection(string.Empty)!)
	{
		Console.WriteLine($"{kvp.Key} = {kvp.Value}");
	}
	Console.WriteLine();
}

foreach (var section in config.SectionNames)
{
	Console.WriteLine($"[{section}]");
	foreach (var kvp in config.GetSection(section)!)
	{
		Console.WriteLine($"{kvp.Key} = {kvp.Value}");
	}
	Console.WriteLine();
}
