namespace tests;
using System;
using System.Diagnostics;
using HyuugaGame.Model;
using HyuugaGame.Connection;
using static HyuugaGame.Model.Serialization;

public record ConnectionSetting
{
    public string ServerURL;
    public string UserID;
    public string Password;
}

public class TestConnectionInfo : IConnectionInfo
{
    ConnectionSetting _setting = null;
    void ReadSetting()
    {
        if (_setting != null) return;
        // open hytest.yaml from etc under home directory
        var yaml = new YamlDotNet.Serialization.DeserializerBuilder()
            .Build()
            .Deserialize<ConnectionSetting>(System.IO.File.ReadAllText(
                System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "etc/hytest.yaml")));
        _setting = yaml;
    }
    public string GetServerURL()
    {
        ReadSetting();
        return _setting.ServerURL;

    }

    public AuthInfo GetStoredAuthInfo()
    {
        ReadSetting();
        return new AuthInfo() { UserID = _setting.UserID, Password = _setting.Password };
    }
}
public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestConnectionInfo()
    {
        var ci = new TestConnectionInfo();
        var url = ci.GetServerURL();
        var auth = ci.GetStoredAuthInfo();
        Console.WriteLine($"url: {url}");
        Console.WriteLine($"auth: {auth.ToJson()}");
        Assert.Pass();
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

        Assert.Pass();
    }
}
