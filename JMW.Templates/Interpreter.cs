using JMW.Extensions.String;
using JMW.Functional;
using JMW.Template.Tags;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Version = JMW.Template.Tags.Version;

namespace JMW.Template
{
    public delegate void Handler(Tag token, Interpreter interpreter);

    public delegate string ValueRetriever(string column);

    public class Interpreter
    {
        #region Handlers

        private readonly Variable _varHandler;
        private readonly Text _textHandler;

        #endregion Handlers

        #region Properties

        public readonly Dictionary<string, ValueRetriever> Retrievers = [];
        public Dictionary<string, ITagHandler> Handlers { get; } = [];
        public Optional<Handler> DefaultHandler { get; set; } = Optional<Handler>.None();
        public TextWriter OutputStream { get; set; }
        public Parser Parser { get; }

        /// <summary>
        /// The directory that <c>&lt;include path&gt;</c> and <c>&lt;output filename&gt;</c> paths must resolve inside.
        /// Relative paths are taken from here. Null means the current directory at the time the tag runs.
        /// </summary>
        public string? FileRoot { get; set; }

        /// <summary>
        /// When false, include and output paths may point anywhere, including outside <see cref="FileRoot"/>.
        /// Leave this true when templates come from anyone you would not let read and write your files.
        /// </summary>
        public bool RestrictFilePaths { get; set; } = true;

        #endregion Properties

        public Interpreter(TextWriter output_stream) : this(new Parser(), output_stream, new List<ITagHandler>())
        {
        }

        public Interpreter(Parser parser, TextWriter output_stream, IEnumerable<ITagHandler> handlers)
        {
            Parser = parser;
            OutputStream = output_stream;

            _varHandler = new Variable();
            _textHandler = new Text();

            AddHandler(new Conditional(Retrievers));
            AddHandler(new Transform(Retrievers));
            AddHandler(_varHandler);
            AddHandler(new Timestamp());
            AddHandler(new Version());
            AddHandler(new Output());

            foreach (var h in handlers)
                AddHandler(h);
        }

        #region Public Functions

        /// <summary>
        /// Parse the template in the input, then interpret the tags.
        /// </summary>
        /// <param name="input">A template string</param>
        public void Eval(string input)
        {
            Eval(Parser.Parse(input));
        }

        /// <summary>
        /// Interpret the tags with a given list of handlers.
        /// </summary>
        /// <param name="tags">The template tags to interpret</param>
        public void Eval(List<Tag> tags)
        {
            // find the named includes.
            var includes = tags.Where(o => o.TagType == TagTypes.Tag && o.Name == Include.TAG && o.Properties.ContainsKey(Include.ATTR_DEFINE))
                .ToDictionary(k => k.Properties[Include.ATTR_DEFINE], v => v);

            // includes should be handled first.
            Include.ReplaceIncludes(tags, includes, this);

            // now populated the variables declared in the template using the <var name="varname" value="varvalue" /> syntax.
            _varHandler.PopulateVariables(tags, this);

            foreach (var tag in tags)
            {
                //  handle variable replacements in tag properties.
                _varHandler.ReplaceVariables(tag);

                switch (tag.TagType)
                {
                    case TagTypes.Tag:
                        var tag_name = tag.Name;

                        // tables are registered by their name attribute.  so if its a table tag,
                        //  then you need to get the name attribute.
                        if (tag.Name == Table.TAG || tag.Name == Table.TAB_TAG)
                        {
                            if (tag.Properties.TryGetValue(Table.ATTR_NAME, out var attrName))
                                tag_name = attrName.ToLowerInvariant();
                            else
                                tag_name = Table.ANON_TABLE_NAME;
                        }

                        if (tag.Name == Transform.TAGALT1 || tag.Name == Transform.TAGALT2)
                        {
                            tag_name = Transform.TAG;
                        }

                        // Join tag handlers are built when they are found using table data
                        // from tables that have already been added. No need to make the user
                        // prep the tag before hand.  Besides, you won't know what the data
                        // source is until you've seen the tag in the template.
                        if (tag.Name == Join.TAG)
                        {
                            // get the tables.
                            var left_tag = tag.Properties[Join.ATTR_LEFT_TABLE].ToLowerInvariant();
                            var right_tag = tag.Properties[Join.ATTR_RIGHT_TABLE].ToLowerInvariant();

                            // this is how we build Join names.
                            tag_name = left_tag + "_" + right_tag;

                            // if we haven't built this join before, build it now.
                            if (!Handlers.ContainsKey(tag_name))
                            {
                                // we need to create the handler here using the data from the tables specified.
                                if (!Handlers.TryGetValue(left_tag, out var leftHandler) || leftHandler is not Table left || left.TableData is null)
                                    throw new Exception("Specified table: '" + left_tag + "' in '" + Join.ATTR_LEFT_TABLE + "' attribute does not exist.");
                                if (!Handlers.TryGetValue(right_tag, out var rightHandler) || rightHandler is not Table right || right.TableData is null)
                                    throw new Exception("Specified table: '" + right_tag + "' in '" + Join.ATTR_RIGHT_TABLE + "' attribute does not exist.");

                                AddHandler(new Join(left.TableData, right.TableData, this));
                            }
                        }

                        // Lookup tag handlers are also built when they are found using table data
                        // from tables that have already been added. No need to make the user
                        // prep the tag before hand.  Besides, you won't know what the data
                        // source is until you've seen the tag in the template.
                        if (tag.Name == Lookup.TAG)
                        {
                            // get the tables.
                            var table_tag = tag.Properties[Lookup.ATTR_TABLE].ToLowerInvariant();

                            // this is how we build Join names.
                            tag_name = "lookup_" + table_tag;

                            // if we haven't built this join before, build it now.
                            if (!Handlers.ContainsKey(tag_name))
                            {
                                // we need to create the handler here using the data from the tables specified.
                                if (!Handlers.TryGetValue(table_tag, out var tableHandler) || tableHandler is not Table table || table.TableData is null)
                                    throw new Exception("Specified table: '" + table_tag + "' in '" + Lookup.ATTR_TABLE + "' attribute does not exist.");

                                AddHandler(new Lookup(table.TableData, this));
                            }
                        }

                        if (Handlers.TryGetValue(tag_name, out var tagHandler))
                            tagHandler.Handler(tag, this);
                        else if (tag_name.Contains(':') && Handlers.ContainsKey(tag_name.ParseToIndexOf(":"))) // handle variables
                        {
                            Handlers[tag_name.ParseToIndexOf(":")].Handler(tag, this);
                        }
                        else
                            DefaultHandler.Do(h => h(tag, this));
                        break;

                    case TagTypes.Text:
                        _textHandler.Handler(tag, this);
                        break;
                }
            }
        }

