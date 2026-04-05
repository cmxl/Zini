using System.Text;

namespace Zini;

public static class ConfigWriter
{
	public static string Write(ConfigDocument config)
	{
		var length = ComputeExactLength(config);
		if (length == 0) return string.Empty;

		return string.Create(length, config, static (span, cfg) =>
		{
			var nl = Environment.NewLine.AsSpan();
			var pos = 0;
			var needsBlankLine = false;

			if (cfg.HasGlobalKeys)
			{
				foreach (var kv in cfg[string.Empty])
					pos += WriteKeyValueToSpan(span[pos..], kv, nl);
				needsBlankLine = true;
			}

			foreach (var section in cfg.SectionNames)
			{
				if (needsBlankLine)
				{
					nl.CopyTo(span[pos..]);
					pos += nl.Length;
				}

				span[pos++] = '[';
				section.AsSpan().CopyTo(span[pos..]);
				pos += section.Length;
				span[pos++] = ']';
				nl.CopyTo(span[pos..]);
				pos += nl.Length;

				foreach (var kv in cfg[section])
					pos += WriteKeyValueToSpan(span[pos..], kv, nl);
				needsBlankLine = true;
			}
		});
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

			writer.Write('[');
			writer.Write(section);
			writer.Write(']');
			writer.WriteLine();
			foreach (var kv in config[section])
				WriteKeyValue(writer, kv);
			needsBlankLine = true;
		}
	}

	public static async Task WriteAsync(TextWriter writer, ConfigDocument config, CancellationToken cancellationToken = default)
	{
		var needsBlankLine = false;
		var sectionNames = config.SectionNamesArray;

		if (config.HasGlobalKeys)
		{
			foreach (var kv in config[string.Empty])
				await WriteKeyValueAsync(writer, kv, cancellationToken);
			needsBlankLine = true;
		}

		for (var i = 0; i < sectionNames.Length; i++)
		{
			var section = sectionNames[i];

			if (needsBlankLine)
				await writer.WriteLineAsync(ReadOnlyMemory<char>.Empty, cancellationToken);

			await writer.WriteAsync("[".AsMemory(), cancellationToken);
			await writer.WriteAsync(section.AsMemory(), cancellationToken);
			await writer.WriteLineAsync("]".AsMemory(), cancellationToken);
			foreach (var kv in config[section])
				await WriteKeyValueAsync(writer, kv, cancellationToken);
			needsBlankLine = true;
		}
	}

	// ── string.Create helpers ───────────────────────────────────────

	private static int ComputeExactLength(ConfigDocument config)
	{
		var nlLen = Environment.NewLine.Length;
		var len = 0;
		var needsBlankLine = false;

		if (config.HasGlobalKeys)
		{
			foreach (var kv in config[string.Empty])
				len += kv.Key.Length + 3 + ComputeValueLength(kv.Value) + nlLen;
			needsBlankLine = true;
		}

		foreach (var section in config.SectionNames)
		{
			if (needsBlankLine) len += nlLen;
			len += 1 + section.Length + 1 + nlLen;
			foreach (var kv in config[section])
				len += kv.Key.Length + 3 + ComputeValueLength(kv.Value) + nlLen;
			needsBlankLine = true;
		}

		return len;
	}

	private static int ComputeValueLength(string value)
	{
		if (!NeedsQuoting(value)) return value.Length;

		var extraQuotes = 0;
		var span = value.AsSpan();
		int idx;
		while ((idx = span.IndexOf('"')) >= 0)
		{
			extraQuotes++;
			span = span[(idx + 1)..];
		}

		return value.Length + extraQuotes + 2;
	}

	private static int WriteKeyValueToSpan(Span<char> dest, KeyValuePair<string, string> kv, ReadOnlySpan<char> newLine)
	{
		var pos = 0;
		kv.Key.AsSpan().CopyTo(dest[pos..]);
		pos += kv.Key.Length;
		" = ".AsSpan().CopyTo(dest[pos..]);
		pos += 3;
		pos += WriteValueToSpan(dest[pos..], kv.Value);
		newLine.CopyTo(dest[pos..]);
		pos += newLine.Length;
		return pos;
	}

	private static int WriteValueToSpan(Span<char> dest, string value)
	{
		if (!NeedsQuoting(value))
		{
			value.AsSpan().CopyTo(dest);
			return value.Length;
		}

		var pos = 0;
		dest[pos++] = '"';

		var src = value.AsSpan();
		int idx;
		while ((idx = src.IndexOf('"')) >= 0)
		{
			src[..(idx + 1)].CopyTo(dest[pos..]);
			pos += idx + 1;
			dest[pos++] = '"';
			src = src[(idx + 1)..];
		}
		src.CopyTo(dest[pos..]);
		pos += src.Length;

		dest[pos++] = '"';
		return pos;
	}

	// ── TextWriter helpers ──────────────────────────────────────────

	private static void WriteKeyValue(TextWriter writer, KeyValuePair<string, string> kv)
	{
		writer.Write(kv.Key);
		writer.Write(" = ");
		WriteValue(writer, kv.Value);
		writer.WriteLine();
	}

	private static void WriteValue(TextWriter writer, string value)
	{
		if (!NeedsQuoting(value))
		{
			writer.Write(value);
			return;
		}

		writer.Write('"');
		WriteEscaped(writer, value);
		writer.Write('"');
	}

	private static void WriteEscaped(TextWriter writer, string value)
	{
		var span = value.AsSpan();
		int idx;
		while ((idx = span.IndexOf('"')) >= 0)
		{
			writer.Write(span[..(idx + 1)]);
			writer.Write('"');
			span = span[(idx + 1)..];
		}
		writer.Write(span);
	}

	// ── Async helpers ───────────────────────────────────────────────

	private static async Task WriteKeyValueAsync(TextWriter writer, KeyValuePair<string, string> kv, CancellationToken cancellationToken)
	{
		await writer.WriteAsync(kv.Key.AsMemory(), cancellationToken);
		await writer.WriteAsync(" = ".AsMemory(), cancellationToken);
		await WriteValueAsync(writer, kv.Value, cancellationToken);
		await writer.WriteLineAsync(ReadOnlyMemory<char>.Empty, cancellationToken);
	}

	private static async Task WriteValueAsync(TextWriter writer, string value, CancellationToken cancellationToken)
	{
		if (!NeedsQuoting(value))
		{
			await writer.WriteAsync(value.AsMemory(), cancellationToken);
			return;
		}

		await writer.WriteAsync("\"".AsMemory(), cancellationToken);
		await WriteEscapedAsync(writer, value, cancellationToken);
		await writer.WriteAsync("\"".AsMemory(), cancellationToken);
	}

	private static async Task WriteEscapedAsync(TextWriter writer, string value, CancellationToken cancellationToken)
	{
		var mem = value.AsMemory();
		int idx;
		while ((idx = mem.Span.IndexOf('"')) >= 0)
		{
			await writer.WriteAsync(mem[..(idx + 1)], cancellationToken);
			await writer.WriteAsync("\"".AsMemory(), cancellationToken);
			mem = mem[(idx + 1)..];
		}
		await writer.WriteAsync(mem, cancellationToken);
	}

	// ── Shared predicate ────────────────────────────────────────────

	private static bool NeedsQuoting(string value)
		=> value.Length > 0
			&& (value[0] == ' ' || value[0] == '\t'
				|| value[^1] == ' ' || value[^1] == '\t'
				|| value.AsSpan().IndexOfAny('#', ';', '"') >= 0);
}
