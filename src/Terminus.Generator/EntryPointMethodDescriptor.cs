namespace Terminus.Generator;

internal readonly record struct EntryPointMethodDescriptor(
    string MethodName,
    string ContainingType,
    string AttributeType,
    AttributeInfo AttributeData,
    bool IsStatic
)
{
    public string MethodName { get; } = MethodName;
    public string ContainingType { get; } = ContainingType;
    public string AttributeType { get; } = AttributeType;
    public AttributeInfo AttributeData { get; } = AttributeData;
    public bool IsStatic { get; } = IsStatic;
};