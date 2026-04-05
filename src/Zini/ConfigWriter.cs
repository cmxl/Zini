using System.Text;

namespace Zini;

public static class ConfigWriter
{
	public static string Write(ConfigDocument config)
	{
		var sb = new StringBuilder();
		var needsBlankLine = false;

		if (config.HasGlobalKeys)
		{
			foreach (var kv in config[string.Empty])
				AppendKeyValue(sb, kv);
			needsBlankLine = true;
		}

		foreach (var section in config.SectionNames)
		{
			if (needsBlankLine)
				sb.AppendLine();

			sb.Append('[').Append(section).Append(']').AppendLine();
			foreach (var kv in config[section])
				AppendKeyValue(sb, kv);
			needsBlankLine = true;
		}

		return sb.ToString();
	}

	public static void Write(TextWriter writer, ConfigDocument config)
	{
		var needsBlankLine = false;

		if (config.HasGlobalKeys)
		{
			foreach (var kv in config[string.Empty])
				WriteKeyValue(writer, kv);
			needsBlankLine = true;
		}

		foreach (var section in config.SectionNames)
		{
			if (needsBlankLine)
				writer.WriteLine();

			writer.WriteLine("[{0}]", section);
			foreach (var kv in config[section])
				WriteKeyValue(writer, kv);
			needsBlankLine = true;
		}
	}

	public static async Task WriteAsync(TextWriter writer, ConfigDocument config, CancellationToken cancellationToken = default)
	{
		var needsBlankLine = false;

		if (config.HasGlobalKeys)
		{
			foreach (var kv in config[string.Empty])
			{
				await WriteKeyValueAsync(writer, kv, cancellationToken);
			}
			needsBlankLine = true;
		}

		foreach (var section in config.SectionNames)
		{
			if (needsBlankLine)
				await writer.WriteLineAsync(ReadOnlyMemory<char>.Empty, cancellationToken);

			await writer.WriteLineAsync($"[{section}]".AsMemory(), cancellationToken);
			foreach (var kv in config[section])
			{
				await WriteKeyValueAsync(writer, kv, cancellationToken);
			}
			needsBlankLine = true;
		}
	}

	private static void AppendKeyValue(StringBuilder sb, KeyValuePair<string, string> kv)
	{
		sb.Append(kv.Key).Append(" = ");
		AppendValue(sb, kv.Value);
		sb.AppendLine();
	}

	private static void AppendValue(StringBuilder sb, string value)
	{
		if (!NeedsQuoting(value))
		{
			sb.Append(value);
			return;
		}

		sb.Append('"');
		if (value.Contains('"'))
			sb.Append(value.Replace("\"", "\"\""));
		else
			sb.Append(value);
		sb.Append('"');
	}

	private static string FormatValue(string value)
	{
		if (!NeedsQuoting(value))
			return value;

		var sb = new StringBuilder(value.Length + 4);
		AppendValue(sb, value);
		return sb.ToString();
	}

	private static void WriteKeyValue(TextWriter writer, KeyValuePair<string, string> kv)
		=> writer.WriteLine("{0} = {1}", kv.Key, FormatValue(kv.Value));

	private static async Task WriteKeyValueAsync(TextWriter writer, KeyValuePair<string, string> kv, CancellationToken cancellationToken)
	{
		var line = $"{kv.Key} = {FormatValue(kv.Value)}";
		await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
	}

	private static bool NeedsQuoting(string value)
		=> value.Length > 0
			&& (value[0] == ' ' || value[0] == '\t'
				|| value[^1] == ' ' || value[^1] == '\t'
				|| value.AsSpan().IndexOfAny('#', ';', '"') >= 0);
}
