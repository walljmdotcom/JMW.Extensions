using JMW.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JMW.Templates.Tags
{
    public class Table : TagHandlerBase
    {
        private readonly string _name;
        private readonly HashSet<string> _columnTags;

        public const string TAG = "table";
        public const string TAB_TAG = "tab";
        public const string ANON_TABLE_NAME = "anon_table";

        public override string TagName { get; } = TAG;
        public override HashSet<string> ALLOWEDPROPS { get; } = [ATTR_NAME, ATTR_DISTINCT, ATTR_WHERE, ATTR_ORDERBY, ATTR_ORDER];
        public const string ATTR_DISTINCT = "distinct";
        public const string ATTR_WHERE = "where";
        public const string ATTR_ORDERBY = "orderby";
        public const string ATTR_ORDER = "order";
        public const string ATTR_NAME = "name";

        public const string ATTR_ORDER_VALUE_DESC = "desc";

        public TableData? TableData { get; }
        public int CurrentRow { get; set; }

        private static readonly char[] separator = [','];

        public Table()
        {
            _name = string.Empty;
            _columnTags = [];
        }

        public Table(TableData table_data, Interpreter interp)
        {
            _name = table_data.Name.ToLowerInvariant();
            TableData = table_data;
            _columnTags = TagHelpers.ColumnTagNames(table_data.Columns);

            interp.Retrievers.Add(table_data.Name.ToLowerInvariant(), RetrieveColumnValue);

            foreach (var column in table_data.Columns)
                interp.AddHandler(MakeColumnHandler(table_data, column));
        }

        public override void Handler(Tag tag, Interpreter interp)
        {
            if (TableData is null)
            {
                throw new InvalidOperationException();
            }

            // prefix the names of the columns
            TagHelpers.PrefixTags(tag.Children, _name, _columnTags);

            EvalFilteredRows(tag, TableData, interp, row => CurrentRow = row);
        }

        /// <summary>
        /// Applies the tag's distinct, where, and orderby to <paramref name="table_data"/>, runs the tag's body once per
        /// remaining row, then puts the unfiltered rows back so the next tag over the same data starts from all of them.
        /// </summary>
        internal static void EvalFilteredRows(Tag tag, TableData table_data, Interpreter interp, Action<int> set_current_row)
        {
            var unfiltered = table_data.Data;
            try
            {
                FilterDistinct(tag, table_data);
                FilterWhere(tag, table_data);
                FilterOrderBy(tag, table_data);

                // column tags read the row through the current row number, so it is set before each pass.
                for (var row = 0; row < table_data.Data.Count; row++)
                {
                    set_current_row(row);
                    interp.Eval(tag.Children);
                }
            }
            finally
            {
                table_data.Data = unfiltered;
            }
        }

        public static void FilterOrderBy(Tag tag, TableData table_data)
        {
            if (tag.Properties.TryGetValue(ATTR_ORDERBY, out var attrOrderBy))
            {
                var column = table_data.ColumnIndexes[attrOrderBy.Trim().ToLowerInvariant()];
                if (
                       tag.Properties.TryGetValue(ATTR_ORDER, out var attrOrder)
                    && attrOrder == ATTR_ORDER_VALUE_DESC
                )
                {
                    table_data.Data = [.. table_data.Data.OrderByDescending(r => r[column])];
                }
                else
                    table_data.Data = [.. table_data.Data.OrderBy(r => r[column])];
            }
        }

        public static void FilterWhere(Tag tag, TableData table_data)
        {
            if (tag.Properties.TryGetValue(ATTR_WHERE, out var attrWhere))
            {
                table_data.Data = table_data.Data.Where(r => TagHelpers.EvaluateBooleanExpression(attrWhere, r)).ToList();
            }
        }

        public static void FilterDistinct(Tag tag, TableData table_data)
        {
            if (tag.Properties.TryGetValue(ATTR_DISTINCT, out var attrDistinct))
            {
                var cols = attrDistinct.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                var indexes = new int[cols.Length];
                for (var i = 0; i < cols.Length; i++)
                    indexes[i] = table_data.ColumnIndexes[cols[i].Trim().ToLowerInvariant()];

                var seen = new HashSet<string>(StringComparer.Ordinal);
                var rows = new List<List<string>>();
                var key = new StringBuilder();
                foreach (var row in table_data.Data)
                {
                    key.Clear();
                    for (var i = 0; i < indexes.Length; i++)
                    {
                        if (i > 0)
                            key.Append('|');
                        key.Append(row[indexes[i]]);
                    }

                    if (seen.Add(key.ToString()))
                        rows.Add(row);
                }

                table_data.Data = rows;
            }
        }

        public string RetrieveColumnValue(string column)
        {
            return TableData?.Data[CurrentRow][TableData.ColumnIndexes[column.ToLowerInvariant()]] ?? string.Empty;
        }

        internal string RetrieveColumnValue(int column_index)
        {
            return TableData?.Data[CurrentRow][column_index] ?? string.Empty;
        }

        private TableColumn MakeColumnHandler(TableData table_data, string column_name)
        {
            var name = column_name.ToLowerInvariant().Trim();
            return table_data.ColumnIndexes.TryGetValue(name, out var index)
                ? new TableColumn(_name + ":" + name, this, index)
                : new TableColumn(_name + ":" + name, this);
        }

        public static bool IsTable(Tag tag)
        {
            return tag.TagType == TagTypes.Tag && (tag.Name == TAG || tag.Name == TAB_TAG);
        }
    }
}