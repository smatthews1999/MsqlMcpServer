using Microsoft.Extensions.Configuration;

namespace MsqlMcpServer;

public static class AppConfig
{
    public static IConfiguration Configuration { get; private set; } = null!;

    public static void Initialize(IConfiguration configuration)
    {
        Configuration = configuration;
    }
}
