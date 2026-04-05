using System.Collections.Frozen;

namespace Zini;

public sealed class ConfigDocument
{
	private readonly FrozenDictionary<string, FrozenDictionary<string, string>> _sections;
	private readonly string[] _sectionNames;

	internal ConfigDocument(FrozenDictionary<string, FrozenDictionary<string, string>> sections)
	{
		_sections = sections;

		int count = 0;
		foreach (var key in sections.Keys)
			if (key.Length > 0) count++;

		var names = new string[count];
		int i = 0;
		foreach (var key in sections.Keys)
			if (key.Length > 0) names[i++] = key;

		_sectionNames = names;
	}

	public FrozenDictionary<string, string> this[string section] => _sections[section];

	public ReadOnlySpan<string> SectionNames => _sectionNames;

	internal string[] SectionNamesArray => _sectionNames;

	public bool HasGlobalKeys => _sections.ContainsKey(string.Empty);

	public int Count => _sections.Count;

	public FrozenDictionary<string, string>? GetSection(string section)
		=> _sections.TryGetValue(section, out var s) ? s : null;

	public string? GetValue(string section, string key)
		=> _sections.TryGetValue(section, out var s) && s.TryGetValue(key, out var v) ? v : null;

	public string? GetGlobalValue(string key)
		=> GetValue(string.Empty, key);

	public bool ContainsSection(string section) => _sections.ContainsKey(section);
}
