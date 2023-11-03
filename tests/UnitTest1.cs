namespace tests;
using System;
using System.Diagnostics;
using HyuugaGame.Model;
using static HyuugaGame.Model.Serialization;
public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        var a = new Asset() { AssetType = "Master"};
        Console.WriteLine($"Asset Key: {a.Key}");
        var b = a with { Key = Guid.NewGuid().ToString(), AssetType = "Slave" };

        var json_a = a.ToJson();
        var json_b = b.ToJson();
        Console.WriteLine($"json_a: {json_a}");
        Console.WriteLine($"json_b: {json_b}");

        Assert.Equals(a, @"{""Key"":""bac6ac98-4f48-40e5-a1d9-5f0390773009"",""AssetType"":""Master""}");

        Assert.Pass();
    }
}