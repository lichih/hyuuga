namespace tests;
using System;
using System.Diagnostics;
using Skuld.Model;
public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        var m = new Mail();
        // Debug.Print($"mail id: {m.MailId}");
        Console.WriteLine($"mail id: {m.MailId}");
        // _output.WriteLine($"id: {m.MailId}");
        Assert.Pass();
    }
}