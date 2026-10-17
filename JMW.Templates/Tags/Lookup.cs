using System;
using System.Collections.Generic;

namespace JMW.Templates.Tags
{
    public class Lookup : TagHandlerBase
    {
        private List<List<string>>? indexedData;
        private string? indexedKeyExp;
        private string? indexedKeyCol;

        public const string TAG = "lookup";
        public override string TagName { get; } = TAG;
        public override HashSet<string> ALLOWEDPROPS { get; } = [ATTR_KEY_EXP, ATTR_KEY_COL, ATTR_TABLE];
        public const string ATTR_KEY_EXP = "key_exp";
        public const string ATTR_KEY_COL = "key_col";
        public const string ATTR_TABLE = "table";

        public TableData? TableData { get; }
        public Dictionary<string, List<List<string>>> LookupTable { get; private set; } = [];
        public readonly string? Name;

        public int CurrentRow { get; set; }
        public string? Key { get; set; }

        public Lookup()
        {
        }

        public Lookup(TableData table_data, Interpreter interp)
        {
            Name = "lookup_" + table_data.Name.ToLowerInvariant();
            TableData = table_data;

            interp.Retrievers.Add(Name, RetrieveColumnValue);

            foreach (var column in table_data.Columns)
                interp.AddHandler(MakeColumnHandler(column));

            interp.AddHandler(new LookupRow(Name + ":" + LookupRow.TAG, this));
        }

        public override void Handler(Tag tag, Interpreter interp)
        {
            if (Name is null || TableData is null)
            {
                throw new InvalidOperationException();
            }

            TagHelpers.PrefixTags(tag.Children, Name);

            tag.Properties.TryGetValue(ATTR_KEY_EXP, out var attrKeyExp);
            tag.Properties.TryGetValue(ATTR_KEY_COL, out var attrKeyCol);

            if (attrKeyExp is null && attrKeyCol is null)
                throw new Exception("Require attribute is missing. A '" + ATTR_KEY_EXP + "' or '" + ATTR_KEY_COL + "' attribute is required.");

            // The index is reused until the key attributes or the table's row list change.
            if (!ReferenceEquals(indexedData, TableData.Data) || attrKeyExp != indexedKeyExp || attrKeyCol != indexedKeyCol)
            {
                LookupTable.Clear();
                var keyColIndex = attrKeyExp is null && attrKeyCol is not null
                    ? TableData.ColumnIndexes[attrKeyCol.ToLowerInvariant()]
                    : -1;

                foreach (var lr in TableData.Data)
                {
                    var key = attrKeyExp is not null ? TagHelpers.EvaluateExpression(attrKeyExp, lr) : lr[keyColIndex];

                    if (LookupTable.TryGetValue(key, out List<List<string>>? lookupValue))
                        lookupValue.Add(lr);
                    else
                        LookupTable.Add(key, [lr]);
                }

                indexedData = TableData.Data;
                indexedKeyExp = attrKeyExp;
                indexedKeyCol = attrKeyCol;
            }
            interp.HandleChildren(tag);
        }

        public string RetrieveColumnValue(string column)
        {
            if (Key is null || TableData is null)
            {
                throw new InvalidOperationException();
            }

            return LookupTable[Key][CurrentRow][TableData.ColumnIndexes[column.ToLowerInvariant()]];
        }

        private LookupColumn MakeColumnHandler(string column_name)
        {
            return new LookupColumn(Name + ":" + column_name.ToLowerInvariant().Trim(), this);
        }
    }
}