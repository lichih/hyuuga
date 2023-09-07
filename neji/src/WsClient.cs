using System;
using System.IO; // MemoryStream
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Threading; // CancellationToken
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Skuld.Model;
using Kara.Pattern;
using Kara;
using UnityEngine;

namespace Skuld.Services {
    public record KaraConnectionData
    {
        public string SkuldScheme { get; set; } = "https";
        public string SkuldHost { get; set; } = "eir-k.4649.tw";
        public string EirHost { get; set; } = "eir-k.4649.tw";
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
    }
    public class Identity {
        public string wallet { get; set; }
        public string uid { get; set; }
        public string nickname { get; set; }
        public string avatar { get; set; }
        public bool linked { get; set; }
    }
    class AuthResult {
        public int code { get; set; }
        public string message { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public Identity identity { get; set; }
    }
    class MailResult {
        public int code { get; set; }
        public string message { get; set; }
        public List<Mail> items { get; set; }
    }
    class ShopResult {
        public int code { get; set; }
        public string message { get; set; }
        public List<ShopAsset> items { get; set; }
    }
    public partial class KaraService {
        HashSet<string> messages = new HashSet<string>();
        // SemaphoreSlim messagesLock = new SemaphoreSlim(0);
        public HttpClient NewClient() {
            if(this == null) throw new ArgumentNullException("service");
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", this.accessToken);
            return client;
        }
        void addMessage(string msg) {
            // if(this.messages.Count == 0) this.messagesLock.Release();
            this.messages.Add(msg);
        }
        void addMessage(string[] newMessages) {
            if(newMessages.Count() == 0) return;
            // if(this.messages.Count == 0) this.messagesLock.Release();
            foreach(var m in newMessages) {
                this.messages.Add(m);
            }
        }
        // skuld data stream web socket connection async worker
        public void refreshMails() {
            addMessage("mail");
        }
        public void refreshShopAssets() {
            addMessage("shop");
        }
        public async Task<int> Run(CancellationToken cancelToken, WatchMany w, params string[] initialMessages) {
            ThrowConnDataExceptions(true);
            addMessage(initialMessages);
            var subTasks = new Task[] {
                MessageDealerLoop(cancelToken, w),
                // refreshTokensAsync(cancelToken),
            };
            var taskLoop = DataStreamLoop(cancelToken, w);
            while(!cancelToken.IsCancellationRequested) {
                try {
                    await Task.Delay(1000, cancelToken);
                    // TODO: 檢查 token exp ，在適當的時間從 /auth/refresh 取得新的 token
                }
                catch (TaskCanceledException) {
                    Debug.LogWarning("KaraService.Run() TaskCanceledException");
                    break;
                }
            }
            // Debug.Log($"KaraService.Run() leaving {cancelToken.IsCancellationRequested}");
            // Task.WaitAll(taskMessageDealer, refreshTask);
            // Task.WaitAll(taskMessageDealer);
            // Task.WaitAll(refreshTask);
            Task.WaitAll(subTasks, 2000);
            // Debug.Log($"KaraService.Run() left");
            return await taskLoop;
        }
        class RefreshResult {
            public int code { get; set; }
            public string message { get; set; }
            public JObject identity { get; set; }
            public string access_token { get; set; }
            public string refresh_token { get; set; }
        }
        async Task refreshTokensAsync(CancellationToken cancelToken)
        {
            // 每30分鐘更新一次 /auth/refresh
            while(!cancelToken.IsCancellationRequested) {
                try {
                    var client = NewClient();
                    var url = $"https://{this.EirHost}/api/auth/refresh";
                    var resp = await client.PostAsync(url, null, cancelToken);
                    var statuscode = resp.StatusCode.GetHashCode();
                    var contents = await resp.Content.ReadAsStringAsync();
                    if(statuscode == 200)
                    {
                        RefreshResult o = JsonConvert.DeserializeObject<RefreshResult>(contents);
                        this.accessToken = o.access_token;
                        this.refreshToken = o.refresh_token;
                        this.addMessage("token");
                    }
                    await Task.Delay(1000 * 60 * 30, cancelToken);
                }
                catch (TaskCanceledException) {
                    break;
                }
            }
        }
        async Task MessageDealerLoop(CancellationToken cancelToken, WatchMany states) {
            while(!cancelToken.IsCancellationRequested) {
                try {
                    // while(messagesLock.CurrentCount > 1) {
                    //     messagesLock.Wait(cancelToken);
                    // }
                    // await messagesLock.WaitAsync(cancelToken);
                    await Task.Delay(200, cancelToken);
                    if(this.messages.Count == 0) {
                        continue;
                    }
                    else
                    {
                        var msgs = this.messages;
                        this.messages = new HashSet<string>();
                        // Debug.Log($"*** states.messages: {messages.Count}::{JsonConvert.SerializeObject(messages)} {this.messages.Contains("mail")}");
                        foreach(var m_ in msgs) {
                            if(m_=="mail") {
                                await ReadUserMailAsync(states);
                            }
                            else if(m_=="shop") {
                                await ReadUserShopAsync(states);
                            }
                            else if (m_=="token") {
                                // TODO: 更新 access_token 至 websocket peer (skuld server side)
                                if(this.ws != null) {
                                    var msg = new {
                                        type = "token",
                                        token = this.accessToken
                                    };
                                    var msgStr = JsonConvert.SerializeObject(msg);
                                    var msgBytes = Encoding.UTF8.GetBytes(msgStr);
                                    // var msgStream = new ReadOnlyMemory<byte>(msgBytes); // not supported in .net framework 4.8
                                    var msgStream = new ArraySegment<byte>(msgBytes);
                                    await this.ws.SendAsync(msgStream, WebSocketMessageType.Text, true, cancelToken);
                                }
                            }
                            else {
                                Debug.LogWarning($"*** unknown message: {m_}");
                            }
                        }
                    }
                }
                catch(TaskCanceledException) {
                    break;
                }
            }
        }
        ClientWebSocket ws = null;
        Dictionary<string, JObject> rpcResponses = new ();
        async Task<int> DataStreamLoop(CancellationToken cancelToken, WatchMany w) {
            var url = $"wss://{SkuldHost}/skuld/wsapi/user/{identity.uid}/ws";
            Debug.Log($"ws connect to {url}");
            var uri = new Uri(url);
            var bytesReceived = WebSocket.CreateClientBuffer(4096, 4096);
            try {
                while(!cancelToken.IsCancellationRequested)
                {
                    await Task.Yield();
                    if(ws == null) {
                        ws = new ClientWebSocket();
                        ws.Options.AddSubProtocol("ws.kara.skuld");
                        ws.Options.SetRequestHeader("Authorization", "Bearer " + this.accessToken);
                        try {
                            // Debug.Log("Connecting...");
                            await ws.ConnectAsync(uri, cancelToken);
                        }
                        catch(WebSocketException) {
                            ws = null;
                            await Task.Delay(3000, cancelToken);
                        }
                    }
                    else if(ws.State == WebSocketState.Open) {
                        try {
                            using (var ms = new MemoryStream()) {
                                while(true) {
                                    WebSocketReceiveResult rs = await ws.ReceiveAsync(bytesReceived, cancelToken);
                                    // Debug.Log($"ws received {rs.Count} bytes");
                                    ms.Write(bytesReceived.Array, 0, rs.Count);
                                    if(rs.EndOfMessage) break;
                                }
                                ms.Seek(0, SeekOrigin.Begin);
                                using (var sr = new StreamReader(ms)) {
                                    String jsons = sr.ReadToEnd();
                                    JObject j = JObject.Parse(jsons);
                                    IList<string> keys = j.Properties().Select(p => p.Name).ToList();
                                    // Debug.Log($"KEYS {keys.Count} keys, keys: {String.Join(",", keys)}");
                                    foreach(var k in keys) {
                                        // Debug.Log($"ws received key: {k}");
                                        try {
                                            if(k == "rpc") {
                                                var rpc = j["rpc"].ToObject<JObject>();
                                                var key = rpc["key"].ToObject<string>();
                                                rpcResponses[key] = rpc;
                                                await Task.Delay(200);
                                            }
                                            else if(k == "player") {
                                                Player p = j["player"].ToObject<Player>();
                                                //w.Set(p);
                                                w.Update((BackendDataHolder d) => d.Player = p);
                                            }
                                            else if (k == "assets") {
                                                AssetCollection assets = j["assets"].ToObject<AssetCollection>();
                                                //w.Set(assets);
                                                w.Update((BackendDataHolder d) => d.AssetsCollection = assets);
                                            }
                                            else if (k == "messages") {
                                                var messages = j["messages"].ToObject<string[]>();
                                                addMessage(messages);
                                            }
                                            else {
                                                Debug.LogWarning($"ws received unknown key: {k}");
                                            }
                                        }
                                        catch(Exception e) {
                                            Debug.LogWarning($"ws received exception: {e} {e.InnerException}");
                                        }
                                    }
                                    await Task.Yield();
                                }
                            }
                        }
                        catch(WebSocketException) {
                            ws = null;
                        }
                    }
                    else {
                        // Debug.Log(ws.State.ToString());
                        await Task.Delay(1000, cancelToken);
                    }
                }
                // Debug.Log($"DataStreamLoop canceled::{cancelToken.IsCancellationRequested}");
            }
            catch(OperationCanceledException) {
                // Debug.Log("DataStreamLoop canceled");
                if(ws!=null) {
                    if(ws.State == WebSocketState.Open) {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                    }
                    ws = null;
                }
            }

            return 0;
        }
        // 讀取玩家郵件
        public async Task<int> ReadUserMailAsync(WatchMany w) {
            ThrowConnDataExceptions(true);
            var url = $"https://{this.EirHost}/api/user/{this.identity.uid}/mail";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", this.accessToken);
            var resp = await client.GetAsync(url);
            this.lastStatuscode = resp.StatusCode.GetHashCode();
            var contents = await resp.Content.ReadAsStringAsync();
            this.lastContent = contents;
            MailResult o = JsonConvert.DeserializeObject<MailResult>(contents);
            MailCollection mails = new MailCollection();
            foreach(var m in o.items) {
                mails.Add(m.MailId, m);
            }
            w.Set(mails);
            return 0;
        }
        // 讀取玩家上架商店的商品清單
        public async Task<int> ReadUserShopAsync(WatchMany w) {
            ThrowConnDataExceptions(true);
            var url = $"https://{this.EirHost}/api/user/{this.identity.uid}/shop";
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", this.accessToken);
            var resp = await client.GetAsync(url);
            this.lastStatuscode = resp.StatusCode.GetHashCode();
            var contents = await resp.Content.ReadAsStringAsync();
            this.lastContent = contents;
            ShopResult o = JsonConvert.DeserializeObject<ShopResult>(contents);
            ShopAssetCollection shops = new ShopAssetCollection();
            foreach(var s in o.items) {
                shops.Add(s.AssetId, s);
            }
            w.Set(shops);
            return 0;
        }
    
