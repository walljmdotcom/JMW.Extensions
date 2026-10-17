using JMW.Extensions;
using JMW.Templates.Expressions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JMW.Templates.Tags;

public static class TagHelpers
{
    /// <summary>
    /// Prefixes the tags in <paramref name="tags"/> named in <paramref name="column_tags"/>, and the column lists of
    /// if and transform tags, with <paramref name="sheet_name"/>. Every other tag keeps its own handler.
    /// </summary>
    /// <param name="column_tags">The sheet's tag names, lowercase, as the parser writes tag names.</param>
    public static void PrefixTags(IEnumerable<Tag> tags, string sheet_name, ISet<string> column_tags)
    {
        foreach (var tag in tags)
        {
            if (Conditional.IsConditional(tag) || Transform.IsTransform(tag))
            {
                PrefixIfColumns(tag, sheet_name);
            }
            else if (tag.TagType == TagTypes.Tag && column_tags.Contains(tag.Name) && !Output.IsOutput(tag) && !Table.IsTable(tag) && !Variable.IsVariable(tag))
            {
                PrefixTag(tag, sheet_name);
            }

            // handle children
            if (Output.IsOutput(tag) || Conditional.IsConditional(tag))
            {
                PrefixTags(tag.Children, sheet_name, column_tags);
            }
        }
    }

    /// <summary>
    /// The tag names for <paramref name="columns"/>, lowercase and trimmed to match parsed tag names.
    /// </summary>
    internal static HashSet<string> ColumnTagNames(IEnumerable<string> columns)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var column in columns)
            names.Add(column.ToLowerInvariant().Trim());

        return names;
    }

    public static void PrefixTag(Tag tag, string name)
    {
        if (!Output.IsOutput(tag) && !Table.IsTable(tag) && !tag.Name.Contains(':'))
        {
            tag.Name = name + ":" + tag.Name;
        }
    }

    public static void PrefixIfColumns(Tag tag, string name)
    {
        var attr = Transform.IsTransform(tag) ? Transform.ATTR_COLUMN : Conditional.ATTR_COLUMN;

        // The column list is optional, for an expression that reads no columns.
        if (!tag.Properties.TryGetValue(attr, out var columnList))
            return;

        List<string> columns = [.. columnList.Split(',')];

        if (columns.Count > 1)
        {
            tag.Properties[attr] = string.Empty;

            for (var i = 0; i < columns.Count; i++)
            {
                var column = columns[i].Trim();
                if (!string.IsNullOrWhiteSpace(column))
                {
                    if (!column.Contains(':'))
                    {
                        column = name + ":" + column;
                    }

                    tag.Properties[attr] += column + ", "; ;
                }
            }

            if (tag.Properties[attr].Contains(", "))
                tag.Properties[attr] = tag.Properties[attr].ParseToLastIndexOf(", ");
        }

        // If name of column is not prefixed, conditional belongs in current sheet.
        else if (!tag.Properties[attr].Contains(':'))
        {
            tag.Properties[attr] = name + ":" + tag.Properties[attr];
        }
    }

    public static void CheckAllowedAttributes(Tag curr, HashSet<string> allowed, Token token)
    {
        foreach (var p in curr.Properties)
        {
            if (!allowed.Contains(p.Key))
                throw new ParseException("Invalid attribute found on tag. Allowed attributes: " + allowed.ToDelimitedString(", "), token);
        }
    }

    public static string RetrieveOctet(string data, string octet_string)
    {
        if (!int.TryParse(octet_string, out var octet) || octet > 4 || octet < 1)
        {
            throw new Exception("The value provided for octet argument was invalid. Must be a number between 1 and 4.");
        }
        string val;
        if (data.Contains('.'))
        {
            var split = data.Split(dotSeparator, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 4)
            {
                throw new Exception("There was a problem parsing octets from the IP: '" + data + "'. Please check formatting.");
            }
            val = split[octet - 1];
        }
        else
        {
            throw new Exception("The provided column value: '" + data + "' is not an IP.");
        }

        return val;
    }

    private static readonly string[] dotSeparator = ["."];

    public static string ReplaceOctet(string data, string octet, double octet_result)
    {
        if (octet_result > 255) octet_result = 255;
        else if (octet_result < 0) octet_result = 0;

        var split = data.Split(dotSeparator, StringSplitOptions.RemoveEmptyEntries);

        split[int.Parse(octet, CultureInfo.InvariantCulture) - 1] = octet_result.ToString(CultureInfo.InvariantCulture);

        data = string.Empty;
        for (var i = 0; i < split.Length; i++)
        {
            data += split[i];
            if (i != split.Length - 1) data += ".";
        }
        return data;
    }

    public static bool EvaluateBooleanExpression(string expression, List<string> arguments)
    {
        var result = ScriptEvaluator.Evaluate(expression, arguments).ToString();

        if (bool.TryParse(result, out var value))
            return value;

        throw new Exception("The expression:\n" + expression + "\ndid not evaluate to a boolean value.");
    }

    public static double EvaluateArithmeticExpression(string expression, List<string> arguments)
    {
        var result = ScriptEvaluator.Evaluate(expression, arguments).ToString();

        if (double.TryParse(result, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value))
            return value;

        throw new Exception("The expression:\n" + expression + "\ndid not evaluate to a numeric value.");
    }

    public static string EvaluateExpression(string expression, List<string> arguments)
    {
        return ScriptEvaluator.Evaluate(expression, arguments).ToString();
    }
}