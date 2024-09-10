using Newtonsoft.Json.Linq;
using PenMotion.System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PenMotion.Datas.Items;
using PenMotion.Datas.Items.Elements;

namespace PenMotion.Datas;

public class MotionFile {
    public class MotionItemData {
        public string parent;
        public MotionItemBase item;

        public MotionItemData(string parent, MotionItemBase item) {
            this.parent = parent;
            this.item = item;
        }
    }

    public const string RootFolderName = "__RootFolder";

    public bool IsFilePathAvailable => !string.IsNullOrEmpty(filePath);
    public string filePath;

    public Dictionary<string, MotionItemBase> itemDict;

    public MotionFolderItem rootFolder;

    public delegate void MotionItemDelegate(MotionItemBase item, MotionFolderItem parentFolder);

    public event MotionItemDelegate ItemCreated;
    public event MotionItemDelegate ItemRemoved;

    public MotionFile(bool createRootFolder = true) {
        itemDict = new Dictionary<string, MotionItemBase>();

        if (createRootFolder) {
            rootFolder = new MotionFolderItem(this);
            rootFolder.SetName(RootFolderName);
        }
    }

    public void Save(string filename) {
        JObject jFile = ToJObject();

        using (var fileStream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite)) {
            using (var writer = new StreamWriter(fileStream, Encoding.UTF8)) {
                writer.Write(jFile.ToString());
            }
        }
    }

    public void Load(string filename) {
        filePath = filename;

        string jsonString;
        using (var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read)) {
            using (var reader = new StreamReader(fileStream, Encoding.UTF8)) {
                jsonString = reader.ReadToEnd();
            }
        }

        JObject jRoot = JObject.Parse(jsonString);

        string version = jRoot["Version"].ToObject<string>();

        switch (version) {
            case "2.0":
                LoadFromJson_V2_0(jRoot);
                break;
            default:
                LoadFromJson_V3_0(jRoot);
                break;
        }
    }

    public JObject ToJObject() {
        var jFile = new JObject();
        jFile.Add("Version", SystemInfo.Version);

        var jElements = new JArray();
        jFile.Add("Elements", jElements);

        AddItemRecursive(rootFolder);

        void AddItemRecursive(MotionItemBase item) {
            var jItem = new JObject();

            if (item.IsRoot) {
                jFile.Add("RootFolder", jItem);
            } else {
                jElements.Add(jItem);

                jItem.Add("Parent", item.Parent.Guid);
            }

            jItem.Add("Type", item.Type.ToString());
            jItem.Add("Guid", item.Guid);
            jItem.Add("Name", item.Name);

            if (item.Type == MotionItemType.Motion) {
                var jPoints = new JArray();
                jItem.Add("Point", jPoints);

                var motionItem = item as MotionItem;

                for (int pointI = 0; pointI < motionItem.pointList.Count; ++pointI) {
                    var jPoint = new JArray();
                    jPoints.Add(jPoint);

                    MotionPoint point = motionItem.pointList[pointI];
                    jPoint.Add(point.MainPoint.ToString());
                    jPoint.Add(point.SubPoints[0].ToString());
                    jPoint.Add(point.SubPoints[1].ToString());
                }
            } else {
                var folderItem = item as MotionFolderItem;

                for (int i = 0; i < folderItem.childList.Count; ++i) {
                    MotionItemBase childItem = folderItem.childList[i];
                    AddItemRecursive(childItem);
                }
            }
        }

        return jFile;
    }

    public void LoadFromJson_V3_0(JObject jRoot) {
        Dictionary<string, MotionFolderItem> folderDict = new();

        List<MotionItemData> childItemList = new();

        var jRootFolder = jRoot["RootFolder"] as JObject;

        LoadFolder(jRootFolder, true);

        var jElements = jRoot["Elements"] as JArray;

        foreach (JToken jElement in jElements) {
            var jElementObj = jElement as JObject;

            LoadItem(jElementObj);
        }

        foreach (MotionItemData itemData in childItemList) {
            if (folderDict.TryGetValue(itemData.parent, out MotionFolderItem parentFolder)) {
                rootFolder.RemoveChild(itemData.item);
                parentFolder.AddChild(itemData.item);
            }
        }

        void LoadItem(JToken jItem) {
            JToken jType = jItem["Type"];

            if (jType == null) return;

            string typeText = jType.ToString();
            string name = jItem["Name"].ToObject<string>();

            switch (typeText) {
                case nameof(MotionItemType.Motion):
                    LoadMotion(jItem);
                    break;
                case nameof(MotionItemType.Folder):
                    LoadFolder(jItem, false);
                    break;
            }
        }

        void LoadMotion(JToken jItem) {
            var jPoints = jItem["Point"] as JArray;

            string guid = null;
            JToken jGuid = jItem["Guid"];
            if (jGuid != null) {
                guid = jGuid.ToObject<string>();
            }

            MotionItem motion = CreateMotionEmpty(null, guid);
            motion.SetName(jItem["Name"].ToObject<string>());
            string parentGuid = jItem["Parent"].ToObject<string>();

            foreach (JToken jPointToken in jPoints) {
                var jPoint = jPointToken as JArray;

                var point = new MotionPoint(
                    PVector2.Parse(jPoint[0].ToObject<string>()),
                    PVector2.Parse(jPoint[1].ToObject<string>()),
                    PVector2.Parse(jPoint[2].ToObject<string>()));
                motion.AddPoint(point);
            }

            if (parentGuid != rootFolder.Guid) {
                childItemList.Add(new MotionItemData(parentGuid, motion));
            }
        }

        void LoadFolder(JToken jItem, bool isRoot) {
            string guid = null;
            JToken jGuid = jItem["Guid"];
            if (jGuid != null) {
                guid = jGuid.ToObject<string>();
            }

            MotionFolderItem folder = CreateFolder(null, guid);

            if (isRoot) {
                rootFolder = folder;
            }

            folder.SetName(jItem["Name"].ToObject<string>());

            folderDict.Add(folder.Guid, folder);

            if (!isRoot) {
                string parentGuid = jItem["Parent"].ToObject<string>();

                if (parentGuid != rootFolder.Guid) {
                    childItemList.Add(new MotionItemData(parentGuid, folder));
                }
            }
        }
    }

    public void LoadFromJson_V2_0(JObject jRoot) {
        //MotionTree
        var jRootFolder = jRoot[RootFolderName] as JObject;
        LoadItemRecursive(jRootFolder, null);


        void LoadItemRecursive(JToken jItem, MotionFolderItem parent) {
            JToken jType = jItem["Type"];
            string typeText = jType != null ? jType.ToString() : null;
            string name = (jItem.Parent as JProperty).Name;

            if (typeText == "Motion") {
                LoadMotion(parent, jItem, name);
            } else {
                LoadFolder(parent, jItem, name);
            }
        }

        void LoadMotion(MotionFolderItem parent, JToken jItem, string name) {
            var jData = jItem["Data"] as JObject;
            var jPoints = jData["Point"] as JArray;

            MotionItem motion = CreateMotionEmpty(parent);
            motion.SetName(name);

            if (jData["Guid"] != null) {
                motion.Guid = jData["Guid"].ToObject<string>();
            }

            foreach (JToken jPointToken in jPoints) {
                var jPoint = jPointToken as JArray;

                var point = new MotionPoint(
                    PVector2.Parse(jPoint[0].ToObject<string>()),
                    PVector2.Parse(jPoint[1].ToObject<string>()),
                    PVector2.Parse(jPoint[2].ToObject<string>()));
                motion.AddPoint(point);
            }
        }

        void LoadFolder(MotionFolderItem parent, JToken jItem, string name) {
            MotionFolderItem folder = CreateFolder(parent);

            if (parent == null) {
                rootFolder = folder;
            } else {
                folder.SetName(name);
            }

            var jItems = jItem["Items"] as JObject;
            foreach (JToken jChild in jItems.Children()) {
                var jChildProp = jChild as JProperty;
                LoadItemRecursive(jChildProp.Value, folder);
            }
        }
    }

    public float GetMotionValueByGuid(string guid, float linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
        return GetMotionValue(GetMotionByGuid(guid), linearValue, maxSample, tolerance);
    }

    public PVector2 GetMotionValueByGuid(string guid, PVector2 linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
        return GetMotionValue(GetMotionByGuid(guid), linearValue, maxSample, tolerance);
    }

    public PVector3 GetMotionValueByGuid(string guid, PVector3 linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
        return GetMotionValue(GetMotionByGuid(guid), linearValue, maxSample, tolerance);
    }

    public float GetMotionValueByName(string motionId, float linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
        return GetMotionValue(GetMotionByName(motionId), linearValue, maxSample, tolerance);
    }

    public PVector2 GetMotionValueByName(string motionId, PVector2 linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
        return GetMotionValue(GetMotionByName(motionId), linearValue, maxSample, tolerance);
    }

    public PVector3 GetMotionValueByName(string motionId, PVector3 linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
        return GetMotionValue(GetMotionByName(motionId), linearValue, maxSample, tolerance);
    }

    public float GetMotionValue(MotionItem motion, float linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
        return motion.GetMotionValue(linearValue, maxSample, tolerance);
    }

    public PVector2 GetMotionValue(MotionItem motion, PVector2 linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
        return new PVector2(
            motion.GetMotionValue(linearValue.x, maxSample, tolerance),
            motion.GetMotionValue(linearValue.y, maxSample, tolerance)
        );
    }

    public PVector3 GetMotionValue(MotionItem motion, PVector3 linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
        return new PVector3(
            motion.GetMotionValue(linearValue.x, maxSample, tolerance),
            motion.GetMotionValue(linearValue.y, maxSample, tolerance),
            motion.GetMotionValue(linearValue.z, maxSample, tolerance)
        );
    }

    public MotionItem CreateMotionDefault(MotionFolderItem parentFolder = null, string name = null) {
        MotionItem motion = CreateMotionEmpty(parentFolder, name);
        MotionItem.CreateDefault(motion);

        return motion;
    }

    public MotionItem CreateMotionEmpty(MotionFolderItem parentFolder = null, string guid = null) {
        if (parentFolder == null) {
            parentFolder = rootFolder;
        }

        var motion = new MotionItem(this);
        if (guid != null) {
            motion.Guid = guid;
        }

        itemDict.Add(motion.Guid, motion);

        ItemCreated?.Invoke(motion, parentFolder);

        parentFolder.AddChild(motion);
        motion.SetName(GetNewName(MotionItemType.Motion));

        return motion;
    }

    public MotionFolderItem CreateFolder(MotionFolderItem parentFolder = null, string guid = null) {
        if (parentFolder == null)
            parentFolder = rootFolder;

        var folder = new MotionFolderItem(this);
        if (guid != null) {
            folder.Guid = guid;
        }

        itemDict.Add(folder.Guid, folder);

        ItemCreated?.Invoke(folder, parentFolder);

        if (parentFolder != null) {
            parentFolder.AddChild(folder);
        }

        folder.SetName(GetNewName(MotionItemType.Folder));

        return folder;
    }

    public void RemoveItem(MotionItemBase item) {
        if (item.Type == MotionItemType.Folder) {
            //메세지 띄우기

            var folderItem = (MotionFolderItem)item;
            foreach (MotionItemBase childItem in folderItem.childList.ToList()) {
                RemoveItem(childItem);
            }
        }

        MotionFolderItem parentFolder = item.Parent;

        item.Parent.childList.Remove(item);
        itemDict.Remove(item.Guid);

        ItemRemoved?.Invoke(item, parentFolder);
    }

    public MotionItem GetMotionByGuid(string guid) {
        if (guid == null) {
            return null;
        }

        if (itemDict.TryGetValue(guid, out MotionItemBase itemBase)) {
            return itemBase as MotionItem;
        }

        return null;
    }

    public MotionItem GetMotionByName(string name) {
        if (name == null) {
            return null;
        }

        foreach (KeyValuePair<string, MotionItemBase> itemBase in itemDict) {
            if (itemBase.Value.Type == MotionItemType.Motion) {
                if (itemBase.Value.Name == name) {
                    return itemBase.Value as MotionItem;
                }
            }
        }

        return null;
    }

    public string GetNewName(MotionItemType type) {
        return $"New {type.ToString()}";
    }
}