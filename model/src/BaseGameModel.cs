namespace HyuugaGame.Model;
public record Asset
{
    public string Key = Guid.NewGuid().ToString();
    public string AssetType;
}

public record Player
{
    public string Key; // user id
    public string Nickname;
    public int Gold;
    public List<Asset> Assets;
}
