/// <summary>
/// Original char-by-char implementation for benchmark comparison.
/// </summary>
public static class ConfigParserOriginal
{
	public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Parse(ReadOnlySpan<char> content)
	{
		var ctx = new ParseContext();

		for (var i = 0; i < content.Length; i++)
			ctx.State = ctx.Consume(content, i, content[i]);

		return ctx.Config.ToDictionary(
			x => x.Key,
			x => (IReadOnlyDictionary<string, string>)x.Value);
	}

	private ref struct ParseContext
	{
		public State State;
		public Dictionary<string, Dictionary<string, string>> Config;
		private int _bufferStart;
		private string? _currentSection;
		private string? _currentKey;
		private bool _inQuotes;
		private bool _pendingQuoteClose;

		public ParseContext()
		{
			Config = new();
			State = State.Data;
		}

		public State Consume(ReadOnlySpan<char> content, int i, char c)
		{
			return State switch
			{
				State.Data => HandleData(c, i),
				State.ConfigSectionOpen => HandleConfigSectionOpen(c, i, content),
				State.ConfigSectionClose => HandleConfigSectionClose(c, i),
				State.Key => HandleKey(c, i, content),
				State.Value => HandleValue(c, i, content),
				State.Comment => HandleComment(c),
				_ => State
			};
		}

		private State HandleData(char c, int i)
		{
			if (c == SpecialChars.SectionOpen)
			{
				_currentSection = null;
				_bufferStart = i + 1;
				return State.ConfigSectionOpen;
			}
			else if (char.IsWhiteSpace(c))
			{
				return State.Data;
			}
			else if (SpecialChars.Comment.Contains(c))
			{
				return State.Comment;
			}
			else if (_currentSection != null)
			{
				_bufferStart = i;
				return State.Key;
			}
			else
			{
				throw new FormatException("Missing Config Section before Key");
			}
		}

		private State HandleConfigSectionOpen(char c, int i, ReadOnlySpan<char> content)
		{
			if (SpecialChars.Comment.Contains(c))
				throw new FormatException($"Invalid character '{c}' in section name");

			if (c == SpecialChars.SectionClose)
			{
				_currentSection = content[_bufferStart..i].Trim().ToString();
				Config[_currentSection] = new Dictionary<string, string>();
				return State.ConfigSectionClose;
			}

			return State.ConfigSectionOpen;
		}

		private State HandleConfigSectionClose(char c, int i)
		{
			if (c == '\n')
			{
				_bufferStart = i + 1;
				return State.Data;
			}
			return State.ConfigSectionClose;
		}

		private State HandleKey(char c, int i, ReadOnlySpan<char> content)
		{
			if (c == SpecialChars.Delimiter)
			{
				_currentKey = content[_bufferStart..i].Trim().ToString();
				_bufferStart = i + 1;
				return State.Value;
			}
			else if (c == '\n')
			{
				_currentKey = null;
				return State.Data;
			}
			else if (SpecialChars.Comment.Contains(c))
			{
				_currentKey = null;
				return State.Comment;
			}
			return State.Key;
		}

		private State HandleValue(char c, int i, ReadOnlySpan<char> content)
		{
			if (_pendingQuoteClose)
			{
				_pendingQuoteClose = false;
				if (c == SpecialChars.Quote)
				{
					_inQuotes = true;
					return State.Value;
				}
				_inQuotes = false;
			}

			if (c == SpecialChars.Quote)
			{
				if (_inQuotes)
				{
					_pendingQuoteClose = true;
					return State.Value;
				}

				if (!content[_bufferStart..i].IsWhiteSpace())
					throw new FormatException("Unexpected '\"' in value — quotes must wrap the entire value");

				_inQuotes = true;
				return State.Value;
			}

			string? value = null;
			if (c == '\n')
			{
				value = content[_bufferStart..i].Trim().ToString();
			}
			else if (i == content.Length - 1)
			{
				value = content[_bufferStart..].Trim().ToString();
			}
			else if (SpecialChars.Comment.Contains(c) && !_inQuotes)
			{
				value = content[_bufferStart..i].Trim().ToString();
			}
			if (value != null)
			{
				if (value.Length >= 2 && value[0] == SpecialChars.Quote && value[^1] == SpecialChars.Quote)
					value = value[1..^1].Replace("\"\"", "\"");

				if (_currentSection != null && _currentKey != null)
					Config[_currentSection][_currentKey] = value;

				_currentKey = null;
				_inQuotes = false;
				_pendingQuoteClose = false;
				_bufferStart = i + 1;
				if (SpecialChars.Comment.Contains(c))
					return State.Comment;

				return State.Data;
			}
			return State.Value;
		}

		private State HandleComment(char c)
		{
			if (c == '\n')
				return State.Data;

			return State.Comment;
		}
	}
}
