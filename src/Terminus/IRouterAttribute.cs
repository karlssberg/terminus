using System;

namespace Terminus;

public interface IRouterAttribute
{
    Type[] EntryPointAttributes { get; set; }
    Type[] TargetTypes { get; }
}