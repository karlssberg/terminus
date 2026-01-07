using Microsoft.CodeAnalysis.Testing;
using Terminus.Generator.Tests.Unit.Generator.Infrastructure;

namespace Terminus.Generator.Tests.Unit.Generator;

public class FacadeGeneratorErrorTests
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

        var test = new TerminusSourceGeneratorTest<FacadeGenerator>
        {
            TestState =
            {
                Sources = { (SourceFilename, source) },
            }
        };
        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;

        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("TM0001")
                .WithSpan(SourceFilename, 14, 28, 14, 33) // A.Hello() method identifier
                .WithArguments("Hello")
        );

        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("TM0001")
                .WithSpan(SourceFilename, 20, 28, 20, 33) // B.Hello() method identifier
                .WithArguments("Hello")
        );

        await test.RunAsync();
    }

    [Fact]
    public async Task Given_generic_entry_point_method_Should_fail_compilation_with_TM0002()
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
                    public static T Echo<T>(T value) => value;
                }
            }
            """;

        var test = new TerminusSourceGeneratorTest<FacadeGenerator>
        {
            TestState =
            {
                Sources = { (SourceFilename, source) },
            }
        };
        
        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;

        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("TM0002")
                .WithSpan(SourceFilename, 14, 25, 14, 29) // TestFacadeMethods.Echo<T>() method identifier
                .WithArguments("Echo")
        );

        await test.RunAsync();
    }


    [Fact]
    public async Task Given_the_presence_of_ref_and_out_parameters_Should_fail_TM0003()
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

        var test = new TerminusSourceGeneratorTest<FacadeGenerator>
        {
            TestState =
            {
                Sources = { (SourceFilename, source) },
            }
        };
        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;

        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("TM0003")
                .WithSpan(SourceFilename, 14, 47, 14, 52) // TestFacadeMethods.ProcessRef(ref int value) parameter 'value'
                .WithArguments("ProcessRef", "value")
        );

        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("TM0003")
                .WithSpan(SourceFilename, 17, 47, 17, 52) // TestFacadeMethods.ProcessOut(out int value) parameter 'value'
                .WithArguments("ProcessOut", "value")
        );
        
        await test.RunAsync();
    }
}
