namespace MAS.Application.Abstractions;

public interface IDatabaseBootstrapper
{
    string DatabasePath { get; }
    string SchemaScriptPath { get; }
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
}
