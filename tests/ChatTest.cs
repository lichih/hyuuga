using wssrv;
namespace tests;
class ChatTests
{
    [SetUp]
    public void Setup()
    {
    }
    [Test]
    public void Test1()
    {
        // 創建一個聊天室
        ChatRoom chatRoom = new ChatRoom();

        // 參與者1發送訊息
        chatRoom.SendMessage("User1", "大家好!");

        // 參與者2發送訊息
        chatRoom.SendMessage("User2", "你好，有人在嗎？");

        // 取得所有人到目前的所有對話
        List<Message> allMessages = chatRoom.GetAllMessages();
        Console.WriteLine("所有人到目前的所有對話:");
        foreach (var message in allMessages)
        {
            Console.WriteLine($"{message.Timestamp} {message.Sender}: {message.Content}");
        }

        // 取得目前所有人最新的一段對話
        Message latestMessage = chatRoom.GetLatestMessage();
        Console.WriteLine("\n目前所有人最新的一段對話:");
        if (latestMessage != null)
        {
            Console.WriteLine($"{latestMessage.Timestamp} {latestMessage.Sender}: {latestMessage.Content}");
        }
        else
        {
            Console.WriteLine("尚無對話");
        }

        // 發出對話，傳達給其他所有參與者
        chatRoom.SendMessage("User1", "有人想要參加道具商店嗎？");

        // 更新最新的一段對話
        latestMessage = chatRoom.GetLatestMessage();
        Console.WriteLine("\n更新後的最新一段對話:");
        if (latestMessage != null)
        {
            Console.WriteLine($"{latestMessage.Timestamp} {latestMessage.Sender}: {latestMessage.Content}");
        }
        else
        {
            Console.WriteLine("尚無對話");
        }
    }
}
