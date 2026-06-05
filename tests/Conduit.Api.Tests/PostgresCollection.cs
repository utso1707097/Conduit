using Conduit.Tests.Shared;
using Xunit;

namespace Conduit.Api.Tests;

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
