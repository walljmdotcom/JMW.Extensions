using System;
using System.Collections.Generic;
using System.Linq;

namespace JMW.Templates.Tags
{
    public class Join : TagHandlerBase
    {
        private JoinKey _joinKey;

        // The row lists compare by reference: a filtered table hands its body a new list.
        private readonly record struct JoinKey(
            string? LeftKeyExp,
            string? LeftKey,
            string? LeftWhere,
            string? RightKeyExp,
            string? RightKey,
            string? RightWhere,
            List<List<string>>? LeftData,
            List<List<string>>? RightData
        );

        private readonly TableData? _leftTableData;
        private readonly TableData? _rightTableData;

        public const string TAG = "join";
        public override string TagName { get; } = TAG;

        public override HashSet<string> ALLOWEDPROPS { get; } = [
            ATTR_LEFT_TABLE,
            ATTR_LEFT_WHERE,
            ATTR_LEFT_KEY_EXP,
            ATTR_LEFT_KEY,
            ATTR_RIGHT_TABLE,
            ATTR_RIGHT_WHERE,
            ATTR_RIGHT_KEY_EXP,
            ATTR_RIGHT_KEY,
            ATTR_DISTINCT,
            ATTR_WHERE,
            ATTR_ORDERBY,
            ATTR_ORDER];

        public const string ATTR_LEFT_TABLE = "left_table";
        public const string ATTR_LEFT_WHERE = "left_where";
        public const string ATTR_LEFT_KEY_EXP = "left_key_exp";
        public const string ATTR_LEFT_KEY = "left_key";

        public const string ATTR_RIGHT_TABLE = "right_table";
        public const string ATTR_RIGHT_WHERE = "right_where";
        public const string ATTR_RIGHT_KEY_EXP = "right_key_exp";
        public const string ATTR_RIGHT_KEY = "right_key";

        public const string ATTR_DISTINCT = "distinct";
        public const string ATTR_WHERE = "where";
        public const string ATTR_ORDERBY = "orderby";
        public const string ATTR_ORDER = "order";

        public TableData? JoinedTableData { get; private set; }

        public string? Name { get; }

        public int CurrentRow { get; set; }

        public Join()
        {
        }

        public Join(TableData leftTableData, TableData rightTableData, Interpreter interp)
        {
            _leftTableData = leftTableData;
            _rightTableData = rightTableData;

            Name = _leftTableData.Name + "_" + _rightTableData.Name;

            interp.Retrievers.Add(Name + JoinColumn.LEFT_PREFIX + leftTableData.Name.ToLowerInvariant(), RetrieveColumnValue);
            interp.Retrievers.Add(Name + JoinColumn.RIGHT_PREFIX + rightTableData.Name.ToLowerInvariant(), RetrieveColumnValue);

            // A joined row is the left row followed by the right row, so the column positions are known now.
            for (var i = 0; i < leftTableData.Columns.Count; i++)
                interp.AddHandler(MakeColumnHandler(JoinColumn.LEFT_PREFIX + leftTableData.Columns[i], i));

            for (var i = 0; i < rightTableData.Columns.Count; i++)
                interp.AddHandler(MakeColumnHandler(JoinColumn.RIGHT_PREFIX + rightTableData.Columns[i], leftTableData.Columns.Count + i));
        }

