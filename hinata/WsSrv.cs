using System;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace hinata;
public class EchoSrv : WebSocketBehavior
{
    protected override void OnMessage(MessageEventArgs e)
    {
        var msg = $"received data: [{e.Data}][{e.Data.Length}]";
        Send(msg);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var wssv = new WebSocketServer("ws://localhost:4649");
        wssv.AddWebSocketService<EchoSrv>("/echo");
        wssv.Start();
        Console.WriteLine("Hinata is listening on port 4649, and providing WebSocket services:");
        Console.ReadKey(true);
        wssv.Stop();
    }    
}
