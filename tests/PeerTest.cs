using System;
using System.Net.Http;
using System.Threading.Tasks;
namespace tests;
class PeerTests
{
    [SetUp]
    public void Setup()
    {
    }
    private Uri baseUri = new Uri("http://localhost:4649");

    [Test]
    public async Task Test1()
    {
        // 設定目標URL
        Uri url = new Uri(baseUri, "Message");

        // 使用 HttpClient 來發送 GET 請求
        using (HttpClient client = new HttpClient())
        {
            try
            {
                // 發送 GET 請求並等待回應
                HttpResponseMessage response = await client.GetAsync(url);

                // 確保回應成功
                response.EnsureSuccessStatusCode();

                // 讀取回應內容
                string responseBody = await response.Content.ReadAsStringAsync();

                // 處理回應內容
                Console.WriteLine("回應內容:");
                Console.WriteLine(responseBody);
            }
            catch (HttpRequestException e)
            {
                // 處理錯誤
                Console.WriteLine($"發生錯誤: {e.Message}");
            }
        }
    }
}
