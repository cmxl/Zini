using sINI;

var configContent = await File.ReadAllTextAsync("some.config");

var config = ConfigParser.Parse(configContent);

foreach (var section in config)
{
	Console.WriteLine(section.Key.Length == 0 ? "[Global]" : $"[{section.Key}]");
	foreach (var kvp in section.Value)
	{
		Console.WriteLine($"{kvp.Key} = {kvp.Value}");
	}
	Console.WriteLine();
}
