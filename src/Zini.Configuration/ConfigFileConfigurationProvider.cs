using Microsoft.Extensions.Configuration;

namespace Zini.Configuration;

public class ConfigFileConfigurationProvider(ConfigFileConfigurationSource source)
	: FileConfigurationProvider(source)
{
	public override void Load(Stream stream)
	{
		using var reader = new StreamReader(stream);
		var content = reader.ReadToEnd();
		var doc = ConfigParser.Parse(content.AsSpan());

		var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

		if (doc.HasGlobalKeys)
		{
			foreach (var kvp in doc.GetSection(string.Empty)!)
				data[kvp.Key] = kvp.Value;
		}

		foreach (var section in doc.SectionNames)
		{
			foreach (var kvp in doc.GetSection(section)!)
				data[ConfigurationPath.Combine(section, kvp.Key)] = kvp.Value;
		}

		Data = data;
	}
}
