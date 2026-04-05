using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace Zini;

public static class ConfigParser
{
	public static ConfigDocument Parse(string content)
		=> Parse(content.AsSpan());

	public static ConfigDocument Parse(ReadOnlySpan<char> content)
	{
		var ctx = new ParseContext();
		ctx.Execute(content);

		var frozen = ctx.Config.ToFrozenDictionary(
			kvp => kvp.Key,
			kvp => kvp.Value.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
			StringComparer.OrdinalIgnoreCase);

		return new ConfigDocument(frozen);
	}

	private ref struct ParseContext
	{
		public Dictionary<string, Dictionary<string, string>> Config;
		private Dictionary<string, string>? _sectionDict;
		private string _currentSection;
		private string? _currentKey;

		public ParseContext()
		{
			Config = new(StringComparer.OrdinalIgnoreCase);
			_currentSection = string.Empty;
		}

		public void Execute(ReadOnlySpan<char> content)
		{
			var state = State.Data;
			var i = 0;
			var bufferStart = 0;
			var inQuotes = false;
			var wasQuoted = false;

			while (i < content.Length)
			{
				switch (state)
				{
					case State.Data:
					{
						// SIMD: skip all whitespace in bulk
						var offset = content[i..].IndexOfAnyExcept(SpecialChars.Whitespace);
						if (offset < 0) goto done;
						i += offset;

						var c = content[i];
						if (c == SpecialChars.SectionOpen)
						{
							bufferStart = i + 1;
							state = State.ConfigSectionOpen;
							i++;
						}
						else if (SpecialChars.Comment.Contains(c))
						{
							state = State.Comment;
							i++;
						}
						else
						{
							bufferStart = i;
							state = State.Key;
						}
						break;
					}

					case State.ConfigSectionOpen:
					{
						// SIMD: jump straight to ] or comment char
						var offset = content[i..].IndexOfAny(SpecialChars.SectionEnd);
						if (offset < 0)
							throw new FormatException("Unterminated section header — expected ']'");

						i += offset;

						if (SpecialChars.Comment.Contains(content[i]))
							throw new FormatException($"Invalid character '{content[i]}' in section name");

						var sectionSpan = content[bufferStart..i].Trim();
						var lookup = Config.GetAlternateLookup<ReadOnlySpan<char>>();
						if (lookup.TryGetValue(sectionSpan, out var existing))
						{
							// _currentSection is intentionally not updated here — it's only
							// read in FlushValue when _sectionDict is null, which can't happen
							// after a cache hit since we set _sectionDict below.
							_sectionDict = existing;
						}
						else
						{
							_currentSection = sectionSpan.ToString();
							_sectionDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
							Config[_currentSection] = _sectionDict;
						}
						state = State.ConfigSectionClose;
						i++;
						break;
					}

					// Both states just need to skip to the next newline
					case State.ConfigSectionClose:
					case State.Comment:
					{
						// SIMD: skip entire line in one call
						var offset = content[i..].IndexOf('\n');
						if (offset < 0) goto done;
						i += offset + 1;
						state = State.Data;
						break;
					}

					case State.Key:
					{
						// SIMD: jump to = or line end or comment
						var offset = content[i..].IndexOfAny(SpecialChars.KeyDelimiters);
						if (offset < 0) goto done;
						i += offset;

						var c = content[i];
						if (c == SpecialChars.Delimiter)
						{
							_currentKey = content[bufferStart..i].Trim().ToString();
							i++;
							bufferStart = i;
							wasQuoted = false;
							state = State.Value;
						}
						else if (c == '\n')
						{
							_currentKey = null;
							i++;
							state = State.Data;
						}
						else
						{
							_currentKey = null;
							i++;
							state = State.Comment;
						}
						break;
					}

					case State.Value:
					{
						if (inQuotes)
						{
							// SIMD: jump to next quote
							var offset = content[i..].IndexOf(SpecialChars.Quote);
							if (offset < 0)
								throw new FormatException("Unterminated quoted value — expected closing '\"'");

							i += offset + 1;

							// Peek ahead: "" is an escaped quote, stay in quotes
							if (i < content.Length && content[i] == SpecialChars.Quote)
							{
								i++;
							}
							else
							{
								inQuotes = false;
								wasQuoted = true;

								// Validate: only whitespace, comment, or newline may follow closing quote
								var rest = content[i..];
								var endOffset = rest.IndexOfAny('\n', '#', ';');
								if (endOffset < 0)
								{
									// EOF after closing quote
									if (!rest.IsWhiteSpace() && rest.Length > 0)
										throw new FormatException("Unexpected content after closing quote");
									FlushValue(content[bufferStart..i], wasQuoted: true);
									goto done;
								}
								if (!rest[..endOffset].IsWhiteSpace())
									throw new FormatException("Unexpected content after closing quote");
								FlushValue(content[bufferStart..(i + endOffset)], wasQuoted: true);
								i += endOffset;
								if (content[i] == '\n')
								{
									i++;
									state = State.Data;
								}
								else
								{
									i++;
									state = State.Comment;
								}
							}
						}
						else
						{
							// SIMD: jump to newline, quote, or comment
							var offset = content[i..].IndexOfAny(SpecialChars.ValueDelimiters);
							if (offset < 0)
							{
								// Rest of content IS the value (no trailing newline)
								FlushValue(content[bufferStart..], wasQuoted: false);
								goto done;
							}

							i += offset;
							var c = content[i];

							if (c == SpecialChars.Quote)
							{
								if (!content[bufferStart..i].IsWhiteSpace())
									throw new FormatException("Unexpected '\"' in value — quotes must wrap the entire value");
								inQuotes = true;
								i++;
							}
							else if (c == '\n')
							{
								FlushValue(content[bufferStart..i], wasQuoted: false);
								i++;
								bufferStart = i;
								state = State.Data;
							}
							else // comment char
							{
								FlushValue(content[bufferStart..i], wasQuoted: false);
								i++;
								state = State.Comment;
							}
						}
						break;
					}
				}
			}

			done:
			// Handle value at EOF without trailing newline (but not unterminated quotes)
			if (state == State.Value && _currentKey != null && !inQuotes && !wasQuoted)
				FlushValue(content[bufferStart..], wasQuoted: false);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void FlushValue(ReadOnlySpan<char> raw, bool wasQuoted)
		{
			if (_sectionDict is null)
			{
				_sectionDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				Config[_currentSection] = _sectionDict;
			}

			var trimmed = raw.Trim();
			string value;

			if (wasQuoted && trimmed.Length >= 2
				&& trimmed[0] == SpecialChars.Quote && trimmed[^1] == SpecialChars.Quote)
			{
				var inner = trimmed[1..^1];
				value = inner.IndexOf("\"\"") >= 0
					? UnescapeQuotes(inner)
					: inner.ToString();
			}
			else
			{
				value = trimmed.ToString();
			}

			_sectionDict[_currentKey!] = value;
			_currentKey = null;
		}

		private static string UnescapeQuotes(ReadOnlySpan<char> inner)
		{
			// Count escaped pairs to determine output length
			int escapedCount = 0;
			for (int j = 0; j < inner.Length - 1; j++)
			{
				if (inner[j] == '"' && inner[j + 1] == '"')
				{
					escapedCount++;
					j++;
				}
			}

			int outputLen = inner.Length - escapedCount;
			Span<char> buffer = outputLen <= 256
				? stackalloc char[outputLen]
				: new char[outputLen];

			int di = 0;
			for (int si = 0; si < inner.Length; si++)
			{
				buffer[di++] = inner[si];
				if (inner[si] == '"' && si + 1 < inner.Length && inner[si + 1] == '"')
					si++;
			}

			return new string(buffer);
		}
	}
}
