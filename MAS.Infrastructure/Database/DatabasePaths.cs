namespace MAS.Infrastructure.Database;

public static class DatabasePaths
{
    public static string Root => Path.Combine(AppContext.BaseDirectory, "Data");

    public static string DatabaseFile => Path.Combine(Root, "MASQC.db");

    public static string SchemaResourceName => "MAS.Infrastructure.Database.Schema.mas_schema.sql";
}
