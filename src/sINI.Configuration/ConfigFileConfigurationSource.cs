using Microsoft.Extensions.Configuration;

namespace sINI.Configuration;

public class ConfigFileConfigurationSource : FileConfigurationSource
{
	public override IConfigurationProvider Build(IConfigurationBuilder builder)
	{
		EnsureDefaults(builder);
		return new ConfigFileConfigurationProvider(this);
	}
}
