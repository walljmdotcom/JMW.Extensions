using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace JMW.Templates
{
    public class TableData
    {
        /// <summary>
        /// Construct the data from a list of csv rows
        /// </summary>
        /// <param name="name">The name of tab</param>
        /// <param name="_table"></param>
        public TableData(string name, List<string> _table)
        {
            Name = name;

            var header = _table[0];
            if (header.Length > 0 && header[0] == ByteOrderMark)
                header = header.Substring(1);

            Columns = ReadFields(header, 1);

            for (var row = 1; row < _table.Count; row++)
            {
                Data.Add(ReadFields(_table[row], row + 1));
            }

            var i = 0;
            foreach (var column in Columns)
            {
                ColumnIndexes.Add(column.ToLowerInvariant(), i);
                i++;
            }
        }

        /// <summary>
        /// Construct the data from raw info.
        /// </summary>
        /// <param name="name">Name of the tab</param>
        /// <param name="cols">Column values in order</param>
        /// <param name="col_indexes">The index of each column by column name</param>
        /// <param name="data">The data in nested rows and columns/cells</param>
        public TableData(string name, List<string> cols, Dictionary<string, int> col_indexes, List<List<string>> data)
        {
            Name = name;

            Columns = cols;
            Data = data;
            ColumnIndexes = col_indexes;
        }

        /// <summary>
        /// Construct the data from a DataTable
        /// </summary>
        /// <param name="dt"></param>
        public TableData(DataTable dt)
        {
            Name = dt.TableName;

            foreach (DataColumn c in dt.Columns)
            {
                Columns.Add(c.ColumnName);
            }

            foreach (DataRow r in dt.Rows)
            {
                Data.Add(
                    r.ItemArray
                     .Select(o => o?.ToString())
                     .Where(o => o is not null)
                     .Cast<string>()
                     .ToList()
                );
            }

            var i = 0;
            foreach (var column in Columns)
            {
                ColumnIndexes.Add(column.ToLowerInvariant(), i);
                i++;
            }
        }

        private const char ByteOrderMark = (char)0xFEFF;

        /// <summary>
        /// Splits one CSV line into fields per RFC 4180. A field that opens with a quote may hold commas, and a doubled
        /// quote inside it is one quote. Unquoted fields are kept as written, spaces included.
        /// </summary>
        /// <param name="line_number">The 1-based line, for the error message.</param>
        private static List<string> ReadFields(string line, int line_number)
        {
            if (line.IndexOf('"') < 0)
                return [.. line.Split(',')];

            var fields = new List<string>();
            var field = new StringBuilder();
            var i = 0;
            while (true)
            {
                if (i < line.Length && line[i] == '"')
                {
                    i++;
                    while (true)
                    {
                        if (i >= line.Length)
                            throw new FormatException("Line " + line_number + " has a quoted field with no closing quote.");

                        var c = line[i++];
                        if (c != '"')
                            field.Append(c);
                        else if (i < line.Length && line[i] == '"')
                            field.Append(line[i++]);
                        else
                            break;
                    }
                }

                // Text after a closing quote, or a quote that does not open the field, is kept as written.
                while (i < line.Length && line[i] != ',')
                    field.Append(line[i++]);

                fields.Add(field.ToString());
                field.Clear();

                if (i >= line.Length)
                    return fields;

                i++;
            }
        }

        #region Properties

        /// <summary>
        /// The name of the tab
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A list of column names as strings
        /// </summary>
        public List<string> Columns { get; } = [];

        /// <summary>
        /// The index of the each column name.
        /// </summary>
        public Dictionary<string, int> ColumnIndexes { get; } = [];

        /// <summary>
        /// The data in a nested list.  Rows[Columns[]]
        /// </summary>
        public List<List<string>> Data { get; set; } = [];

        #endregion Properties
    }
}