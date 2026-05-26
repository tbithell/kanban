using Kanban.Tests.Integration.Infrastructure;

[CollectionDefinition(nameof(SqliteCollection))]
public sealed class SqliteCollection : ICollectionFixture<SqliteTestFixture> { }
