namespace JMW.TextExtraction.Tests;

/// <summary>The hand-written samples in <c>Samples/</c>, copied beside the test assembly at build.</summary>
public static class SampleFiles
{
    public static string Read(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Samples", name));
}
