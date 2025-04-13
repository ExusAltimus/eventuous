// Copyright (C) Eventuous HQ OÜ.All rights reserved
// Licensed under the Apache License, Version 2.0.

namespace Eventuous;

public static partial class FuncServiceDelegates {
    public static ExecuteUntypedCommand<TState> AsExecute<TCommand, TState>(this Func<TCommand, CancellationToken, Task<NewEvents>> execute)
        where TState : State<TState> where TCommand : class
        => async (_, _, command, token) => await execute((TCommand)command, token).NoContext();

    public static ResolveReaderFromCommand AsResolveReader<TCommand>(this Func<TCommand, IEventReader> resolveReader) where TCommand : class
        => cmd => resolveReader((TCommand)cmd);

    public static ResolveWriterFromCommand AsResolveWriter<TCommand>(this Func<TCommand, IEventWriter> resolveWriter) where TCommand : class
        => cmd => resolveWriter((TCommand)cmd);
    
    public static AmendEventFromCommand AsAmendEvent<TCommand>(this AmendEvent<TCommand> amendEvent) where TCommand : class
        => (streamEvent, cmd) => amendEvent(streamEvent, (TCommand)cmd);
}
