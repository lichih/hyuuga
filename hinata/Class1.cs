using System;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace hinata;
public class HinataServer : WebSocketBehavior
{
    protected override void OnMessage(MessageEventArgs e)
    {
        var msg = e.Data == "BALUS"
                    ? "I've been balused already..."
                    : "I'm not available now.";

        Send(msg);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var wssv = new WebSocketServer("ws://localhost:4649");
        wssv.AddWebSocketService<HinataServer>("/hinata");
        wssv.Start();
        Console.WriteLine("Hinata is listening on port 4649, and providing WebSocket services:");
        Console.ReadKey(true);
        wssv.Stop();
    }    
}