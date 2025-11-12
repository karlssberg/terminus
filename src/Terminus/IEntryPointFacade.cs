using System;

namespace Terminus;

public interface IEntryPointFacade
{
    Type[] EntryPointAttributes { get; set; }
    Type[] TargetTypes { get; }
}