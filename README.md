# SQL MCP Server

A .NET 8 MCP (Model Context Protocol) server that enables AI assistants to query SQL Server databases using natural language.

## Features

- **Natural Language Queries**: Ask questions in plain English and get SQL results back
- **Database Schema Access**: Exposes your database schema to help AI understand your data structure
- **Safe Query Execution**: Only SELECT queries are allowed, preventing accidental data modifications
- **MCP Protocol**: Works with any MCP-compatible client (Claude Desktop, Claude Code, etc.)

## Available Tools

| Tool | Description |
|------|-------------|
| `nl_query` | Execute natural language queries against the database |

### nl_query Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `prompt` | string | (required) | Natural language query to execute |
| `output_format` | string | `formatted` | Output format for results |

### Output Formats

| Format | Description |
|--------|-------------|
| `formatted` | Table format with column headers and row count |
| `json` | JSON array of objects |
| `csv` | Comma-separated values |
| `query_only` | Returns only the generated SQL without executing it |

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server with the **pubs** sample database installed
- [Anthropic API key](https://console.anthropic.com/)

### Installing the pubs Sample Database

The pubs database is a classic Microsoft sample database. You can install it from the official Microsoft SQL Server samples:

1. Download `instpubs.sql` from [Microsoft SQL Server Samples](https://github.com/microsoft/sql-server-samples/tree/master/samples/databases/northwind-pubs)
2. Run the script against your SQL Server instance

## How Schema Discovery Works

This server uses [EF Core Power Tools](https://github.com/ErikEJ/EFCorePowerTools) to reverse-engineer the database schema into C# entity classes. These entity model files (located in `MsqlMcpServer/Models/`) are read at runtime by the `db_schema` tool to provide the AI with complete knowledge of your database structure.

When you ask a natural language question:
1. The server reads the entity model `.cs` files from the Models folder
2. This schema information is sent to Claude along with your question
3. Claude generates the appropriate SQL query based on the schema
4. The query is executed and results are returned

### Adapting to Your Own Database

To use this server with a different database:

1. Install [EF Core Power Tools](https://marketplace.visualstudio.com/items?itemName=ErikEJ.EFCorePowerTools) in Visual Studio
2. Right-click the project > EF Core Power Tools > Reverse Engineer
3. Connect to your database and generate new entity models
4. Update `appsettings.json` with your connection string

## Installation

### 1. Clone the repository

```bash
git clone <repository-url>
cd sqlmcp
```

### 2. Configure settings

Create or edit `MsqlMcpServer/MsqlMcpServer/appsettings.json`:

```json
{
  "AnthropicApiKey": "your-anthropic-api-key",
  "ConnectionString": "Server=your-server;Database=your-database;User Id=your-user;Password=your-password;TrustServerCertificate=True"
}
```

### 3. Build the project

```bash
dotnet build MsqlMcpServer/MsqlMcpServer.sln
```

### 4. Configure your MCP client

Add to your `.mcp.json` file (project or user level):

```json
{
  "mcpServers": {
    "sqlmcp": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/MsqlMcpServer/MsqlMcpServer/MsqlMcpServer.csproj"]
    }
  }
}
```

## Usage

Once configured, you can ask natural language questions about your database:

- "Show me all authors from California"
- "What are the top 10 best-selling titles?"
- "List employees hired in the last year"

The server will:
1. Retrieve the database schema
2. Use Claude to generate an appropriate SQL query
3. Execute the query against your database
4. Return formatted results

### Output Format Examples

**Formatted (default):**
```
au_fname | au_lname | city
---------------------------
Johnson | White | Menlo Park
Marjorie | Green | Oakland

(2 rows)
```

**JSON:**
```json
[
  {"au_fname": "Johnson", "au_lname": "White", "city": "Menlo Park"},
  {"au_fname": "Marjorie", "au_lname": "Green", "city": "Oakland"}
]
```

**CSV:**
```
au_fname,au_lname,city
Johnson,White,Menlo Park
Marjorie,Green,Oakland
```

**Query Only:**
```sql
SELECT au_fname, au_lname, city FROM authors WHERE state = 'CA'
```


## Security Notes

- Only SELECT queries are executed; INSERT, UPDATE, DELETE, and other statements are rejected
- Keep your `appsettings.json` secure and never commit it to version control
- Use a database user with read-only permissions for additional safety
- Query results are limited to 100 rows maximum

## License

MIT
