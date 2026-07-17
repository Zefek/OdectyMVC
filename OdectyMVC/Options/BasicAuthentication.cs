namespace OdectyMVC.Options;

public class BasicAuthentication
{
    public List<BasicAuthClient> Clients { get; set; } = new();
}

public class BasicAuthClient
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public List<string> Roles { get; set; } = new();
}
