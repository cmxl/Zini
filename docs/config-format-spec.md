# Config File Format Specification

This document defines the INI-style configuration file format parsed by `ConfigParser`.

## File Structure

A config file is a sequence of **lines** separated by `\n` (LF) or `\r\n` (CRLF). Each line is one of:

- A **blank line** (empty or whitespace-only) — ignored
- A **comment**
- A **section header**
- A **key-value pair**

## Comments

A line whose first non-whitespace character is `#` or `;` is a full-line comment.

```ini
# This is a comment
; This is also a comment
```

Inline comments are supported for unquoted values. Everything after `#` or `;` in a value is discarded:

```ini
host = localhost  # this part is stripped → value is "localhost"
```

Inline comments after section headers are also discarded:

```ini
[MySection] # this is ignored
```

## Sections

A section header is a line matching `[` *name* `]`, with optional surrounding whitespace inside the brackets:

```ini
[SectionName]
[ Spaced Section ]
```

The section name is trimmed of leading/trailing whitespace. Section names are **case-insensitive**: `[Server]` and `[server]` refer to the same section.

### Duplicate Sections

If the same section name appears more than once, the sections are **merged**. Keys from later occurrences are added to the existing section. If a key already exists, the later value **overwrites** the earlier one.

```ini
[Database]
host = alpha

[Database]
host = beta       # overwrites → "beta"
port = 5432       # added to [Database]
```

### Restrictions

Section names must not contain `#`, `;`, or `]`. The parser throws a `FormatException` if a comment character appears inside the brackets before the closing `]`.

## Global Keys

Key-value pairs that appear **before** any section header belong to the **global section**, stored under the empty-string key (`""`). If no global keys exist, the global section is omitted from the output.

```ini
app_name = MyApp     # global key

[Settings]
theme = dark
```

Result:

| Section | Key        | Value  |
|---------|------------|--------|
| `""`    | `app_name` | `MyApp` |
| `Settings` | `theme` | `dark` |

## Key-Value Pairs

A key-value pair is a line containing `=` as a delimiter:

```ini
key = value
```

- The **key** is everything before the first `=`, trimmed of whitespace.
- The **value** is everything after the first `=`, trimmed of whitespace (unless quoted).
- Additional `=` characters in the value are treated as literal: `key = a=b=c` → value is `a=b=c`.
- Keys are **case-insensitive**: `MyKey` and `mykey` are the same key.
- Duplicate keys within a section (including across merged duplicate sections) are resolved by **last-write-wins**.
- A line with no `=` that is not a section header or comment is silently skipped.

### Empty Values

A key with nothing (or only whitespace) after `=` produces an empty string value:

```ini
key =
```

### Whitespace Handling

Leading and trailing whitespace is trimmed from both keys and values:

```ini
tight=value          # key: "tight",  value: "value"
loose  =  value      # key: "loose",  value: "value"
```

## Quoted Values

Values wrapped in double quotes (`"`) are treated as quoted strings. The quotes are stripped from the result, and the content inside is preserved verbatim — including `#`, `;`, and leading/trailing whitespace.

```ini
message = "  hello world  "       # value: "  hello world  "
path = "value # not a comment"    # value: "value # not a comment"
```

### Rules

- Quotes must wrap the **entire** value (after trimming). A `"` appearing mid-value throws a `FormatException`.
- Only double quotes (`"`) are recognized. Single quotes (`'`) are treated as literal characters.
- Escaped quotes use doubling: `""` inside a quoted value produces a single `"`.

```ini
greeting = "she said ""hello"" to me"   # value: she said "hello" to me
```

## Output Format

`ConfigParser.Parse` returns a `ConfigDocument` — an immutable object wrapping the parsed sections and their key-value pairs. Both section names and keys use `OrdinalIgnoreCase` comparison.

```csharp
ConfigDocument doc = ConfigParser.Parse(input);

doc["Server"]["host"]            // indexer access (throws on missing)
doc.GetValue("Server", "host")   // safe access (returns null on missing)
doc.GetGlobalValue("app_name")   // shorthand for GetValue("", key)
doc.SectionNames                 // all section names (excludes global)
doc.HasGlobalKeys                // true if keys exist outside any section
doc.ContainsSection("Server")    // check if a section exists
doc.Count                        // total number of sections (including global if present)
```

## Error Handling

The parser throws `FormatException` in these cases:

| Condition | Message |
|-----------|---------|
| `#` or `;` inside a section name | `Invalid character '{c}' in section name` |
| `"` appearing mid-value (not wrapping the full value) | `Unexpected '"' in value — quotes must wrap the entire value` |

All other malformed lines (e.g., a line with no `=` outside a section header) are silently skipped.

## Complete Example

```ini
# Global settings
app_name = MyApp
version = 2.0

[Server]
host = localhost   # inline comment stripped
port = 8080

[Server]
port = 9090        # overwrites previous port
ssl = true         # added to [Server]

[Database]
connection = "Server=db;Port=5432;User=""admin"""
timeout =

[display]
theme = dark

[Display]
font_size = 14     # merges with [display] (case-insensitive)
```

Result:

| Section    | Key          | Value                              |
|------------|--------------|------------------------------------|
| `""`       | `app_name`   | `MyApp`                            |
| `""`       | `version`    | `2.0`                              |
| `Server`   | `host`       | `localhost`                        |
| `Server`   | `port`       | `9090`                             |
| `Server`   | `ssl`        | `true`                             |
| `Database` | `connection` | `Server=db;Port=5432;User="admin"` |
| `Database` | `timeout`    | *(empty string)*                   |
| `display`  | `theme`      | `dark`                             |
| `display`  | `font_size`  | `14`                               |
