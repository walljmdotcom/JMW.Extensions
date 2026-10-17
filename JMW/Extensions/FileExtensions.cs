using System;
using System.IO;

namespace JMW.Extensions;

public static class FileExtensions
{
    public static void IfExists(this string path, Action<string> todo)
    {
        if (File.Exists(path))
        {
            todo(path);
        }
    }
}