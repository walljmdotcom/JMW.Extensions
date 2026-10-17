using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace JMW.Networking.Parsers;

/// <summary>
/// Minimal indented JSON writer, so the library needs no serializer package.
/// </summary>
internal sealed class JsonWriter(TextWriter writer)
{
    private const int indentSize = 2;

    private readonly Stack<Scope> scopes = new();

    public void WriteStartObject() => StartScope(null, '{', ScopeKind.Object);

    public void WriteStartObject(string name) => StartScope(name, '{', ScopeKind.Object);

    public void WriteStartArray() => StartScope(null, '[', ScopeKind.Array);

    public void WriteStartArray(string name) => StartScope(name, '[', ScopeKind.Array);

    public void WriteEndObject() => EndScope(ScopeKind.Object, '}');

    public void WriteEndArray() => EndScope(ScopeKind.Array, ']');

    public void WriteString(string name, string value)
    {
        WritePrefix(name);
        WriteQuoted(value);
    }

    public void WriteStringValue(string value)
    {
        WritePrefix(null);
        WriteQuoted(value);
    }

    private void StartScope(string? name, char open, ScopeKind kind)
    {
        WritePrefix(name);
        writer.Write(open);
        scopes.Push(new Scope(kind));
    }

    private void EndScope(ScopeKind kind, char close)
    {
        if (scopes.Count == 0 || scopes.Peek().Kind != kind)
        {
            throw new InvalidOperationException($"Cannot close a JSON {kind} here.");
        }

        var scope = scopes.Pop();
        if (scope.HasItems)
        {
            writer.WriteLine();
            WriteIndent();
        }

        writer.Write(close);
    }

    private void WritePrefix(string? name)
    {
        if (scopes.Count > 0)
        {
            var scope = scopes.Peek();
            if (name is null && scope.Kind == ScopeKind.Object)
            {
                throw new InvalidOperationException("A value inside a JSON object needs a property name.");
            }

            if (name is not null && scope.Kind == ScopeKind.Array)
            {
                throw new InvalidOperationException("A value inside a JSON array cannot have a property name.");
            }

            if (scope.HasItems)
            {
                writer.Write(',');
            }

            scope.HasItems = true;
            writer.WriteLine();
            WriteIndent();
        }

        if (name is not null)
        {
            WriteQuoted(name);
            writer.Write(": ");
        }
    }

    private void WriteIndent() => writer.Write(new string(' ', scopes.Count * indentSize));

    private void WriteQuoted(string value)
    {
        writer.Write('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': writer.Write("\\\""); break;
                case '\\': writer.Write("\\\\"); break;
                case '\n': writer.Write("\\n"); break;
                case '\r': writer.Write("\\r"); break;
                case '\t': writer.Write("\\t"); break;
                case '\b': writer.Write("\\b"); break;
                case '\f': writer.Write("\\f"); break;
                case < ' ':
                    writer.Write("\\u");
                    writer.Write(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    break;
                default: writer.Write(c); break;
            }
        }

        writer.Write('"');
    }

    private enum ScopeKind
    {
        Object,
        Array,
    }

    private sealed class Scope(ScopeKind kind)
    {
        public ScopeKind Kind { get; } = kind;

        public bool HasItems { get; set; }
    }
}
