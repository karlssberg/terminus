using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Terminus;

public interface IEntryPointDescriptor
{
    MethodInfo MethodInfo { get; }
    ReturnTypeKind ReturnKind { get; }

    Type EntryPointDescriptorType { get; }
    IReadOnlyDictionary<string, Type> ParameterWithAttributeBinders { get; }
    IReadOnlyDictionary<string, IParameterBinder> GetParameterBinders(IServiceProvider provider);
    object? Invoke(IBindingContext bindingContext, CancellationToken ct);
}