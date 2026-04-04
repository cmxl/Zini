using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace sINI.Configuration;

public static class ConfigFileConfigurationExtensions
{
	public static IConfigurationBuilder AddConfigFile(
		this IConfigurationBuilder builder,
		string path)
		=> AddConfigFile(builder, provider: null, path, optional: false, reloadOnChange: false);

	public static IConfigurationBuilder AddConfigFile(
		this IConfigurationBuilder builder,
		string path,
		bool optional)
		=> AddConfigFile(builder, provider: null, path, optional, reloadOnChange: false);

	public static IConfigurationBuilder AddConfigFile(
		this IConfigurationBuilder builder,
		string path,
		bool optional,
		bool reloadOnChange)
		=> AddConfigFile(builder, provider: null, path, optional, reloadOnChange);

	public static IConfigurationBuilder AddConfigFile(
		this IConfigurationBuilder builder,
		IFileProvider? provider,
		string path,
		bool optional,
		bool reloadOnChange)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		return builder.Add<ConfigFileConfigurationSource>(source =>
		{
			source.FileProvider = provider;
			source.Path = path;
			source.Optional = optional;
			source.ReloadOnChange = reloadOnChange;
			source.ResolveFileProvider();
		});
	}
}