        public override void Handler(Tag tag, Interpreter interp)
        {
            if (Name is null || _leftTableData is null || _rightTableData is null)
            {
                throw new InvalidOperationException();
            }

            TagHelpers.PrefixTags(tag.Children, Name);

            // The joined rows are reused until the join attributes or either table's row list change.
            var join_key = new JoinKey(
                tag.Properties.TryGetValue(ATTR_LEFT_KEY_EXP, out var leftKeyExp) ? leftKeyExp : null,
                tag.Properties.TryGetValue(ATTR_LEFT_KEY, out var leftKey) ? leftKey : null,
                tag.Properties.TryGetValue(ATTR_LEFT_WHERE, out var leftWhere) ? leftWhere : null,
                tag.Properties.TryGetValue(ATTR_RIGHT_KEY_EXP, out var rightKeyExp) ? rightKeyExp : null,
                tag.Properties.TryGetValue(ATTR_RIGHT_KEY, out var rightKey) ? rightKey : null,
                tag.Properties.TryGetValue(ATTR_RIGHT_WHERE, out var rightWhere) ? rightWhere : null,
                _leftTableData.Data,
                _rightTableData.Data
            );

            if (JoinedTableData is null || _joinKey != join_key)
            {
                var left = new Dictionary<string, List<string>>();
                var ldata = _leftTableData.Data;
                if (tag.Properties.TryGetValue(ATTR_LEFT_WHERE, out var attrLeftWhere))
                    ldata = _leftTableData.Data.Where(r => TagHelpers.EvaluateBooleanExpression(attrLeftWhere, r)).ToList();

                foreach (var lr in ldata)
                {
                    if (tag.Properties.TryGetValue(ATTR_LEFT_KEY_EXP, out var attrLeftKeyExp))
                    {
                        var key = TagHelpers.EvaluateExpression(attrLeftKeyExp, lr);
                        if (left.ContainsKey(key))
                            throw new Exception("Key expression does not produce a unique key.  Please provide a key expression that produces a unique key.  Duplicate key: '" + key + "'");
                        left.Add(key, lr);
                    }
                    else if (tag.Properties.TryGetValue(ATTR_LEFT_KEY, out var attrLeftkey)) // use the value of the key column
                    {
                        var col = attrLeftkey.ToLowerInvariant();
                        left.Add(lr[_leftTableData.ColumnIndexes[col]], lr);
                    }
                    else
                    {
                        throw new Exception("Require attribute is missing. A '" + ATTR_LEFT_KEY_EXP + "' or a '" + ATTR_LEFT_KEY + "' attribute is required.");
                    }
                }

                var joined = new List<List<string>>();

                var rdata = _rightTableData.Data;
                if (tag.Properties.TryGetValue(ATTR_RIGHT_WHERE, out var attrRightWhere))
                    rdata = _rightTableData.Data.Where(r => TagHelpers.EvaluateBooleanExpression(attrRightWhere, r)).ToList();

                foreach (var rr in rdata)
                {
                    var key = string.Empty;

                    if (tag.Properties.TryGetValue(ATTR_RIGHT_KEY_EXP, out var attrRightkeyExp))
                    {
                        key = TagHelpers.EvaluateExpression(attrRightkeyExp, rr);
                    }
                    else if (tag.Properties.TryGetValue(ATTR_RIGHT_KEY, out var attrRightKey)) // use the value of the key column
                        key = rr[_rightTableData.ColumnIndexes[attrRightKey.ToLowerInvariant()]];
                    else
                        throw new Exception("Require attribute is missing. A '" + ATTR_RIGHT_KEY_EXP + "' or a '" + ATTR_RIGHT_KEY + "' attribute is required.");

                    if (left.TryGetValue(key, out List<string>? leftValue))
                    {
                        var cols = new List<string>(leftValue);
                        cols.AddRange(rr);
                        joined.Add(cols);
                    }
                }

                var columns = _leftTableData.Columns.Select(c => JoinColumn.LEFT_PREFIX + c).Concat(_rightTableData.Columns.Select(c => JoinColumn.RIGHT_PREFIX + c)).ToList();
                var i = 0;
                var col_indexes = columns.ToDictionary(c => c.ToLowerInvariant(), v => i++);
                JoinedTableData = new TableData(Name, columns, col_indexes, joined);
                _joinKey = join_key;
            }

            Table.EvalFilteredRows(tag, JoinedTableData, interp, row => CurrentRow = row);
        }

        public string RetrieveColumnValue(string? column)
        {
            if (column is null)
            {
                return string.Empty;
            }
            return JoinedTableData?.Data[CurrentRow][JoinedTableData.ColumnIndexes[column.ToLowerInvariant()]] ?? string.Empty;
        }

        internal string RetrieveColumnValue(int column_index)
        {
            return JoinedTableData?.Data[CurrentRow][column_index] ?? string.Empty;
        }

        private JoinColumn MakeColumnHandler(string column_name, int column_index)
        {
            return new JoinColumn(Name + ":" + column_name.ToLowerInvariant().Trim(), this, column_index);
        }
    }
}