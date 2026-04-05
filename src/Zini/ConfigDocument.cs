using System.Collections.Frozen;

namespace Zini;

public sealed class ConfigDocument
{
	private readonly FrozenDictionary<string, IReadOnlyDictionary<string, string>> _sections;

	internal ConfigDocument(FrozenDictionary<string, IReadOnlyDictionary<string, string>> sections)
	{
		_sections = sections;
	}

	public IReadOnlyDictionary<string, string> this[string section] => _sections[section];

	public IEnumerable<string> SectionNames => _sections.Keys.Where(k => k.Length > 0);

	public bool HasGlobalKeys => _sections.ContainsKey(string.Empty);

	public int Count => _sections.Count;

	public IReadOnlyDictionary<string, string>? GetSection(string section)
		=> _sections.GetValueOrDefault(section);

	public string? GetValue(string section, string key)
		=> _sections.TryGetValue(section, out var s) && s.TryGetValue(key, out var v) ? v : null;

	public string? GetGlobalValue(string key)
		=> GetValue(string.Empty, key);

	public bool ContainsSection(string section) => _sections.ContainsKey(section);
}
