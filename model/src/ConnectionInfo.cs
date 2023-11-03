namespace HyuugaGame.Connection;

public record AuthInfo
{
    public string UserID;
    public string Password;
}
public interface IConnectionInfo
{
    string GetServerURL();
    AuthInfo GetStoredAuthInfo();
}
