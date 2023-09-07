using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Kara.Model.Templates;
using Kara;

namespace Skuld.Model
{
    public class Data {
        public static T DeepClone<T>(T obj) {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj));
        }
        public static Dictionary<string,T> Merge<T>(Dictionary<string,T> a, Dictionary<string,T> b)
        {
            var c = new Dictionary<string,T>();
            foreach(var kv in a) {
                c.Add(kv.Key, DeepClone<T>(kv.Value));
            }
            foreach(var kv in b) {
                c[kv.Key] = DeepClone<T>(kv.Value);
            }
            return c;
        }
    }
    public class ShopAsset : ICloneable {
        [JsonProperty("uid")]
        public string Uid { get; set; }
        [JsonProperty("asset_id")]
        public string AssetId { get; set; }
        [JsonProperty("asset_type")]
        public string AssetType { get; set; }
        [JsonProperty("template_id")]
        public int? TemplateId { get; set; }
        [JsonProperty("meta")]
        public JObject Meta { get; set; }
        [JsonProperty("tags")]
        public string[] Tags { get; set; }
        [JsonProperty("price")]
        public int Price { get; set; }
        [JsonProperty("expire")]
        public int? Expire { get; set; }
        [JsonProperty("created")]
        public int Created { get; set; }
        [JsonProperty("updated")]
        public int Updated { get; set; }
        public ShopAsset Clone() {
            return new ShopAsset {
                Uid = this.Uid,
                AssetId = this.AssetId,
                AssetType = this.AssetType,
                TemplateId = this.TemplateId,
                Meta = this.Meta,
                Tags = this.Tags,
                Price = this.Price,
                Expire = this.Expire,
                Created = this.Created,
                Updated = this.Updated
            };
        }
        object ICloneable.Clone() {
            return this.Clone();
        }
    }
    public class ShopAssetCollection : Dictionary<string, ShopAsset>, ICloneable {
        public ShopAssetCollection Clone() {
            ShopAssetCollection clone = new ShopAssetCollection();
            foreach(var kv in this) {
                clone.Add(kv.Key, kv.Value.Clone() as ShopAsset);
            }
            return clone;
        }
        object ICloneable.Clone() {
            return this.Clone();
        }
    }
    public class Player : ICloneable {
        [JsonProperty("uid")]
        public string Uid { get; set; }
        [JsonProperty("name")]
        public string Nickname { get; set; }
        [JsonProperty("gold")]
        public int Gold { get; set; }
        public Player Clone() {
            return new Player {
                Uid = this.Uid,
                Nickname = this.Nickname,
                Gold = this.Gold,
            };
        }
        object ICloneable.Clone() {
            return this.Clone();
        }
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
    // public class SlaveState : ICloneable
    public record SlaveState
    {
        public long TemplateId = 0;
        [JsonProperty("lvl")]
        public int Level = 1;
        [JsonProperty("rarity", NullValueHandling = NullValueHandling.Ignore)]
        public Kara.Model.SlaveRarity Rarity = Kara.Model.SlaveRarity.None;

        [JsonProperty("hp")]
        public int HP = 100;
        [JsonProperty("stamina")]
        public int Stamina = 100;
        [JsonProperty("exp")]
        public int Exp = 0;
        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public SlaveStatus Status = SlaveStatus.stNone;
        [JsonProperty("in_room_key")]
        public string InRoomKey = null;
        [JsonProperty("attrs")]
        public long[] Attrs = null;
        [JsonProperty("prefers")]
        public string[] Prefers = null;
        [JsonProperty("skills")]
        public (Skill Skill, int Level)[] Skills = null;
    }
    public partial class Asset : ICloneable {
        [JsonProperty("asset_id")]
        public string AssetId { get; set; }

        [JsonProperty("asset_type")]
        public string AssetType { get; set; }

        [JsonProperty("read")]
        public bool Read = false;
        [JsonProperty("template_id")]
        public long TemplateId = 0;

        [JsonProperty("meta")]
        public JObject Meta { get; set; }

        [JsonProperty("loc")]
        public string Loc { get; set; }

        [JsonProperty("room_status")]
        public RoomStatus RoomStatus = null;

        [JsonProperty("slave_state", NullValueHandling = NullValueHandling.Ignore)]
        public SlaveState SlaveState = null;

        public Asset Clone() {
            return new Asset {
                AssetId = this.AssetId,
                AssetType = this.AssetType,
                TemplateId = this.TemplateId,
                Read = this.Read,
                Meta = this.Meta?.DeepClone() as JObject,
                Loc = this.Loc,
                RoomStatus = this.RoomStatus?.Clone(),
                SlaveState = this.SlaveState with {},
            };
        }
        object ICloneable.Clone() {
            return this.Clone();
        }
    }
    public class AssetCollection : Dictionary<string, Asset>, ICloneable
    {
        public static AssetCollection Merge(AssetCollection a, AssetCollection b) {
            AssetCollection c = new AssetCollection();
            foreach(var kv in a) {
                c.Add(kv.Key, kv.Value.Clone() as Asset);
            }
            foreach(var kv in b) {
                c[kv.Key] = kv.Value.Clone() as Asset;
            }
            return c;
        }
        public AssetCollection Clone() {
            AssetCollection clone = new AssetCollection();
            foreach(var kv in this) {
                clone.Add(kv.Key, kv.Value.Clone() as Asset);
            }
            return clone;
        }
        object ICloneable.Clone() {
            return this.Clone();
        }
    }
    public class SlaveMarketAsset {
        public Asset SlaveAsset;
        public bool Enabled = true;
    }
    public record SlaveMarketAssets {
        public List<SlaveMarketAsset> Slaves;
    }
}
