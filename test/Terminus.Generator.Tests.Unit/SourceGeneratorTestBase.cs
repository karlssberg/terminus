using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Terminus.Generator.Tests.Unit.Generator.Infrastructure;

namespace Terminus.Generator.Tests.Unit;

public abstract class SourceGeneratorTestBase<TSourceGenerator> 
    where TSourceGenerator : IIncrementalGenerator, new()
{
    protected static Task VerifyAsync(
        string source,
        params (string filename, string content)[] expectedGeneratedFiles)
    {
        var test = new TerminusSourceGeneratorTest<TSourceGenerator>
        {
            TestState = { Sources = { source } }
        };

        foreach (var (filename, content) in expectedGeneratedFiles)
        {
            test.TestState.GeneratedSources.Add(
                (typeof(TSourceGenerator), filename, SourceText.From(NormalizeLineEndings(content), Encoding.UTF8)));
        }

        return test.RunAsync();
    }

    protected static Task VerifyAsync(
        string[] sources,
        params (string filename, string content)[] expectedGeneratedFiles)
    {
        var test = new TerminusSourceGeneratorTest<TSourceGenerator>
        {
            TestState = { Sources = { } }
        };

        foreach (var source in sources)
        {
            test.TestState.Sources.Add(source);
        }

        foreach (var (filename, content) in expectedGeneratedFiles)
        {
            test.TestState.GeneratedSources.Add(
                (typeof(TSourceGenerator), filename, SourceText.From(NormalizeLineEndings(content), Encoding.UTF8)));
        }

        return test.RunAsync();
    }

    protected static Task VerifyAsync(
        string source,
        DiagnosticResult[] expectedDiagnostics,
        bool skipGeneratedSourcesCheck = true,
        string? sourceFilename = null)
    {
        var test = new TerminusSourceGeneratorTest<TSourceGenerator>
        {
            TestState = { Sources = { (sourceFilename ?? "Source.cs", source) } }
        };

        if (skipGeneratedSourcesCheck)
        {
            test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;
        }

        test.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

        return test.RunAsync();
    }

    private static string NormalizeLineEndings(string text)
    {
        return text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\n", Environment.NewLine);
    }
}