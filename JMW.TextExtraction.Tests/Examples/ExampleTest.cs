using System.Text;

namespace JMW.TextExtraction.Tests.Examples;

/// <summary>
/// Runs every example folder: infers a spec from the folder's <c>input-*.txt</c> files, checks it against
/// <c>spec.txt</c>, and checks what each input reads as against its <c>input-*.expected.txt</c>.
/// </summary>
/// <remarks>
/// The examples are written by hand to the layouts real devices print, with made-up names and addresses.
/// When a change to the library changes what is read, run <see cref="WriteExpectations"/>, then review the
/// diff of the expected files line by line before keeping it.
/// </remarks>
[TestFixture]
public class ExampleTest
{
    public static IEnumerable<TestCaseData> Folders() =>
        Directory.GetDirectories(Root(AppContext.BaseDirectory), "*", SearchOption.AllDirectories)
            .Where(static d => Directory.GetFiles(d, "input-*.txt").Length > 0)
            .Order(StringComparer.Ordinal)
            .Select(d => new TestCaseData(Path.GetRelativePath(Root(AppContext.BaseDirectory), d).Replace('\\', '/')).SetName(Path.GetRelativePath(Root(AppContext.BaseDirectory), d).Replace('\\', '/')));

    [TestCaseSource(nameof(Folders))]
    public void ReadsAsExpected(string folder)
    {
        string dir = Path.Combine(Root(AppContext.BaseDirectory), folder);
        (DocumentSpec spec, List<(string Path, string Text)> inputs) = Infer(dir);

        Assert.That(SpecText.Write(spec), Is.EqualTo(Normalize(File.ReadAllText(Path.Combine(dir, "spec.txt")))), "spec.txt");
        foreach ((string path, string text) in inputs)
        {
            string expected = Normalize(File.ReadAllText(ExpectedPath(path)));
            Assert.That(Rendering.Render(TextExtractor.Extract(spec, text, inferUnknown: false), text), Is.EqualTo(expected), Path.GetFileName(path));
        }
    }

    [Test]
    [Explicit("Rewrites the expected files in the source tree from what the library reads now.")]
    public void WriteExpectations()
    {
        string source = Root(SourceFolder());
        foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories).Where(static d => Directory.GetFiles(d, "input-*.txt").Length > 0))
        {
            (DocumentSpec spec, List<(string Path, string Text)> inputs) = Infer(dir);
            File.WriteAllText(Path.Combine(dir, "spec.txt"), SpecText.Write(spec), new UTF8Encoding(false));
            foreach ((string path, string text) in inputs)
            {
                File.WriteAllText(ExpectedPath(path), Rendering.Render(TextExtractor.Extract(spec, text, inferUnknown: false), text), new UTF8Encoding(false));
            }
        }
    }

    private static (DocumentSpec Spec, List<(string Path, string Text)> Inputs) Infer(string dir)
    {
        List<(string Path, string Text)> inputs = Directory.GetFiles(dir, "input-*.txt")
            .Where(static f => !f.EndsWith(".expected.txt", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .Select(static f => (f, Normalize(File.ReadAllText(f))))
            .ToList();
        return (TextExtractor.Infer(inputs.Select(static i => i.Text).ToList()), inputs);
    }

    private static string ExpectedPath(string input) => Path.ChangeExtension(input, ".expected.txt");

    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string Root(string baseFolder) => Path.Combine(baseFolder, "Examples");

    // The test project folder, found by walking up from the test assembly to the project file.
    private static string SourceFolder()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "JMW.TextExtraction.Tests.csproj")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("The test project folder was not found above " + AppContext.BaseDirectory);
    }
}
