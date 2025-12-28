using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using Microsoft.Data.SqlClient;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Anthropic.SDK.Constants;

namespace MsqlMcpServer;

[McpServerToolType]
public static class NaturalLanguageQueryTool
{
    private const int MaxRows = 100;

    [McpServerTool, Description("Execute a natural language query against the database. Returns query results in a readable format.")]
    public static async Task<string> nl_query(string prompt)
    {
        try
        {
            // 1. Get API key from configuration
            var apiKey = AppConfig.Configuration["AnthropicApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return "Error: AnthropicApiKey not found in appsettings.json";

            // 2. Get schema
            var schema = DBSchemaTool.GetSchema();
            if (schema.StartsWith("Error:"))
                return schema;

            // 3. Call Claude to generate SQL
            var sql = await GenerateSqlAsync(apiKey, schema, prompt);
            if (sql.StartsWith("Error:"))
                return sql;

            // 4. Validate SELECT only
            if (!sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                return $"Error: Only SELECT queries allowed. Generated: {sql}";

            // 5. Execute and format results
            return await ExecuteQueryAsync(sql);
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private static async Task<string> GenerateSqlAsync(string apiKey, string schema, string prompt)
    {
        try
        {
            var client = new AnthropicClient(apiKey);

            var systemPrompt = @"You are a SQL Server expert. Given the database schema and user request, generate ONLY a valid T-SQL SELECT query.
Output ONLY the SQL query, no explanations or markdown.
The schema shows C# entity classes. IMPORTANT: Convert PascalCase property names to snake_case for actual column names (e.g., AuId -> au_id, AuLname -> au_lname, AuFname -> au_fname, TitleId -> title_id).
Table names are lowercase (e.g., Authors -> authors, Titles -> titles).
Use proper SQL Server syntax.";

            var userMessage = $@"Database Schema:
{schema}

User Request: {prompt}

Generate the SQL query:";

            var messages = new List<Message>
            {
                new Message(RoleType.User, userMessage)
            };

            var parameters = new MessageParameters
            {
                Model = AnthropicModels.Claude35Haiku,
                MaxTokens = 500,
                System = new List<SystemMessage> { new SystemMessage(systemPrompt) },
                Messages = messages
            };

            var response = await client.Messages.GetClaudeMessageAsync(parameters);

            // Extract text from the response content
            var textContent = response.Content.FirstOrDefault(c => c is TextContent) as TextContent;
            return textContent?.Text?.Trim() ?? "Error: No text response from Claude";
        }
        catch (Exception ex)
        {
            return $"Error generating SQL: {ex.Message}";
        }
    }

    private static async Task<string> ExecuteQueryAsync(string sql)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Query: {sql}");
        sb.AppendLine();

        try
        {
            var connectionString = AppConfig.Configuration["ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
                return "Error: ConnectionString not found in appsettings.json";

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 30;

            using var reader = await command.ExecuteReaderAsync();

            // Get column names
            var columns = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
                columns.Add(reader.GetName(i));

            sb.AppendLine(string.Join(" | ", columns));
            sb.AppendLine(new string('-', columns.Count * 15));

            // Read rows
            int rowCount = 0;
            while (await reader.ReadAsync() && rowCount < MaxRows)
            {
                var values = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                    values.Add(reader.IsDBNull(i) ? "NULL" : reader.GetValue(i)?.ToString() ?? "NULL");
                sb.AppendLine(string.Join(" | ", values));
                rowCount++;
            }

            sb.AppendLine();
            sb.AppendLine($"({rowCount} rows)");

            return sb.ToString();
        }
        catch (SqlException ex)
        {
            return $"{sb}SQL Error: {ex.Message}";
        }
    }
}
