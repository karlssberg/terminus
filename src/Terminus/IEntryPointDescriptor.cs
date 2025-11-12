using System;
using System.Reflection;
using System.Threading;

namespace Terminus;

public interface IEntryPointDescriptor
{
    MethodInfo MethodInfo { get; }
    Func<ParameterBindingContext, CancellationToken, object?> Invoker { get; }
    ReturnTypeKind ReturnKind { get; }
    
    Type EntryPointDescriptorType { get; }
    ParameterInfo[] Parameters { get; }
}