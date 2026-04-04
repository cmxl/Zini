using Microsoft.Extensions.Configuration;

namespace sINI.Configuration;

public class ConfigFileConfigurationProvider(ConfigFileConfigurationSource source)
	: FileConfigurationProvider(source)
{
	public override void Load(Stream stream)
	{
		using var reader = new StreamReader(stream);
		var content = reader.ReadToEnd();
		var parsed = ConfigParser.Parse(content.AsSpan());

		var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

		foreach (var section in parsed)
		{
			foreach (var kvp in section.Value)
			{
				var key = section.Key.Length == 0
					? kvp.Key
					: ConfigurationPath.Combine(section.Key, kvp.Key);

				data[key] = kvp.Value;
			}
		}

		Data = data;
	}
}
