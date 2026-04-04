using System.Buffers;
using System.Runtime.CompilerServices;

public static class ConfigParser
{
	// SearchValues for SIMD-accelerated bulk scanning
	private static readonly SearchValues<char> s_whitespace = SearchValues.Create([' ', '\t', '\r', '\n']);
	private static readonly SearchValues<char> s_sectionEnd = SearchValues.Create([']', '#', ';']);
	private static readonly SearchValues<char> s_keyDelimiters = SearchValues.Create(['=', '\n', '#', ';']);
	private static readonly SearchValues<char> s_valueDelimiters = SearchValues.Create(['\n', '"', '#', ';']);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Parse(ReadOnlySpan<char> content)
	{
		var ctx = new ParseContext();
		ctx.Execute(content);

		// Remove the global section if it ended up empty
		if (ctx.Config.TryGetValue(string.Empty, out var global) && global.Count == 0)
			ctx.Config.Remove(string.Empty);

		return ctx.Config;
	}

	private ref struct ParseContext
	{
		// Store inner dicts as IReadOnlyDictionary directly — avoids the .ToDictionary() copy at the end
		public Dictionary<string, IReadOnlyDictionary<string, string>> Config;
		private Dictionary<string, string> _sectionDict;
		private string _currentSection;
		private string? _currentKey;

		public ParseContext()
		{
			Config = new(StringComparer.OrdinalIgnoreCase);
			_currentSection = string.Empty;
			_sectionDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			Config[_currentSection] = _sectionDict;
		}

		public void Execute(ReadOnlySpan<char> content)
		{
			var state = State.Data;
			var i = 0;
			var bufferStart = 0;
			var inQuotes = false;

			while (i < content.Length)
			{
				switch (state)
				{
					case State.Data:
					{
						// SIMD: skip all whitespace in bulk
						var offset = content[i..].IndexOfAnyExcept(s_whitespace);
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
						var offset = content[i..].IndexOfAny(s_sectionEnd);
						if (offset < 0) goto done;
						i += offset;

						if (SpecialChars.Comment.Contains(content[i]))
							throw new FormatException($"Invalid character '{content[i]}' in section name");

						_currentSection = content[bufferStart..i].Trim().ToString();
						if (Config.TryGetValue(_currentSection, out var existing))
							_sectionDict = (Dictionary<string, string>)existing;
						else
						{
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
						var offset = content[i..].IndexOfAny(s_keyDelimiters);
						if (offset < 0) goto done;
						i += offset;

						var c = content[i];
						if (c == SpecialChars.Delimiter)
						{
							_currentKey = content[bufferStart..i].Trim().ToString();
							i++;
							bufferStart = i;
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
							if (offset < 0) goto done;
							i += offset + 1;

							// Peek ahead: "" is an escaped quote, stay in quotes
							if (i < content.Length && content[i] == SpecialChars.Quote)
								i++;
							else
								inQuotes = false;
						}
						else
						{
							// SIMD: jump to newline, quote, or comment
							var offset = content[i..].IndexOfAny(s_valueDelimiters);
							if (offset < 0)
							{
								// Rest of content IS the value (no trailing newline)
								FlushValue(content[bufferStart..]);
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
								FlushValue(content[bufferStart..i]);
								i++;
								bufferStart = i;
								state = State.Data;
							}
							else // comment char
							{
								FlushValue(content[bufferStart..i]);
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
			if (state == State.Value && _currentKey != null && !inQuotes)
				FlushValue(content[bufferStart..]);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void FlushValue(ReadOnlySpan<char> raw)
		{
			var trimmed = raw.Trim();
			string value;

			if (trimmed.Length >= 2 && trimmed[0] == SpecialChars.Quote && trimmed[^1] == SpecialChars.Quote)
			{
				var inner = trimmed[1..^1];
				// Only allocate the Replace when there are actually escaped quotes
				value = inner.IndexOf("\"\"") >= 0
					? inner.ToString().Replace("\"\"", "\"")
					: inner.ToString();
			}
			else
			{
				value = trimmed.ToString();
			}

			_sectionDict[_currentKey!] = value;
			_currentKey = null;
		}
	}
}
