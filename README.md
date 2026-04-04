# sINI

A high-performance, zero-allocation INI-style configuration file parser for .NET 10 with `Microsoft.Extensions.Configuration` integration.

## Features

- **Zero-allocation hot path** — uses `SearchValues<char>` for SIMD-accelerated parsing
- **Immutable output** — returns `FrozenDictionary` instances
- **Case-insensitive** sections and keys with `OrdinalIgnoreCase` comparison
- **Section merging** — duplicate sections merge automatically (last-write-wins for keys)
- **Quoted values** — preserves whitespace and comment characters inside `"..."`
- **Inline comments** — `#` and `;` stripped from unquoted values
- **Global keys** — keys before any section header are supported
- **Configuration integration** — drop-in `IConfigurationBuilder.AddConfigFile()` extension

## Installation

```shell
dotnet add package sINI
dotnet add package sINI.Configuration  # optional, for IConfigurationBuilder support
```

## Quick Start

### Direct Parsing

```csharp
using sINI;

var config = ConfigParser.Parse("""
    # Application settings
    app_name = MyApp

    [Server]
    host = localhost
    port = 8080

    [Database]
    connection = "Server=db;Port=5432;User=""admin"""
    """);

// Access values
var host = config["Server"]["host"];       // "localhost"
var appName = config[""]["app_name"];       // "MyApp" (global section)
```

### With Microsoft.Extensions.Configuration

```csharp
using Microsoft.Extensions.Configuration;
using sINI.Configuration;

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
dotnet build sINI.slnx
dotnet test sINI.slnx
```

## License

[MIT](LICENSE.md)
