namespace api_server.Models
{
    public record CreateUserData(string Username, string Password);
    public record DeleteUserData(string username);

    public class SyslogClientOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 514;
        public string serviceName { get; set; } = "api_server";
    }

    public class KeycloakOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:8080";
        public string Realm { get; set; } = "master";
        public string ClientId { get; set; } = "api-client";
        public string ClientSecret { get; set; } = "A34T45rVUbaHorGoUQ5OgjUmGomg9BzN";
    }

    public class UnixDomainSocketClientOptions
    {
        public string SocketPath { get; set; } = "/tmp/database_server.sock";
    }
}