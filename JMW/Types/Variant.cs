using System;
using System.Collections.Generic;
using System.Linq;

namespace JMW.Types;

public class Variant : IEquatable<Variant>
{
    #region Properties

    private string? _stringValue;

    public string? StringValue
    {
        get { return _stringValue; }
        set { Nullify(); _stringValue = value; }
    }

    private List<Variant>? _listValue;

    public List<Variant>? ListValue
    {
        get { return _listValue; }
        set { Nullify(); _listValue = value; }
    }

    private Dictionary<string, Variant>? _dictValue;

    public Dictionary<string, Variant>? DictValue
    {
        get { return _dictValue; }
        set { Nullify(); _dictValue = value; }
    }

    #endregion Properties

    #region Constructors

    public Variant()
    {
    }

    public Variant(string value)
    {
        _stringValue = value;
    }

    public Variant(List<Variant> value)
    {
        _listValue = value;
    }

    public Variant(Dictionary<string, Variant> value)
    {
        _dictValue = value;
    }

    #endregion Constructors

    #region Is Types

    public bool IsString
    {
        get { return _stringValue is not null; }
    }

    public bool IsList
    {
        get { return _listValue is not null; }
    }

    public bool IsDict
    {
        get { return _dictValue is not null; }
    }

    #endregion Is Types

    #region Explicit Conversions Variant to string/udo/lst/dict

    public static explicit operator string(Variant d)
    {
        return d.ToString();
    }

    public static explicit operator List<Variant>?(Variant d)
    {
        return d.ListValue;
    }

    public static explicit operator List<string>?(Variant d)
    {
        return d.ListValue?
            .Select(v => v.StringValue)
            .Where(v => v is not null)
            .Cast<string>()
            .ToList() ?? null;
    }

    public static explicit operator Dictionary<string, Variant>?(Variant d)
    {
        return d.DictValue;
    }

    #endregion Explicit Conversions Variant to string/udo/lst/dict

    #region Implicit Conversions string/udo/lst/dict to Variant

    public static implicit operator Variant(string d)
    {
        return new Variant(d);
    }

    public static implicit operator Variant(List<Variant> d)
    {
        return new Variant(d);
    }

    public static implicit operator Variant(List<string> d)
    {
        return new Variant(d.Select(v => new Variant(v)).ToList());
    }

    public static implicit operator Variant(Dictionary<string, Variant> d)
    {
        return new Variant(d);
    }

    #endregion Implicit Conversions string/udo/lst/dict to Variant

    private void Nullify()
    {
        _stringValue = null;
        _listValue = null;
        _dictValue = null;
    }

    public override string ToString()
    {
        if (IsString)
        {
            return _stringValue ?? string.Empty;
        }

        if (IsList)
        {
            return "(List) Count: " + (_listValue?.Count ?? -1);
        }

        return IsDict
            ? "(Dict) Count: " + (_dictValue?.Count ?? -1)
            : string.Empty;
    }

    public bool Equals(Variant? val)
    {
        if (val is null)
            return false;

        if (ReferenceEquals(this, val))
            return true;

        if (_stringValue is not null || val._stringValue is not null)
            return string.Equals(_stringValue, val._stringValue, StringComparison.Ordinal);

        if (_listValue is not null || val._listValue is not null)
        {
            if (_listValue is null || val._listValue is null || _listValue.Count != val._listValue.Count)
                return false;

            for (var i = 0; i < _listValue.Count; i++)
            {
                if (!_listValue[i].Equals(val._listValue[i])) return false;
            }

            return true;
        }

        if (_dictValue is not null || val._dictValue is not null)
        {
            if (_dictValue is null || val._dictValue is null || _dictValue.Count != val._dictValue.Count)
                return false;

            foreach (var kv in _dictValue)
            {
                if (!val._dictValue.TryGetValue(kv.Key, out var other) || !kv.Value.Equals(other)) return false;
            }

            return true;
        }

        // two empty variants are equal
        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Variant);
    }

    /// <summary>
    /// Lists and dictionaries hash by kind and count only, so variants that compare equal hash equal regardless of
    /// element hashing. The hash changes if the variant's value is mutated.
    /// </summary>
    public override int GetHashCode()
    {
        int hash;
        if (_stringValue is not null)
            hash = StringComparer.Ordinal.GetHashCode(_stringValue);
        else if (_listValue is not null)
            hash = unchecked((int)0x9E3779B1 * (_listValue.Count + 1));
        else if (_dictValue is not null)
            hash = unchecked((int)0x85EBCA77 * (_dictValue.Count + 1));
        else
            hash = 0;

        // kept positive and non-zero, which callers of the original reference-based hash could rely on
        return (hash & int.MaxValue) | 1;
    }
}