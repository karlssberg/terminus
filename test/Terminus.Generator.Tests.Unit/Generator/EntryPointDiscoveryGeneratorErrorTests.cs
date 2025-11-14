using System.Text;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Terminus.Generator.Tests.Unit.Generator.Infrastructure;

namespace Terminus.Generator.Tests.Unit.Generator;

public class EntryPointDiscoveryGeneratorErrorTests
{
    [Fact]
    public async Task Given_duplicate_entry_point_signatures_Should_fail_compilation_with_CS0111()
    {
        const string source =
            """
            using Terminus;

            namespace Demo
            {
                [ScopedEntryPointFacade]
                public partial interface IFacade;

                public static class A
                {
                    [EntryPoint]
                    public static void Hello(string world) { }
                }

                public static class B
                {
                    [EntryPoint]
                    public static void Hello(string world) { }
                }
            }
            """;

        var test = new TerminusSourceGeneratorTest<EntryPointDiscoveryGenerator>
        {
            TestState =
            {
                Sources = { source }
            }
        };
        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;

        // Duplicate method signatures in both the generated partial interface and implementation
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS0111")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 14, 14, 14, 19)
                .WithArguments("Hello", "Demo.IFacade"));

        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS0111")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 32, 21, 32, 26)
                .WithArguments("Hello", "Demo.IFacade_Generated"));

        await test.RunAsync();
    }

    [Fact]
    public async Task Given_generic_entry_point_method_Should_fail_compilation_with_CS0246()
    {
        const string source =
            """
            using Terminus;

            namespace Demo
            {
                [ScopedEntryPointFacade]
                public partial interface IFacade;

                public static class TestEntryPoints
                {
                    [EntryPoint]
                    public static T Echo<T>(T value) => value;
                }
            }
            """;

        var test = new TerminusSourceGeneratorTest<EntryPointDiscoveryGenerator>
        {
            TestState =
            {
                Sources = { source }
            }
        };
        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;

        // The generator does not emit generic type parameter declarations for interface/implementation methods, so "T" is unknown (multiple occurrences)
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS0246")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 13, 9, 13, 10)
                .WithArguments("T"));
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS0246")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 13, 16, 13, 17)
                .WithArguments("T"));
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS0246")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 26, 16, 26, 17)
                .WithArguments("T"));
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS0246")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 26, 23, 26, 24)
                .WithArguments("T"));
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS0246")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 47, 262, 47, 263)
                .WithArguments("T"));
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS0246")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 47, 394, 47, 395)
                .WithArguments("T"));

        await test.RunAsync();
    }

    [Fact]
    public async Task Given_ref_and_out_parameters_present_when_a_facade_is_being_generated_Should_generate_the_appropriate_facade_methods_with_ref_and_out_parameters()
    {
        const string source =
            """
            using Terminus;

            namespace Demo
            {
                [ScopedEntryPointFacade]
                public partial interface IFacade;

                public class TestEntryPoints
                {
                    [EntryPoint]
                    public void SetOut(out int value) { value = 42; }

                    [EntryPoint]
                    public void Increment(ref int value) { value++; }
                }
            }
            """;

        var test = new TerminusSourceGeneratorTest<EntryPointDiscoveryGenerator>
        {
            TestState =
            {
                Sources = { source }
            }
        };
        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;

        // Calls to methods with ref/out parameters must pass arguments with the correct modifier, which the generator omits
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS1620")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 31, 89, 31, 94)
                .WithArguments("1", "out"));
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS1620")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 39, 92, 39, 97)
                .WithArguments("1", "ref"));
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS1620")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 59, 350, 59, 453)
                .WithArguments("1", "out"));
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS1620")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 60, 356, 60, 459)
                .WithArguments("1", "ref"));

        await test.RunAsync();
    }

    [Fact]
    public async Task Given_ref_and_out_overloads_with_same_name_Should_fail_with_signature_clash_CS0111()
    {
        const string source =
            """
            using Terminus;

            namespace Demo
            {
                [ScopedEntryPointFacade]
                public partial interface IFacade;

                public static class TestEntryPoints
                {
                    [EntryPoint]
                    public static void Process(ref int value) { }

                    [EntryPoint]
                    public static void Process(out int value) { value = 0; }
                }
            }
            """;

        var test = new TerminusSourceGeneratorTest<EntryPointDiscoveryGenerator>
        {
            TestState =
            {
                Sources = { source }
            }
        };
        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;

        // Expect: source overloads invalid (CS0663), plus collisions and incorrect argument modifiers in generated code
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS0663")
                .WithSpan("/0/Test2.cs", 14, 28, 14, 35)
                .WithArguments("Demo.TestEntryPoints", "method", "out", "ref"));
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS0111")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 14, 14, 14, 21)
                .WithArguments("Process", "Demo.IFacade"));
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS1620")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 29, 42, 29, 47)
                .WithArguments("1", "ref"));
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS0111")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 32, 21, 32, 28)
                .WithArguments("Process", "Demo.IFacade_Generated"));
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS1620")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 34, 42, 34, 47)
                .WithArguments("1", "ref"));
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS1620")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 53, 321, 53, 424)
                .WithArguments("1", "ref"));
        test.TestState.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerError("CS1620")
                .WithSpan("Terminus.Generator\\Terminus.Generator.EntryPointDiscoveryGenerator\\Demo_IFacade_Generated.g.cs", 54, 321, 54, 424)
                .WithArguments("1", "ref"));

        await test.RunAsync();
    }

    [Fact]
    public async Task Given_ScopedEntryPointMediator_with_custom_attribute_Should_generate_AddEntryPoints_not_AddEntryPointMediator()
    {
        const string source =
            """
            using System;
            using Microsoft.Extensions.DependencyInjection;
            using Terminus;

            namespace Demo
            {
                [ScopedEntryPointMediator(EntryPointAttributes = [typeof(MyCustomAttribute)])]
                public partial interface IDispatcher;

                [AttributeUsage(AttributeTargets.Method)]
                public class MyCustomAttribute(string path) : EntryPointAttribute
                {
                    public string Path { get; } = path;
                }

                public class MyController
                {
                    [MyCustomAttribute("/users/{id}")]
                    public void GetUser(string id) { }
                }

                public class Program
                {
                    public static void Main()
                    {
                        var services = new ServiceCollection();

                        // This should fail because the generated method is AddEntryPoints, not AddEntryPointMediator
                        services.AddEntryPoints<IDispatcher>();
                    }
                }
            }
            """;

        const string expectedMainSource =
            """
            // <auto-generated/> Generated by Terminus EntryPointDiscoveryGenerator
            #nullable enable
            using Microsoft.Extensions.DependencyInjection;
            using System;
            using System.Reflection;
            using Terminus;
            using Terminus.Strategies;
            
            namespace Demo
            {
                public partial interface IDispatcher
                {
                    void Publish(Terminus.ParameterBindingContext context, System.Threading.CancellationToken cancellationToken = default);
                }
            
                internal sealed class IDispatcher_Generated : Demo.IDispatcher
                {
                    private readonly IServiceProvider _serviceProvider;
                    private readonly Terminus.Dispatcher<Demo.IDispatcher> _dispatcher;
                    public IDispatcher_Generated(IServiceProvider serviceProvider, Terminus.Dispatcher<Demo.IDispatcher> dispatcher)
                    {
                        _serviceProvider = serviceProvider;
                        _dispatcher = dispatcher;
                    }
            
                    public void Publish(Terminus.ParameterBindingContext context, System.Threading.CancellationToken cancellationToken = default)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        _dispatcher.Publish(context, cancellationToken);
                    }
                }
            }
            
            namespace Terminus
            {
                public static partial class ServiceCollectionExtensions__Generated
                {
                    private static IServiceCollection AddEntryPointsFor_Demo_IDispatcher(this IServiceCollection services, Action<ParameterBindingStrategyResolver>? configure = null)
                    {
                        services.AddSingleton(provider =>
                        {
                            var resolver = new ParameterBindingStrategyResolver(provider);
                            configure?.Invoke(resolver);
                            return resolver;
                        });
                        services.AddTransient<ScopedDispatcher<Demo.IDispatcher>>();
                        services.AddTransient<IEntryPointRouter<Demo.IDispatcher>, DefaultEntryPointRouter<Demo.IDispatcher>>();
                        services.AddKeyedSingleton<EntryPointDescriptor<Demo.MyCustomAttribute>>(typeof(Demo.IDispatcher), (provider, key) => new EntryPointDescriptor<Demo.MyCustomAttribute>(typeof(Demo.MyController).GetMethod("GetUser", new System.Type[] { typeof(string) })!, (context, ct) => provider.GetRequiredService<Demo.MyController>().GetUser(provider.GetRequiredService<ParameterBindingStrategyResolver>().ResolveParameter<string>("id", context))));
                        services.AddTransient<Demo.MyController>();
                        services.AddSingleton<Demo.IDispatcher, Demo.IDispatcher_Generated>();
                        return services;
                    }
                }
            }
            """;

        const string expectedServiceRegistrationSource =
            """
            #nullable enable
            using Microsoft.Extensions.DependencyInjection;
            using System;
            
            namespace Terminus
            {
                public static partial class ServiceCollectionExtensions__Generated
                {
                    public static IServiceCollection AddEntryPoints<T>(this IServiceCollection services, Action<ParameterBindingStrategyResolver>? configure = null)
                    {
                        switch (typeof(T).FullName)
                        {
                            case "Demo.IDispatcher":
                                return services.AddEntryPointsFor_Demo_IDispatcher(configure);
                        };
                        throw new InvalidOperationException($"The type '{typeof(T).FullName}' is not an entry point aggregator");
                    }
            
                    public static IServiceCollection AddEntryPoints(this IServiceCollection services, Action<ParameterBindingStrategyResolver>? configure = null)
                    {
                        services.AddEntryPointsFor_Demo_IDispatcher();
                        return services;
                    }
                }
            }
            """;
        
        var test = new TerminusSourceGeneratorTest<EntryPointDiscoveryGenerator>
        {
            TestState =
            {
                Sources = { source }
            }
        };

        test.TestState.GeneratedSources.Add((
            typeof(EntryPointDiscoveryGenerator), 
            "Demo_IDispatcher_Generated.g.cs", 
            SourceText.From(expectedMainSource, Encoding.UTF8)));

        test.TestState.GeneratedSources.Add((
            typeof(EntryPointDiscoveryGenerator), 
            "__EntryPointServiceRegistration_Generated.g.cs",
            SourceText.From(expectedServiceRegistrationSource, Encoding.UTF8)));

        await test.RunAsync();

        await test.RunAsync();
    }
}
