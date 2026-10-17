using System;
using System.Collections.Generic;
using System.Linq;

namespace JMW.Template.Tags
{
    public class Join : TagHandlerBase
    {
        private bool _hasBeenJoined = false;
        private string _joinDetails = string.Empty;

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

            foreach (var column in leftTableData.Columns)
                interp.AddHandler(MakeColumnHandler(JoinColumn.LEFT_PREFIX + column));

            foreach (var column in rightTableData.Columns)
                interp.AddHandler(MakeColumnHandler(JoinColumn.RIGHT_PREFIX + column));
        }

        public override void Handler(Tag tag, Interpreter interp)
        {
            if (Name is null || _leftTableData is null || _rightTableData is null)
            {
                throw new InvalidOperationException();
            }

            TagHelpers.PrefixTags(tag.Children, Name);

            var join_details = (tag.Properties.TryGetValue(ATTR_LEFT_KEY_EXP, out var leftKeyExp) ? leftKeyExp : string.Empty) +
                               (tag.Properties.TryGetValue(ATTR_LEFT_KEY, out var leftKey) ? leftKey : string.Empty) +
                               (tag.Properties.TryGetValue(ATTR_LEFT_WHERE, out var leftWhere) ? leftWhere : string.Empty) +
                               (tag.Properties.TryGetValue(ATTR_RIGHT_KEY, out var rightKey) ? rightKey : string.Empty) +
                               (tag.Properties.TryGetValue(ATTR_RIGHT_KEY_EXP, out var rightKeyExp) ? rightKeyExp : string.Empty) +
                               (tag.Properties.TryGetValue(ATTR_RIGHT_WHERE, out var rightWhere) ? rightWhere : string.Empty);

            if (!_hasBeenJoined || join_details != _joinDetails)
            {
                // if its the same join don't do this more than once.
                _joinDetails = join_details;
                _hasBeenJoined = true;

                // do the join
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
                if (tag.Properties.ContainsKey(ATTR_LEFT_WHERE))
                    rdata = _rightTableData.Data.Where(r => TagHelpers.EvaluateBooleanExpression(tag.Properties[ATTR_RIGHT_WHERE], r)).ToList();

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
                var col_indexes = columns.ToDictionary(c => c, v => i++);
                JoinedTableData = new TableData(Name, columns, col_indexes, joined);
            }

            if (JoinedTableData is null)
            {
                throw new InvalidOperationException();
            }

            // filter the distinct if present
            Table.FilterDistinct(tag, JoinedTableData);

            // filter data by where if present.
            Table.FilterWhere(tag, JoinedTableData);

            // filter data by where if present.
            Table.FilterOrderBy(tag, JoinedTableData);

            // Handles all rows in sheet.
            for (CurrentRow = 0; CurrentRow < JoinedTableData.Data.Count; CurrentRow++)
            {
                // we access the row using the CurrentRow value, but it needs to be called for every row
                interp.Eval(tag.Children);
            }
        }

        public string RetrieveColumnValue(string? column)
        {
            if (column is null)
            {
                return string.Empty;
            }
            return JoinedTableData?.Data[CurrentRow][JoinedTableData.ColumnIndexes[column.ToLowerInvariant()]] ?? string.Empty;
        }

        private JoinColumn MakeColumnHandler(string column_name)
        {
            return new JoinColumn(Name + ":" + column_name.ToLowerInvariant().Trim(), this);
        }
    }
}