using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Skuld.Model
{
    public record Attachment {
        public byte[] bytes;
    }
    public record Mail {
        public string MailId;
        public string title, sender, receipt;
        public string content;
        public List<Attachment> Attachments;
    }
    public record MailCollection {
        public Dictionary<string, Mail> collection;
    }
    public record ShopAsset {
        public string Uid { get; set; }
        public string AssetId { get; set; }
        public string AssetType { get; set; }
        public int? TemplateId { get; set; }
        public string[] Tags { get; set; }
        public int Price { get; set; }
        public int? Expire { get; set; }
        public int Created { get; set; }
        public int Updated { get; set; }
    }
    public record ShopAssetCollection {
        public Dictionary<string, ShopAsset> collection;
    }
    public record Player {
        public string Uid { get; set; }
        public string Nickname { get; set; }
        public int Gold { get; set; }
    }
    public enum SlaveStatus {
        stNone,
        stStandby,
        stWorking,
        stShutdown,
        stOverworked,
        stDying,
    }
    public class RoomStatus {
        [JsonProperty("floor")]
        public int Floor;
        [JsonProperty("room")]
        public int Room;
        [JsonProperty("slave_key")]
        public string SlaveKey = null;
        public string CustomerKey = null;
        public RoomStatus Clone() {
            return new RoomStatus {
                Floor = this.Floor,
                Room = this.Room,
                SlaveKey = this.SlaveKey,
                CustomerKey = this.CustomerKey,
            };
        }
    }
    public record SlaveState {
        public long TemplateId = 0;
        public int Level = 1;
        // public Kara.Model.SlaveRarity Rarity = Kara.Model.SlaveRarity.None;
        public int HP = 100;
        public int Stamina = 100;
        public int Exp = 0;
        public SlaveStatus Status = SlaveStatus.stNone;
        public string InRoomKey = null;
        public long[] Attrs = null;
        public string[] Prefers = null;
        // public (Skill Skill, int Level)[] Skills = null;
    }
    public record Asset {
        public string AssetId { get; set; }
        public string AssetType { get; set; }
        public bool Read = false;
        public long TemplateId = 0;
        public string Loc { get; set; }
        public RoomStatus RoomStatus = null;
        public SlaveState SlaveState = null;
    }
    public record AssetCollection {
        public Dictionary<string, Asset> collection;
        public static AssetCollection Merge(AssetCollection a, AssetCollection b) {
            AssetCollection c = new AssetCollection();
            foreach(var kv in a.collection) {
                c.collection.Add(kv.Key, kv.Value with {} as Asset);
            }
            foreach(var kv in b.collection) {
                c.collection[kv.Key] = kv.Value with {} as Asset;
            }
            return c;
        }
    }
    public record SlaveMarketAsset {
        public Asset SlaveAsset;
        public bool Enabled = true;
    }
    public record SlaveMarketAssets {
        public List<SlaveMarketAsset> Slaves;
    }
}
