using JMW.Extensions;
using JMW.Parsing.Compile;
using JMW.Parsing.Expressions;
using JMW.Parsing.Extractors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace JMW.Parsing.Handlers;

public class Table : Base, IParser, IProperty
{
    public const string NAME = "table";

    public const string A_INCLUDE = "include";
    public const string A_HEADER = "header";
    public const string A_ROW = "row";
    public const string A_PROPS = "props";
    public const string A_VALIDATE = "validate";

    public string Name { get; } = NAME;

    private readonly Include include = new(null, null);
    private readonly IExpression? headerExp = null;
    private readonly IExpression? rowExp = null;
    private readonly bool validate = false;

    public Table(Tag token)
    {
        // optional.
        if (token.Properties.TryGetValue(A_INCLUDE, out var includeTag))
        {
            this.include = new Include(includeTag);
        }

        // optional.
        if (token.Properties.TryGetValue(A_HEADER, out var headerTag))
        {
            this.headerExp = Expressions.Base.ToExpression(headerTag);
        }

        // optional
        if (token.Properties.TryGetValue(A_VALIDATE, out var validateTag))
        {
            var v = validateTag.Value.ToString() ?? string.Empty;
            if (
                   !v.Equals("true", StringComparison.OrdinalIgnoreCase)
                && !v.Equals("false", StringComparison.OrdinalIgnoreCase)
            )
            {
                throw new ParseException("Property must be either 'true' or 'false'.");
            }
            this.validate = bool.Parse(v);
        }

        // find first line that matches.
        if (!token.Properties.TryGetValue(A_ROW, out var rowTag))
        {
            throw new ParseException("Missing property: " + A_ROW);
        }
        this.rowExp = Expressions.Base.ToExpression(rowTag);

        if (!token.Properties.TryGetValue(A_PROPS, out var propTag))
        {
            throw new ParseException("Missing property: " + A_PROPS);
        }

        var props = (Stack<Tag>)propTag.Value;
        foreach (var p in props)
        {
            switch (p.Name)
            {
                case Paragraph.NAME:
                    base.Props.Add(new Paragraph(p));
                    break;

                case Property.NAME:
                    base.Props.Add(new Property(p));
                    break;

                default:
                    throw new ArgumentException("Unsupported " + nameof(IProperty) + "Tag", p.Name);
            }
        }
    }

    public IEnumerable<string> GetNextRow(StreamReader reader, IExpression row)
    {
        var sr = new SectionReader(reader, this.include);

        string? line;
        while ((line = sr.ReadLine()) is not null)
        {
            if (row is not null && row.Test(line))
            {
                yield return line;
            }
        }
    }

    public override IEnumerable<object[]> Parse(StreamReader? reader)
    {
        if (reader is null)
            yield break;

        if (this.rowExp is null)
        {
            throw new InvalidOperationException();
        }

        var pos = reader.BaseStream.Position;

        // get header row, and handle it
        var cols = this.Props.FindAll(p => p is Property property && property.Extractors.Any(ex => ex is Column));
        if (this.headerExp is not null && cols.Count > 0)
        {
            var exp = this.validate ? new Or(new List<IExpression> { this.headerExp, this.rowExp }) : this.headerExp;

            var header = this.GetNextRow(reader, exp);

            var columnPositions = DeriveColumnPositions(header.ToList(), validate_with_data: this.validate).Positions.Values.ToList();
            foreach (var p in cols)
            {
                foreach (var c in ((Property)p).Extractors)
                {
                    ((Column)c).Positions = columnPositions;
                }
            }
        }
        reader.BaseStream.Position = pos;

        // parse the rows
        foreach (var line in this.GetNextRow(reader, this.rowExp))
        {
            var record = new object[this.Props.Count];
            for (var i = 0; i < record.Length; i++)
            {
                using var sr = new StringReader(line);
                record[i] = this.Props[i].Parse(sr);
            }
            yield return record;
        }
    }

    object IProperty.Parse(TextReader reader)
    {
        return this.Parse(AsStreamReader(reader));
    }

    #region Static Methods

    private static readonly string[] separator = [" "];

    /// <summary>
    ///   Assumes the first row of the table is the column row.  Derives the column positions.  Handles whitespace and paging breaks in the table.
    /// </summary>
    /// <param name="table"><see>
    ///         <cref>List{string}</cref>
    ///     </see>
    ///     holding the rows of the table.</param>
    /// <param name="validate_with_data">If true, uses the table data to validate the column positions</param>
    /// <returns>Dictionary object indexed by Column name, that has the starting position and length for each column.</returns>
    private static bool IsColumnBreak(char c) => c is ' ' or '\r' or '\n' or '+';

