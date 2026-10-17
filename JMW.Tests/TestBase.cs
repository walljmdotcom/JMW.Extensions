using System;
using System.IO;

namespace JMW.Tests;

public class TestBase
{
    public string ExecutionPath;

    public TestBase()
    {
        this.ExecutionPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..");
    }
}