        public void HandleChildren(Tag token)
        {
            if (token.HasChildren)
                Eval(token.Children);
        }

        public void AddHandler(ITagHandler handler)
        {
            var name = handler.TagName;
            var is_table = handler.TagName == Table.TAG || handler.TagName == Table.TAB_TAG;

            // tables get registered as the table name.
            if (is_table)
            {
                var h = (Table)handler;
                if (h.TableData is null)
                    throw new ArgumentException("A table handler needs table data.", nameof(handler));

                if (h.TableData.Name.IsEmpty())
                    name = Table.ANON_TABLE_NAME;
                else
                    name = h.TableData.Name;
            }

            // join tables get registered as the table name.
            else if (handler.TagName == Join.TAG)
                name = ((Join)handler).Name ?? throw new ArgumentException("A join handler needs a name.", nameof(handler));

            // lookup tables get registered as the table name.
            else if (handler.TagName == Lookup.TAG)
                name = ((Lookup)handler).Name ?? throw new ArgumentException("A lookup handler needs a name.", nameof(handler));

            // tables, joins, and lookups hold data, so a second one with the same name is an error rather than
            // something to skip: skipping would silently drop its rows.
            if (Handlers.ContainsKey(name))
            {
                if (name != handler.TagName)
                    throw new ArgumentException("A table, join, or lookup named '" + name + "' is already defined.", nameof(handler));

                return;
            }

            Handlers.Add(name, handler);

            // add the column tags to the parser
            if (is_table && handler is Table { TableData: { } tableData } table)
            {
                // add the table_name tag.
                Parser.AddTag(tableData.Name, table);

                // add the column_names.
                foreach (var c in tableData.Columns)
                    Parser.AddColumnTag(c);
            }
        }

        public void AddVariable(string name, string value)
        {
            _varHandler.AddVariable(name.ToLowerInvariant(), value);
        }

        #endregion Public Functions

        /// <summary>
        /// Resolves a path named in a template against <see cref="FileRoot"/>, and rejects it when
        /// <see cref="RestrictFilePaths"/> is set and the path leaves the root.
        /// </summary>
        internal string ResolveTemplatePath(string path, string tagName)
        {
            var root = Path.GetFullPath(FileRoot ?? Directory.GetCurrentDirectory());
            var full = Path.GetFullPath(Path.Combine(root, path));

            if (!RestrictFilePaths)
                return full;

            var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? root
                : root + Path.DirectorySeparatorChar;

            // Ordinal, because case-insensitive matching would let /root/Dir accept /root/dir on a case-sensitive file
            // system. On a case-insensitive one the cost is rejecting a path spelled with different casing.
            if (!full.StartsWith(rootWithSeparator, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException(
                    "The <" + tagName + "> tag path '" + path + "' is outside the template directory '" + root + "'."
                );
            }

            return full;
        }
    }
}