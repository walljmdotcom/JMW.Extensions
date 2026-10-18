# JMW.Extensions

Helper libraries for C#. Every package has no dependencies outside this repository. All but `JMW.TextExtraction` target .NET Standard 2.0, so they run on .NET Framework 4.6.1+ and every modern .NET; `JMW.TextExtraction` targets .NET 10.

| Package | What it holds |
|---|---|
| `JMW.Extensions` | Extensions for strings, enumerables, numbers, conversions, and reflection; `Maybe<T>`; `IndexedCollection<T>` for lookups by more than one property; `RangeCollection`, `RingBuffer`, `Pair`, and `Variant`. |
| `JMW.Parsing` | A small instruction language for pulling properties, paragraphs, and tables out of semi-structured text. See [JMW.Parsing/spec.md](JMW.Parsing/spec.md). |
| `JMW.Networking` | Parsers for Junos configuration, `netstat`, and `ifconfig` output, plus a firewall rules engine. |
| `JMW.Templates` | A tag-based text templating engine with tables, joins, lookups, and a JavaScript-subset expression language. See [JMW.Templates/documentation.md](JMW.Templates/documentation.md). |
| `JMW.TextExtraction` | Infers the layout of command output (tables, repeated records, one-off documents) from samples, writes it as an editable spec, and reads text with it. Targets `net10.0`. See [JMW.TextExtraction/README.md](JMW.TextExtraction/README.md). |

## Building

The .NET 10 SDK is required (see `global.json`). The libraries build for `netstandard2.0`, except `JMW.TextExtraction`, which builds for `net10.0`, and the tests run on `net10.0`.

```
dotnet build
dotnet test
```

APIs that `netstandard2.0` lacks are supplied by internal polyfills in `Polyfills/`, compiled into each library.

## Releasing

Push a tag named `v<version>`, for example `v2.0.0`. The NuGet workflow builds, tests, and publishes every packable project at that version. Only `JMW.Extensions` is packable for now; the other packages set `IsPackable` to `false` and are held back.

## Contributing

All contributions should have 100% code coverage.
