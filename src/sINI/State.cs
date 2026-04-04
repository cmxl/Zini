internal enum State
{
	/// <summary>
	/// The default state, where the parser is looking for the next token.
	/// </summary>
	Data,
	/// <summary>
	/// Opening bracket of a section, e.g. [User]
	/// </summary>	
	ConfigSectionOpen,
	/// <summary>
	/// Closing bracket of a section, e.g. [User]
	/// </summary>
	ConfigSectionClose,
	/// <summary>
	/// The parser is looking for a key.
	/// </summary>
	Key,
	/// <summary>
	/// The parser is looking for a value.
	/// </summary>
	Value,
	/// <summary>
	/// # is the start of a comment. Everything in the same line after the # will be ignored
	/// </summary>
	Comment
}