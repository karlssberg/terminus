using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal class  SupportedAttributes(GeneratorSyntaxContext context)
{
    public const string EntryPointAttributeFullName = "Terminus.EntryPointAttribute";
    public const string FacadeAttributeFullName = "Terminus.EntryPointFacadeAttribute";
    public const string ScopedFacadeAttributeFullName = "Terminus.ScopedEntryPointFacadeAttribute";
    public const string MediatorAttributeFullName = "Terminus.EntryPointMediatorAttribute";
    public const string ScopedMediatorAttributeFullName = "Terminus.ScopedEntryPointMediatorAttribute";
    public const string RouterAttributeFullName = "Terminus.EntryPointRouterAttribute";
    public const string ScopedRouterAttributeFullName = "Terminus.ScopedEntryPointRouterAttribute";

    public INamedTypeSymbol[] ScopedAttributes =>
    [
        ScopedFacadeAttributeTypeSymbol,
        ScopedMediatorAttributeTypeSymbol,
        ScopedRouterAttributeTypeSymbol
    ];
        
    public INamedTypeSymbol  EntryPointTypeSymbol { get; } = GetTypeByMetadataName(context, EntryPointAttributeFullName);
    public INamedTypeSymbol  FacadeAttributeTypeSymbol { get; } = GetTypeByMetadataName(context, FacadeAttributeFullName);
    public INamedTypeSymbol  ScopedFacadeAttributeTypeSymbol { get; } = GetTypeByMetadataName(context, ScopedFacadeAttributeFullName);
    public INamedTypeSymbol  MediatorAttributeTypeSymbol { get; } = GetTypeByMetadataName(context, MediatorAttributeFullName);
    public INamedTypeSymbol  ScopedMediatorAttributeTypeSymbol { get; } = GetTypeByMetadataName(context, ScopedMediatorAttributeFullName);
    public INamedTypeSymbol  RouterAttributeTypeSymbol { get; } = GetTypeByMetadataName(context, RouterAttributeFullName);
    public INamedTypeSymbol  ScopedRouterAttributeTypeSymbol { get; } = GetTypeByMetadataName(context, ScopedRouterAttributeFullName);

    private static INamedTypeSymbol GetTypeByMetadataName(GeneratorSyntaxContext context, string fullTypeName)
    {
        return context.SemanticModel.Compilation.GetTypeByMetadataName(fullTypeName)
               ?? throw new InvalidOperationException($"{fullTypeName} not found");
    }
}