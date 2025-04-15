// Copyright (C) Eventuous HQ OÜ. All rights reserved
// Licensed under the Apache License, Version 2.0.

using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Eventuous.Postgresql;

public record SchemaOptions {

    public static SchemaOptions Default => new SchemaOptions();
    /// <summary>
    /// Includes schema for using postgres as an event store 
    /// </summary>
    public bool IncludeEventStoreSchema { get; set; } = true;
    /// <summary>
    /// Includes schema for using postgres as a checkpoint store 
    /// </summary>
    public bool IncludeCheckpointStoreSchema { get; set; } = true;
}


/// <summary>
/// Instantiate a new Schema object with the specified schema name. The default schema name is "eventuous"
/// </summary>
/// <param name="schema"></param>
/// <param name="options"></param>
public class Schema(string schema = Schema.DefaultSchema, SchemaOptions? options = null) {
    public const string DefaultSchema = "eventuous";

    public static string GetStreamMessageTypeName(string schema = DefaultSchema) => $"{schema}.stream_message";

    public string StreamMessage       => GetStreamMessageTypeName(schema);
    public string AppendEvents        => $"select * from {schema}.append_events(@_stream_name, @_expected_version, @_created, @_messages)";
    public string ReadStreamForwards  => $"select * from {schema}.read_stream_forwards(@_stream_name, @_from_position, @_count)";
    public string ReadStreamBackwards => $"select * from {schema}.read_stream_backwards(@_stream_name, @_from_position, @_count)";
    public string ReadStreamSub       => $"select * from {schema}.read_stream_sub(@_stream_id, @_stream_name, @_from_position, @_count)";
    public string ReadAllForwards     => $"select * from {schema}.read_all_forwards(@_from_position, @_count)";
    public string CheckStream         => $"select * from {schema}.check_stream(@_stream_name, @_expected_version)";
    public string StreamExists        => $"select exists (select 1 from {schema}.streams where stream_name = (@name))";
    public string TruncateStream      => $"select * from {schema}.truncate_stream(@_stream_name, @_expected_version, @_position)";
    public string GetCheckpointSql    => $"select position from {schema}.checkpoints where id=(@checkpointId)";
    public string AddCheckpointSql    => $"insert into {schema}.checkpoints (id) values (@checkpointId)";
    public string UpdateCheckpointSql => $"update {schema}.checkpoints set position=(@position) where id=(@checkpointId)";

    static readonly Assembly Assembly = typeof(Schema).Assembly;

    public async Task CreateSchema(NpgsqlDataSource dataSource, ILogger<Schema>? log, CancellationToken cancellationToken = default) {
        log?.LogInformation("Creating schema {Schema}", schema);
        
        var schemaOptions                        = options ?? SchemaOptions.Default;
        var names         = Assembly.GetManifestResourceNames()
            .Where(x => (schemaOptions.IncludeCheckpointStoreSchema && IsCheckpointStoreScript(x)) || (schemaOptions.IncludeEventStoreSchema && IsEventStoreScript(x)))
            .OrderBy(x => x);
        
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).NoContext();

        var transaction = await connection.BeginTransactionAsync(cancellationToken).NoContext();

        try {
            foreach (var name in names) {
                log?.LogInformation("Executing {Script}", name);
                await using var stream = Assembly.GetManifestResourceStream(name);
                using var       reader = new StreamReader(stream!);

#if NET7_0_OR_GREATER
                var script = await reader.ReadToEndAsync(cancellationToken).NoContext();
#else
                var script = await reader.ReadToEndAsync().NoContext();
#endif
                var cmdScript = script.Replace("__schema__", schema);

                await using var cmd = new NpgsqlCommand(cmdScript, connection, transaction);

                await cmd.ExecuteNonQueryAsync(cancellationToken).NoContext();
            }
        } catch (Exception e) {
            log?.LogCritical(e, "Unable to initialize the database schema");
            await transaction.RollbackAsync(cancellationToken);

            throw;
        }

        await transaction.CommitAsync(cancellationToken).NoContext();
        log?.LogInformation("Database schema initialized");

        return;
        
        static bool IsCheckpointStoreScript(string name) => name.EndsWith("_Checkpoints.sql");
        static bool IsEventStoreScript(string name) => name.EndsWith(".sql") && !name.EndsWith("_Checkpoints.sql");
    }
}
