namespace wssrv;
public class ChatUser
{
    public string Name { get; set; }
    public string ConnectionId { get; set; }
    public List<string> Lines { get; set; } = new();

}
