// Copyright (C) Eventuous HQ OÜ.All rights reserved
// Licensed under the Apache License, Version 2.0.
global using NewEvents = System.Collections.Generic.IEnumerable<object>;

namespace Eventuous;

public static partial class FuncServiceDelegates {
    public delegate ValueTask<StreamName> GetStreamNameFromUntypedCommand(object command, CancellationToken cancellationToken);

    public delegate ValueTask<NewEvents> ExecuteUntypedCommand<in T>(T state, object[] events, object command, CancellationToken cancellationToken)
        where T : State<T>;

    public delegate IEventReader ResolveReaderFromCommand(object command);

    public delegate IEventWriter ResolveWriterFromCommand(object command);

    public delegate NewStreamEvent AmendEventFromCommand(NewStreamEvent streamEvent, object command);
}