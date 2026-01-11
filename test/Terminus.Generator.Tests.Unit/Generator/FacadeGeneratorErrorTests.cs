using Microsoft.CodeAnalysis.Testing;
namespace Terminus.Generator.Tests.Unit.Generator;

public class FacadeGeneratorErrorTests : SourceGeneratorTestBase<FacadeGenerator>
{
    private const string SourceFilename = "Source.cs";

    [Fact]
    public async Task Given_duplicate_entry_point_signatures_Should_fail_compilation_with_custom_diagnostic_error_TM0001()
    {
        const string source =
            """
            using System;
            using Terminus;

            namespace Demo
            {
                [FacadeOf(typeof(FacadeMethodAttribute), Scoped=true)]
                public partial interface IFacade;
            
                public class FacadeMethodAttribute : Attribute;

                public static class A
                {
                    [FacadeMethod]
                    public static void Hello(string world) { }
                }

                public static class B
                {
                    [FacadeMethod]
                    public static void Hello(string world) { }
                }
            }
            """;

        await VerifyAsync(
            source,
            expectedDiagnostics: [
                DiagnosticResult.CompilerError("TM0001")
                    .WithSpan(SourceFilename, 14, 28, 14, 33) // A.Hello() method identifier
                    .WithArguments("Hello"),
                DiagnosticResult.CompilerError("TM0001")
                    .WithSpan(SourceFilename, 20, 28, 20, 33) // B.Hello() method identifier
                    .WithArguments("Hello"),
            ],
            sourceFilename: SourceFilename);
    }

    [Fact]
    public async Task Given_the_presence_of_ref_and_out_parameters_Should_fail_TM0002()
    {
        const string source =
            """
            using System;
            using Terminus;

            namespace Demo
            {
                [FacadeOf(typeof(FacadeMethodAttribute), Scoped=true)]
                public partial interface IFacade;
            
                public class FacadeMethodAttribute : Attribute;

                public static class TestFacadeMethods
                {
                    [FacadeMethod]
                    public static void ProcessRef(ref int value) { }

                    [FacadeMethod]
                    public static void ProcessOut(out int value) { value = 0; }
                }
            }
            """;

        await VerifyAsync(
            source,
            expectedDiagnostics: [
                DiagnosticResult.CompilerError("TM0002")
                    .WithSpan(SourceFilename, 14, 47, 14, 52) // TestFacadeMethods.ProcessRef(ref int value) parameter 'value'
                    .WithArguments("ProcessRef", "value"),
                DiagnosticResult.CompilerError("TM0002")
                    .WithSpan(SourceFilename, 17, 47, 17, 52) // TestFacadeMethods.ProcessOut(out int value) parameter 'value'
                    .WithArguments("ProcessOut", "value"),
            ],
            sourceFilename: SourceFilename);
    }

    [Fact]
    public async Task Given_duplicate_entry_point_signatures_due_to_name_override_Should_fail_TM0001()
    {
        const string source =
            """
            using System;
            using Terminus;

            namespace Demo
            {
                [FacadeOf(typeof(FacadeMethodAttribute), Scoped=true, CommandName="Execute")]
                public partial interface IFacade;
            
                public class FacadeMethodAttribute : Attribute;

                public static class A
                {
                    [FacadeMethod]
                    public static void Hello(string world) { }
                }

                public static class B
                {
                    [FacadeMethod]
                    public static void Goodbye(string world) { }
                }
            }
            """;

        await VerifyAsync(
            source,
            expectedDiagnostics: [
                DiagnosticResult.CompilerError("TM0001")
                    .WithSpan(SourceFilename, 14, 28, 14, 33) // A.Hello() method identifier
                    .WithArguments("Hello"),
                DiagnosticResult.CompilerError("TM0001")
                    .WithSpan(SourceFilename, 20, 28, 20, 35) // B.Goodbye() method identifier
                    .WithArguments("Goodbye"),
            ],
            sourceFilename: SourceFilename);
    }
}
