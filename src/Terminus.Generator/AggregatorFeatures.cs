using Microsoft.CodeAnalysis;

namespace Terminus.Generator;

internal class AggregatorFeatures(SupportedAttributes supportedAttributes,  AttributeData aggregatorAttrData)
{
    private readonly INamedTypeSymbol? _facadeAttributeMatch = GetSelfOrBaseType(aggregatorAttrData.AttributeClass,
        supportedAttributes.FacadeAttributeTypeSymbol,
        supportedAttributes.ScopedFacadeAttributeTypeSymbol);

    private readonly INamedTypeSymbol? _mediatorAttributeMatch = GetSelfOrBaseType(aggregatorAttrData.AttributeClass,
        supportedAttributes.MediatorAttributeTypeSymbol,
        supportedAttributes.ScopedMediatorAttributeTypeSymbol);


    public ServiceKind ServiceKind => ResolveServiceKind();
    public bool IsScoped => ResolveIsScoped();
        
    private ServiceKind ResolveServiceKind()  
    {
        if (_facadeAttributeMatch is not null)
            return  ServiceKind.Facade;
        if (_mediatorAttributeMatch is not null)
            return ServiceKind.Mediator;

        return ServiceKind.None;
    }
        
    private bool ResolveIsScoped()
    {
        return SymbolEqualityComparer.Default.Equals(_facadeAttributeMatch, supportedAttributes.ScopedFacadeAttributeTypeSymbol)
               || SymbolEqualityComparer.Default.Equals(_mediatorAttributeMatch, supportedAttributes.ScopedMediatorAttributeTypeSymbol);
    }
        
    private static INamedTypeSymbol? GetSelfOrBaseType(
        INamedTypeSymbol? attributeClass, 
        params IEnumerable<INamedTypeSymbol> types)
    {
        var typeSet =  new HashSet<INamedTypeSymbol>(types,  SymbolEqualityComparer.Default);
        var current = attributeClass;

        while (current is not null)
        {
            if (typeSet.Contains(current))
                return current;

            current = current.BaseType;
        }

        return null;
    }
}