        internal string SkuldHost { get; set; }
        internal string EirHost { get; set; }
        internal string User { get; set; }
        public Identity identity { get; set; }
        public int lastStatuscode { get; set; }
        public int lastCode { get; set; }
        public string lastMessage { get; set; }
        public string lastContent { get; set; }
        public string accessToken { get; set; }
        public string refreshToken { get; set; }
        // eir auth by username/password API
        public void ApplyConnectionData(KaraConnectionData connData) {
            this.EirHost = connData.EirHost;
            this.SkuldHost = connData.SkuldHost;
            this.User = connData.User;
        }
        private void ThrowConnDataExceptions(bool mustAuthed) {
            if(this.EirHost == null) throw new ArgumentNullException("EirHost");
            if(this.User == null) throw new ArgumentNullException("User");
            if(mustAuthed) {
                if(identity == null) {
                    throw new Exception("not authenticated");
                }
                if(accessToken == null) {
                    throw new Exception("not authenticated");
                }
            }
        }
        public async Task<int> AuthUserAsync(KaraConnectionData connData) {
            // Debug.Log($"{connData.SkuldHost} {connData.EirHost} {connData.User} {connData.Password}");
            if(connData==null) {
                throw new ArgumentNullException("connData");
            }
            if(connData.EirHost==null) {
                throw new ArgumentNullException("connData.EirApiUrl");
            }
            if(connData.SkuldHost==null) {
                throw new ArgumentNullException("connData.SkuldApiUrl");
            }
            if(connData.User==null) {
                throw new ArgumentNullException("connData.User");
            }
            if(connData.Password==null) {
                throw new ArgumentNullException("connData.Password");
            }

            var url = $"https://{connData.EirHost}/api/auth/pwd";
            var content = new StringContent(JsonConvert.SerializeObject(
                new JObject {
                    {"uid", connData.User},
                    {"pwd", connData.Password}
                }), Encoding.UTF8, "application/json");
            var client = new HttpClient();
            var resp = await client.PostAsync(url, content);
            this.lastStatuscode = resp.StatusCode.GetHashCode();
            var contents = await resp.Content.ReadAsStringAsync();
            this.lastContent = contents;
            Debug.Log($"AuthUserAsync {this.lastStatuscode} {this.lastContent}");
            if(this.lastStatuscode < 200 || this.lastStatuscode >= 300) {
                return -1;
            }

            AuthResult o = JsonConvert.DeserializeObject<AuthResult>(contents);
            if(o!=null) {
                this.lastCode = o.code;
                this.lastMessage = o.message;
                this.identity = o.identity;
                this.accessToken = o.access_token;
                this.refreshToken = o.refresh_token;
            }
            ApplyConnectionData(connData);
            return 0;
        }

        // eir image api
        public string MailAssetImageUrl(Mail mail, MailAttachment a=null) {
            ThrowConnDataExceptions(false);
            if(mail==null) throw new ArgumentNullException("Mail");
            if(a==null) a = mail.Attachments.FirstOrDefault();
            if(a==null) throw new ArgumentNullException("MailAttachment");
            // "/user/{uid}/mail/{mail_id}/attachment/{asset_id}/image"
            return $"https://{this.EirHost}/api/user/{this.User}/mail/{mail.MailId}/attachment/{a.AssetId}/image";
        }
        public string AssetImageUrl(Asset a) {
            ThrowConnDataExceptions(false);
            if(a == null) throw new ArgumentNullException("asset");
            return $"https://{this.EirHost}/api/user/{this.User}/assets/{a.AssetId}/image";
        }

        public string ShopImageUrl(Asset a) {
            ThrowConnDataExceptions(false);
            if(a == null) throw new ArgumentNullException("asset");
            return $"https://{this.EirHost}/api/shop/{a.AssetId}/image";
        }
    }

    public class BackendDataHolder
    {
        public bool Sync;
        public Player Player;
        public AssetCollection AssetsCollection;
    }
}
