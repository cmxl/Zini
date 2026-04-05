# Zini

A high-performance, zero-allocation INI-style configuration file parser for .NET 10 with `Microsoft.Extensions.Configuration` integration.

## Features

- **Zero-allocation hot path** — uses `SearchValues<char>` for SIMD-accelerated parsing
- **Immutable output** — returns `ConfigDocument` backed by `FrozenDictionary` instances
- **Round-trip support** — `ConfigWriter` serializes back to INI with smart quoting and quote escaping
- **Case-insensitive** sections and keys with `OrdinalIgnoreCase` comparison
- **Section merging** — duplicate sections merge automatically (last-write-wins for keys)
- **Quoted values** — preserves whitespace and comment characters inside `"..."`
- **Inline comments** — `#` and `;` stripped from unquoted values
- **Global keys** — keys before any section header are supported
- **Configuration integration** — drop-in `IConfigurationBuilder.AddConfigFile()` extension

## Installation

| Package | Status |
| --- | --- |
| Zini | [![Zini](https://img.shields.io/nuget/dt/Zini)](https://www.nuget.org/packages/Zini) |
| Zini.Configuration | [![Zini.Configuration](https://img.shields.io/nuget/dt/Zini.Configuration)](https://www.nuget.org/packages/Zini.Configuration) |

```shell
dotnet add package Zini.Configuration  # IConfigurationBuilder support
```

## Quick Start

### Direct Parsing

```csharp
using Zini;

var doc = ConfigParser.Parse("""
    # Application settings
    app_name = MyApp

    [Server]
    host = localhost
    port = 8080

    [Database]
    connection = "Server=db;Port=5432;User=""admin"""
    """);

// Indexer access
var host = doc["Server"]["host"];           // "localhost"

// Safe access (returns null if missing)
var port = doc.GetValue("Server", "port");  // "8080"
var appName = doc.GetGlobalValue("app_name"); // "MyApp"

// Metadata
doc.SectionNames           // ["Server", "Database"]
doc.HasGlobalKeys          // true
doc.ContainsSection("Server") // true
```

### Writing Back to INI

```csharp
var doc = ConfigParser.Parse(input);

// Write to string
var output = ConfigWriter.Write(doc);

// Write to TextWriter (sync or async)
ConfigWriter.Write(textWriter, doc);
await ConfigWriter.WriteAsync(streamWriter, doc);
```

### With Microsoft.Extensions.Configuration

```csharp
using Microsoft.Extensions.Configuration;
using Zini.Configuration;

var configuration = new ConfigurationBuilder()
    .AddConfigFile("appsettings.ini", optional: false, reloadOnChange: true)
    .Build();

var host = configuration["Server:host"];   // "localhost"
var appName = configuration["app_name"];   // "MyApp" (global keys have no prefix)
```

## Format Specification

See [docs/config-format-spec.md](docs/config-format-spec.md) for the full format specification including quoting rules, merging behavior, and error handling.

## Building

```powershell
dotnet build Zini.slnx
dotnet test Zini.slnx
```

## License

[MIT](LICENSE.md)
