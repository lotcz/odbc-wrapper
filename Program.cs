using System.Data;
using System.Data.Odbc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
var config = builder.Configuration;

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/query", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var sql = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(sql))
        return Results.BadRequest("SQL query is required in the request body.");

    Console.WriteLine($"Received SQL query: {sql}");

    var connectionString = config["Odbc:ConnectionString"] ?? "";
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("ODBC connection string is not configured.");

    try
    {
        using var connection = new OdbcConnection(connectionString);
        await connection.OpenAsync();
        using var command = new OdbcCommand(sql, connection);
        using var dataReader = await command.ExecuteReaderAsync();
        var columns = new List<string>();
        for (int i = 0; i < dataReader.FieldCount; i++)
        {
            columns.Add(dataReader.GetName(i));
        }
        var rows = new List<object[]>();
        while (await dataReader.ReadAsync())
        {
            var row = new object[dataReader.FieldCount];
            for (int i = 0; i < dataReader.FieldCount; i++)
            {
                row[i] = dataReader.IsDBNull(i) ? null : dataReader.GetValue(i);
            }
            rows.Add(row);
        }
        var result = new
        {
            columns,
            rows,
            recordsAffected = dataReader.RecordsAffected
        };
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error executing query: {ex.Message}");
    }
})
.WithName("ExecuteSqlQuery");

app.MapGet("/tables", async () =>
{
    var connectionString = config["Odbc:ConnectionString"] ?? "";
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.Problem("ODBC connection string is not configured.");

    try
    {
        using var connection = new OdbcConnection(connectionString);
        await connection.OpenAsync();
        // Restrict to TABLE type only
        DataTable tables = connection.GetSchema("Tables");
        var tableNames = new List<string>();
        foreach (DataRow row in tables.Rows)
        {
            // Table name is in the "TABLE_NAME" column
            tableNames.Add(row["TABLE_NAME"].ToString());
        }
        return Results.Ok(tableNames);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error retrieving tables: {ex.Message}");
    }
});

app.Run();
