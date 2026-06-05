using Conduit.Tests.Shared;
using Xunit;

namespace Conduit.Infrastructure.Tests;

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
