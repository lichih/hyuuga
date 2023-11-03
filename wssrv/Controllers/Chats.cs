using Microsoft.AspNetCore.Mvc;

namespace wssrv.Controllers;

[ApiController]
[Route("[controller]")]
public class ChatsController : ControllerBase
{
    private readonly ILogger<ChatsController> _logger;

    public ChatsController(ILogger<ChatsController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetUsers")]
    public IEnumerable<ChatUser> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new ChatUser
        {
            Name = $"User {index}",
            ConnectionId = Guid.NewGuid().ToString(),
            Lines = new List<string> { "Hello", "World" }
        })
        .ToArray();
    }
}
