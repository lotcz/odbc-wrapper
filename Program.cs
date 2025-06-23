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
            rows
        };
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error executing query: {ex.Message}");
    }
})
.WithName("ExecuteSqlQuery");

app.Run();
