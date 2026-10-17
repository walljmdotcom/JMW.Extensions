using System;
using System.Collections.Generic;
using System.IO;

namespace JMW.Template.Tags
{
    public class Output : TagHandlerBase
    {
        private Dictionary<string, int> _iteration = [];

        public const string TAG = "output";
        public override string TagName { get; } = TAG;

        #region Properties

        public override HashSet<string> ALLOWEDPROPS { get; } = [ATTR_FILENAME, ATTR_MODE];

        public const string ATTR_FILENAME = "filename";

        public const string ATTR_MODE = "mode";
        public const string VALUE_MODE_NEW = "new";
        public const string VALUE_MODE_APPEND = "append";

        #endregion Properties

        public override void Handler(Tag tag, Interpreter interp)
        {
            if (!tag.Properties.ContainsKey(ATTR_FILENAME))
                throw new Exception("The '" + ATTR_FILENAME + "' attribute is required.");

            var stream = interp.OutputStream;

            // create new stream and handle properties.
            var mode = tag.Properties.ContainsKey(ATTR_MODE) ? tag.Properties[ATTR_MODE] : VALUE_MODE_APPEND;

            StreamWriter writer;
            switch (mode)
            {
                case VALUE_MODE_NEW:
                    {
                        var file = tag.Properties[ATTR_FILENAME];
                        var directory = Path.GetDirectoryName(file) ?? string.Empty;
                        var ext = Path.GetExtension(file);
                        var file_name = Path.GetFileNameWithoutExtension(file);

                        if (!_iteration.ContainsKey(file))
                            _iteration.Add(file, 0);

                        var new_file_name = Path.Combine(directory, file_name + _iteration[file] + ext);
                        var path = interp.ResolveTemplatePath(new_file_name, TAG);

                        // Create, not OpenOrCreate: a shorter output must not leave the old file's tail behind.
                        writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write));

                        _iteration[file]++;
                        break;
                    }
                case VALUE_MODE_APPEND:
                    {
                        var path = interp.ResolveTemplatePath(tag.Properties[ATTR_FILENAME], TAG);
                        writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write));
                        break;
                    }
                default:
                    throw new Exception("Unknown attribute file for '" + ATTR_MODE + "'.  Allowed values are: '" + VALUE_MODE_NEW + "' and '" + VALUE_MODE_APPEND + "'");
            }

            interp.OutputStream = writer;
            try
            {
                interp.HandleChildren(tag);
            }
            finally
            {
                writer.Dispose();
                interp.OutputStream = stream;
            }
        }

        public static bool IsOutput(Tag tag)
        {
            return tag.TagType == TagTypes.Tag && tag.Name == TAG;
        }
    }
}