    // True when the trimmed row holds at least two different characters, counting any whitespace as a space.
    private static bool HasTwoDistinctCharacters(string row)
    {
        int first = 0;
        while (first < row.Length && char.IsWhiteSpace(row[first]))
        {
            first++;
        }

        int last = row.Length - 1;
        while (last > first && char.IsWhiteSpace(row[last]))
        {
            last--;
        }

        for (int i = first + 1; i <= last; i++)
        {
            char c = char.IsWhiteSpace(row[i]) ? ' ' : row[i];
            if (c != row[first])
            {
                return true;
            }
        }

        return false;
    }

    public static ColumnPositionCollection DeriveColumnPositions(IList<string> table, bool validate_with_data = true)
    {
        var original_column_string = "";

        #region Get Columns

        // find the first non empty line.
        var c = 0;
        var columnnames = table[c];
        while (columnnames.Trim().Length == 0)
        {
            columnnames = table[++c];
        }
        original_column_string = columnnames;

        // initialize the field positions dictionary object.
        var fieldPositions = new Dictionary<string, ColumnPosition>();
        var positions = new List<ColumnPosition>();

        // get the field names.  we assume the line has them.
        var pre_columns = columnnames.Trim().Split(separator, StringSplitOptions.RemoveEmptyEntries);

        // handle columnnames
        columnnames = HandleDuplicateColumnNames(columnnames, pre_columns);
        table[c] = columnnames;
        var columns = columnnames.Trim().Split(separator, StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();

        for (var i = 0; i < columns.Length; i++)
        {
            var col = columns[i];

            var o = new ColumnPosition();

            if (i + 1 == columns.Length) // we're on the last one
            {
                o.Start = columnnames.LastIndexOf(" " + col, StringComparison.Ordinal) + 1;
                o.Length = -1;
            }
            else if (i + 2 == columns.Length) // we're on the second to last, but that means the next column is the last.
            {
                o.Start = columnnames.IndexOf(" " + col + " ", StringComparison.Ordinal) + 1;
                o.Length = columnnames.LastIndexOf(" " + columns[i + 1], StringComparison.Ordinal) - o.Start + 1;
            }
            else if (i == 0) // we're on the first, treat the start special.
            {
                o.Start = columnnames.IndexOf(col + " ", StringComparison.Ordinal);
                o.Length = columnnames.IndexOf(" " + columns[i + 1] + " ", StringComparison.Ordinal) - o.Start + 1;
            }
            else
            {
                o.Start = columnnames.IndexOf(" " + col + " ", StringComparison.Ordinal) + 1;
                o.Length = columnnames.IndexOf(" " + columns[i + 1] + " ", StringComparison.Ordinal) - o.Start + 1;
            }
            o.Name = columns[i];
            o.Key = columns[i];
            fieldPositions.Add(col, o);
            positions.Add(o);
        }

        #endregion Get Columns

        #region Validate

        if (validate_with_data)
        {
            // we need to access the keys line a normal array/list and the key collection in the dict object wont let us.
            var keys = positions.Select(o => o.Key).ToList();

            //  we increase the start position at least one to hedge our bets, as we can rely on there being
            //   at least 1 char in the column for the row, and if there isn't, it doesn't matter anyway.
            //   but this does allow for situations where the header is off by one, because that happens.
            for (var i = 0; i < positions.Count; i++) positions[i].Start++;

            // now test to make sure your column boundaries are correct by looping through the rest of the table
            for (var i = c; i < table.Count; i++)
            {
                var row = table[i];

                if (HasTwoDistinctCharacters(row)) // don't bother with empty lines or rules like "-----".
                {
                    //check to see if the first column starts at the beginning of the line.
                    while (fieldPositions[keys[0]].Start != 0 &&
                        row[fieldPositions[keys[0]].Start - 1] != ' ' &&
                        row[fieldPositions[keys[0]].Start - 1] != '+')
                    {
                        fieldPositions[keys[0]].Start--;
                        fieldPositions[keys[0]].Length++;
                    }

                    // Adjust the rows if necessary.
                    for (var j = 0; j < keys.Count - 1; j++) // we ignore the last column, because its starting index will get moved, but its length is -1.
                    {
                        var key = keys[j];

                        var start = fieldPositions[key].Start + fieldPositions[key].Length - 1;

                        while (start >= 0 && row.Length > start && !IsColumnBreak(row[start]))
                        {
                            fieldPositions[key].Length--; // only decrement the length if its not the last column
                            fieldPositions[keys[j + 1]].Start--; // only decrement the starting index of the next column if its not the last column

                            // now deal with the increased col length.
                            if (j + 2 < keys.Count) // its not the second to last
                            {
                                fieldPositions[keys[j + 1]].Length++; // only increment the length of the next column if the next colum isn't the last column
                            }
                            start = fieldPositions[key].Start + fieldPositions[key].Length - 1; // reset the start pos.
                        }
                    }
                }
            }
        }

        fieldPositions = CombineMultiwordColumnNames(positions);

        #endregion Validate

        // name the columns from the original header, so duplicates there become Name, Name1, Name2.
        var named = new Dictionary<string, ColumnPosition>();
        foreach (var position in fieldPositions.Values)
        {
            position.Name = UniqueName(named, position.GetColumnValue(original_column_string).Trim());
            named.Add(position.Name, position);
        }

        return new ColumnPositionCollection(named);
    }

    private static string UniqueName(Dictionary<string, ColumnPosition> taken, string name)
    {
        if (!taken.ContainsKey(name))
            return name;

        for (var n = 1; ; n++)
        {
            var candidate = name + n.ToString(CultureInfo.InvariantCulture);
            if (!taken.ContainsKey(candidate))
                return candidate;
        }
    }

    /// <summary>
    ///   This handles the case where column names had multiple words in them and were
    ///     initially treated as individual columns.  When a column has a length of 0
    ///     then you can combine the name with the column next to it.
    /// </summary>
    /// <param name="positions">A list of the Positions for the columns in order they were found.</param>
    /// <returns></returns>
    private static Dictionary<string, ColumnPosition> CombineMultiwordColumnNames(List<ColumnPosition> positions)
    {
        // look for columns with a length of 1, as that column is basically a zero length column and should be combined with the column next to it.
        var field_positions = new Dictionary<string, ColumnPosition>();
        for (var i = 0; i < positions.Count; i++)
        {
            var p = positions[i]; // get the position
            if (p.Length == 0)
            {
                // the length of the position is 0, so it needs to be added to the column next to it.
                positions[i + 1].Name = positions[i].Name + positions[i + 1].Name; // merge this position with the next position.

                while (positions.Count > i + 1 && positions[i + 1].Length == 0)
                {
                    i++; // go to the next line.
                    positions[i + 1].Name = positions[i].Name + positions[i + 1].Name; // merge this position with the next position.
                }

                field_positions.Add(UniqueName(field_positions, positions[i + 1].Name), positions[i + 1]); // add the next position to the fields.

                if (positions.Count > i + 1) i++;
                else break;
            }
            else
            {
                field_positions.Add(UniqueName(field_positions, p.Name), p);
            }
        }
        return field_positions;
    }

    /// <summary>
    ///   This handles the case where duplicate column names were detected in the column header.
    ///     when this happens, you have to change the column names to be unique, preserving the
    ///     length of the column.
    /// </summary>
    /// <param name="columnnames">a string containing the column header</param>
    /// <param name="columns">the array of unique column names</param>
    /// <returns></returns>
    private static string HandleDuplicateColumnNames(string columnnames, string[] columns)
    {
        // now check for uniqueness.
        var cols = columns.Distinct().ToArray();

        foreach (var col in cols)
        {
            var occurrences = columnnames.CountInstances(col, true);
            if (occurrences > 1)
            {
                var n = 0;
                while (columnnames.Contains(col))
                {
                    var nc = columnnames.ParseToIndexOf(col) + col.Substring(0, col.Length - n.ToString(CultureInfo.InvariantCulture).Length) + n + columnnames.ParseAfterIndexOf_PlusLength(col);
                    columnnames = nc;
                    n++;
                }
            }
        }

        return columnnames;
    }

    #endregion Static Methods
}

public class ColumnPositionCollection
{
    public ColumnPositionCollection(Dictionary<string, ColumnPosition> positions)
    {
        this.Positions = positions;
        var start = -1;
        foreach (var pos in positions)
        {
            if (pos.Value.Start > start) start = pos.Value.Start;
        }
    }

    public Dictionary<string, ColumnPosition> Positions { get; set; }
}

public class ColumnPosition
{
    public string Name { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    [DefaultValue(-1)]
    public int Start { get; set; } = -1;

    public int Length { get; set; } = -1;

    public override string ToString()
    {
        return this.Length switch
        {
            -1 => this.Name + ": " + this.Start,
            0 => this.Name + ": " + this.Start + "-" + this.Start,
            _ => this.Name + ": " + this.Start + "-" + (this.Start + this.Length - 1),
        };
    }

    /// <summary>
    /// Uses a set of Column Positions to get a value from a string.
    /// </summary>
    /// <param name="line"></param>
    public string GetColumnValue(string line)
    {
        if (line.Length >= this.Start + this.Length && this.Length > 0)
        {
            return line.Substring(this.Start, this.Length);
        }

        return line.Length > this.Start ? line.Substring(this.Start) : string.Empty;
    }
}