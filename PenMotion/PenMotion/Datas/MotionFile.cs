using Newtonsoft.Json.Linq;
using PenMotion.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft;
using Microsoft.Win32;
using PenMotion.Datas.Items;
using PenMotion.Datas.Items.Elements;

namespace PenMotion.Datas {
	public class MotionFile
	{
		public bool IsFilePathAvailable =>!string.IsNullOrEmpty(filePath);
		public string filePath;

		public Dictionary<string, MotionItemBase> itemDict;

		public MotionFolderItem rootFolder;

		public delegate void MotionItemDelegate(MotionItemBase item, MotionFolderItem parentFolder);
		public event MotionItemDelegate ItemCreated;
		public event MotionItemDelegate ItemRemoved;

		public MotionFile(bool createRootFolder = true) {
			itemDict = new Dictionary<string, MotionItemBase>();

			if(createRootFolder) {
				rootFolder = new MotionFolderItem(this);
			}
		}

		public void Save(string filename) {
			JObject jFile = ToJObject();

			using(FileStream fileStream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite)) {
				using (StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8)) {
					writer.Write(jFile.ToString());
				}
			}
		}
		public void Load(string filename) {
			this.filePath = filename;

			string jsonString;
			using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read)) {
				using (StreamReader reader = new StreamReader(fileStream, Encoding.UTF8)) {
					jsonString = reader.ReadToEnd();
				}
			}

			JObject jRoot = JObject.Parse(jsonString);

			LoadFromJson(jRoot);
		}
		public void LoadFromJson(JObject jRoot) {
			//MotionTree
			JObject jRootFolder = jRoot["RootFolder"] as JObject;
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
				JObject jData = jItem["Data"] as JObject;
				JArray jPoints = jData["Point"] as JArray;

				MotionItem motion = CreateMotionEmpty(parent);
				motion.SetName(name);

				foreach (JToken jPointToken in jPoints) {
					JArray jPoint = jPointToken as JArray;

					MotionPoint point = new MotionPoint(
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

				JObject jItems = jItem["Items"] as JObject;
				foreach (JToken jChild in jItems.Children()) {
					JProperty jChildProp = jChild as JProperty;
					LoadItemRecursive(jChildProp.Value, folder);
				}
			}
		}

		public float GetMotionValue(string motionId, float linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
			MotionItem motion = GetMotion(motionId);
			return motion.GetMotionValue(linearValue, maxSample, tolerance);
		}
		public PVector2 GetMotionValue(string motionId, PVector2 linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
			MotionItem motion = GetMotion(motionId);
			return new PVector2(
					motion.GetMotionValue(linearValue.x, maxSample, tolerance),
					motion.GetMotionValue(linearValue.y, maxSample, tolerance)
				);
		}
		public PVector3 GetMotionValue(string motionId, PVector3 linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
			MotionItem motion = GetMotion(motionId);
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
		public MotionItem CreateMotionEmpty(MotionFolderItem parentFolder = null, string name = null) {
			if (parentFolder == null)
				parentFolder = rootFolder;
			if(string.IsNullOrEmpty(name))
				name = GetNewName(MotionItemType.Motion);

			MotionItem motion = new MotionItem(this);

			ItemCreated?.Invoke(motion, parentFolder);

			parentFolder.AddChild(motion);
			motion.SetName(name);

			return motion;
		}
		public MotionFolderItem CreateFolder(MotionFolderItem parentFolder = null, string name = null) {
			if (parentFolder == null)
				parentFolder = rootFolder;
			if (string.IsNullOrEmpty(name))
				name = GetNewName(MotionItemType.Folder);

			MotionFolderItem folder = new MotionFolderItem(this);

			ItemCreated?.Invoke(folder, parentFolder);

			if(parentFolder != null) {
				parentFolder.AddChild(folder);
			}
			folder.SetName(name);

			return folder;
		}
		public void RemoveItem(MotionItemBase item) {
			if(item.Type == MotionItemType.Folder) {
				//메세지 띄우기

				MotionFolderItem folderItem = (MotionFolderItem)item;
				foreach(MotionItemBase childItem in folderItem.childList.ToList()) {
					RemoveItem(childItem);
				}
			}

			MotionFolderItem parentFolder = item.Parent;

			item.Parent.childList.Remove(item);
			itemDict.Remove(item.Name);

			ItemRemoved?.Invoke(item, parentFolder);
		}

		public MotionItem GetMotion(string motionId) {
			if (motionId == null) {
				throw new Exception("motionId is Null.");
			}
			if (!itemDict.ContainsKey(motionId)) {
				throw new KeyNotFoundException("Not exist motion.");
			}
			MotionItemBase item = itemDict[motionId];
			if (item.Type != MotionItemType.Motion) {
				throw new TypeLoadException("Item isn't Motion type.");
			}
			return item as MotionItem;
		}
		public string GetNewName(MotionItemType type) {
			string nameBase = $"New {type.ToString()} ";
			int num = 1;
			for(; ;) {
				if(itemDict.ContainsKey(nameBase + num)) {
					++num;
					continue;
				} else {
					return nameBase + num;
				}
			}
		}

		public JObject ToJObject() {
			JObject jFile = new JObject();
			jFile.Add("Version", SystemInfo.Version);

			AddItemRecursive(jFile, rootFolder);

			void AddItemRecursive(JObject jParent, MotionItemBase item) {
				JObject jItem = new JObject();
				jParent.Add(item.IsRoot ? "RootFolder" : item.Name, jItem);
				jItem.Add("Type", item.Type.ToString());

				if (item.Type == MotionItemType.Motion) {
					JObject jData = new JObject();
					jItem.Add("Data", jData);

					SaveMotion(jData, item as MotionItem);
				} else {
					JObject jItems = new JObject();
					jItem.Add("Items", jItems);

					SaveFolder(jItems, item as MotionFolderItem);
				}
			}
			void SaveMotion(JObject jData, MotionItem motion) {
				JArray jPoints = new JArray();
				jData.Add("Point", jPoints);

				for (int pointI = 0; pointI < motion.pointList.Count; ++pointI) {
					JArray jPoint = new JArray();
					jPoints.Add(jPoint);

					MotionPoint point = motion.pointList[pointI];
					jPoint.Add(point.MainPoint.ToString());
					jPoint.Add(point.SubPoints[0].ToString());
					jPoint.Add(point.SubPoints[1].ToString());
				}
			}
			void SaveFolder(JObject jData, MotionFolderItem folder) {
				for (int i = 0; i < folder.childList.Count; ++i) {
					MotionItemBase childItem = folder.childList[i];
					AddItemRecursive(jData, childItem);
				}
			}
			return jFile;
		}
	}
}
