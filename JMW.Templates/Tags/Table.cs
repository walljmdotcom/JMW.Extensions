using JMW.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JMW.Templates.Tags
{
    public class Table : TagHandlerBase
    {
        private readonly string _name;

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
        }

        public Table(TableData table_data, Interpreter interp)
        {
            _name = table_data.Name.ToLowerInvariant();
            TableData = table_data;

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
            TagHelpers.PrefixTags(tag.Children, _name);

            var old = TableData.Data;

            // filter the distinct if present
            FilterDistinct(tag, TableData);

            // filter data by where if present.
            FilterWhere(tag, TableData);

            // filter data by where if present.
            FilterOrderBy(tag, TableData);

            // evaluate the children of the table tag.
            for (CurrentRow = 0; CurrentRow < TableData.Data.Count; CurrentRow++)
            {
                // we access the row using the CurrentRow value, but it need to be called for every row
                interp.Eval(tag.Children);
            }

            // just in case you modified TableData.Data in the distinct or where clause, put it back.
            TableData.Data = old;
        }

        public static void FilterOrderBy(Tag tag, TableData table_data)
        {
            if (tag.Properties.TryGetValue(ATTR_ORDERBY, out var attrOrderBy))
            {
                if (
                       tag.Properties.TryGetValue(ATTR_ORDER, out var attrOrder) 
                    && attrOrder == ATTR_ORDER_VALUE_DESC
                )
                {
                    table_data.Data = [.. table_data.Data.OrderByDescending(r => r[table_data.ColumnIndexes[attrOrderBy]])];
                }
                else
                    table_data.Data = [.. table_data.Data.OrderBy(r => r[table_data.ColumnIndexes[attrOrderBy]])];
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

                var dict = new Dictionary<string, List<string>>();
                foreach (var row in table_data.Data)
                {
                    var key = cols.Select(f => row[table_data.ColumnIndexes[f.Trim().ToLowerInvariant()]]).ToDelimitedString("|");
                    dict.AddIfNotPresent(key, row);
                }

                table_data.Data = [.. dict.Values];
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