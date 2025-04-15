// Copyright (C) Eventuous HQ OÜ.All rights reserved
// Licensed under the Apache License, Version 2.0.

using Eventuous.Postgresql.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Eventuous.Postgresql;


public class SchemaInitializer(IServiceProvider sp, ILoggerFactory? loggerFactory = null) : IHostedService {
    public async Task StartAsync(CancellationToken cancellationToken) {

        var storeOptions           = sp.GetService<IOptions<PostgresStoreOptions>>();
        var checkpointStoreOptions = sp.GetService<IOptions<PostgresCheckpointStoreOptions>>();
            
        if (storeOptions?.Value.InitializeDatabase != true && checkpointStoreOptions?.Value.InitializeDatabase != true) return;
        
        var schemaOptions = new SchemaOptions() {
            IncludeEventStoreSchema      = storeOptions?.Value.SchemaOptions.IncludeEventStoreSchema == true && storeOptions?.Value.InitializeDatabase           == true,
            IncludeCheckpointStoreSchema = storeOptions?.Value.SchemaOptions.IncludeCheckpointStoreSchema == true && (checkpointStoreOptions?.Value.InitializeDatabase ?? storeOptions?.Value.InitializeDatabase == true)
        };
        
        if (schemaOptions is { IncludeCheckpointStoreSchema: false, IncludeEventStoreSchema: false }) return;
        var dataSource            = sp.GetRequiredService<NpgsqlDataSource>();
        var storeSchema           = storeOptions?.Value.Schema           ?? Schema.DefaultSchema;
        var checkpointStoreSchema = checkpointStoreOptions?.Value.Schema ?? storeOptions?.Value.Schema ?? Schema.DefaultSchema;

        if (storeSchema == checkpointStoreSchema) {
            var schema        = new Schema(storeSchema, schemaOptions);
            await schema.CreateSchema(dataSource, loggerFactory?.CreateLogger<Schema>(), cancellationToken);
        }
        else {
            if (schemaOptions.IncludeCheckpointStoreSchema) {
                var schema        = new Schema(checkpointStoreSchema, new SchemaOptions() { IncludeEventStoreSchema = false, IncludeCheckpointStoreSchema = true });
                await schema.CreateSchema(dataSource, loggerFactory?.CreateLogger<Schema>(), cancellationToken);
            }
            
            if (schemaOptions.IncludeEventStoreSchema) {
                var schema        = new Schema(storeSchema, new SchemaOptions() { IncludeEventStoreSchema = true, IncludeCheckpointStoreSchema = false });
                await schema.CreateSchema(dataSource, loggerFactory?.CreateLogger<Schema>(), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
