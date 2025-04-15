create schema if not exists __schema__;

create table if not exists __schema__.checkpoints (
    id varchar primary key, 
    position bigint null 
);
