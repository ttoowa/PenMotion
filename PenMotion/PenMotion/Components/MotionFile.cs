using Newtonsoft.Json.Linq;
using PendulumMotion.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft;
using Microsoft.Win32;
using PendulumMotion.Components.Items;
using PendulumMotion.Components.Items.Elements;

namespace PendulumMotion.Components {
	public class MotionFile
	{
		public string filePath;
		public bool IsFilePathAvailable =>!string.IsNullOrEmpty(filePath);
		public Dictionary<string, MotionItemBase> itemDict;
		public MotionFolderItem rootFolder;


		public MotionFile(bool createRootFolder = true) {
			itemDict = new Dictionary<string, MotionItemBase>();
			if(createRootFolder) {
				rootFolder = new MotionFolderItem(this);
			}
		}

		public void Save(string filePath) {
			JObject jRoot = new JObject();
			jRoot.Add("Version", SystemInfo.Version);

			AddItemRecursive(jRoot, rootFolder);

			void AddItemRecursive(JObject jParent, MotionItemBase item) {
				JObject jItem = new JObject();
				jParent.Add(item.IsRoot ? "RootFolder" : item.name, jItem);
				jItem.Add("Type", item.type.ToString());
				JObject jData = new JObject();
				jItem.Add("Data", jData);

				if (item.type == MotionItemType.Motion) {
					SaveMotion(jData, item as MotionItem);
				} else {
					SaveFolder(jData, item as MotionFolderItem);
				}
			}
			void SaveMotion(JObject jData, MotionItem motion) {
				JArray jPoints = new JArray();
				jData.Add("Point", jPoints);
				
				for (int pointI = 0; pointI < motion.pointList.Count; ++pointI) {
					JArray jPoint = new JArray();
					jPoints.Add(jPoint);

					MotionPoint point = motion.pointList[pointI];
					jPoint.Add(point.mainPoint.ToString());
					jPoint.Add(point.subPoints[0].ToString());
					jPoint.Add(point.subPoints[1].ToString());
				}
			}
			void SaveFolder(JObject jData, MotionFolderItem folder) {
				for (int i = 0; i < folder.childList.Count; ++i) {
					MotionItemBase childItem = folder.childList[i];
					AddItemRecursive(jData, childItem);
				}
			}

			string jsonString = jRoot.ToString();

			using(FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite)) {
				using (StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8)) {
					writer.Write(jsonString);
				}
			}
		}
		public static MotionFile Load(string filePath) {
			MotionFile file = new MotionFile(false);
			file.filePath = filePath;

			string jsonString;
			using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
				using (StreamReader reader = new StreamReader(fileStream, Encoding.UTF8)) {
					jsonString = reader.ReadToEnd();
				}
			}

			JObject jRoot = JObject.Parse(jsonString);

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

				MotionItem motion = new MotionItem(file);
				motion.parent = parent;
				motion.name = name;
				foreach (JToken jPointToken in jPoints) {
					JArray jPoint = jPointToken as JArray;

					MotionPoint point = new MotionPoint(
						PVector2.Parse(jPoint[0].ToObject<string>()),
						PVector2.Parse(jPoint[1].ToObject<string>()),
						PVector2.Parse(jPoint[2].ToObject<string>()));
					motion.pointList.Add(point);
				}
				parent.childList.Add(motion);

				file.itemDict.Add(name, motion);
			}
			void LoadFolder(MotionFolderItem parent, JToken jItem, string name) {
				MotionFolderItem folder = new MotionFolderItem(file);
				folder.parent = parent;
				if (parent == null) {
					file.rootFolder = folder;
				} else {
					folder.name = name;
					parent.childList.Add(folder);

					file.itemDict.Add(name, folder);
				}

				JObject jData = jItem["Data"] as JObject;
				foreach(JToken jChild in jData.Children()) {
					JProperty jChildProp = jChild as JProperty;
					LoadItemRecursive(jChildProp.Value, folder);
				}
			}

			return file;
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

		public MotionItem CreateMotionDefault(MotionFolderItem parentFolder = null) {
			if (parentFolder == null)
				parentFolder = rootFolder;

			MotionItem motion = MotionItem.CreateDefault(this);
			motion.parent = parentFolder;
			motion.name = GetNewName(MotionItemType.Motion);
			parentFolder.childList.Add(motion);
			itemDict.Add(motion.name, motion);

			return motion;
		}
		public MotionItem CreateMotionEmpty(MotionFolderItem parentFolder = null) {
			if (parentFolder == null)
				parentFolder = rootFolder;

			MotionItem motion = new MotionItem(this);
			motion.parent = parentFolder;
			motion.name = GetNewName(MotionItemType.Motion);
			parentFolder.childList.Add(motion);
			itemDict.Add(motion.name, motion);

			return motion;
		}
		public MotionFolderItem CreateFolder(MotionFolderItem parentFolder = null) {
			if (parentFolder == null)
				parentFolder = rootFolder;

			MotionFolderItem folder = new MotionFolderItem(this);
			folder.parent = parentFolder;
			folder.name = GetNewName(MotionItemType.Folder);
			parentFolder.childList.Add(folder);
			itemDict.Add(folder.name, folder);

			return folder;
		}
		public void RemoveItem(MotionItemBase item) {
			item.parent.childList.Remove(item);
			itemDict.Remove(item.name);
		}

		public MotionItem GetMotion(string motionId) {
			if (motionId == null) {
				throw new Exception("motionId is Null.");
			}
			if (!itemDict.ContainsKey(motionId)) {
				throw new KeyNotFoundException("Not exist motion.");
			}
			MotionItemBase item = itemDict[motionId];
			if (item.type != MotionItemType.Motion) {
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
	}
}
