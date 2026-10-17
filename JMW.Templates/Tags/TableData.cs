using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace JMW.Template
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

            Columns = [.. _table[0].Split(',')];
            _table = _table.GetRange(1, _table.Count - 1);

            foreach (var line in _table)
            {
                Data.Add([.. line.Split(',')]);
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