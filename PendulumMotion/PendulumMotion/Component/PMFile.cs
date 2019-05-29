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
using PendulumMotion.Items;

namespace PendulumMotion.Component {
	public class PMFile
	{
		public string filePath;
		public bool IsFilePathAvailable =>!string.IsNullOrEmpty(filePath);
		public Dictionary<string, PMItemBase> itemDict;
		public PMFolder rootFolder;


		public PMFile(bool createRootFolder = true) {
			itemDict = new Dictionary<string, PMItemBase>();
			if(createRootFolder) {
				rootFolder = new PMFolder(this);
			}
		}

		public void Save(string filePath) {
			JObject jRoot = new JObject();
			jRoot.Add("Version", SystemInfo.Version);

			AddItemRecursive(jRoot, rootFolder);

			void AddItemRecursive(JObject jParent, PMItemBase item) {
				JObject jItem = new JObject();
				jParent.Add(item.IsRoot ? "RootFolder" : item.name, jItem);
				jItem.Add("Type", item.type.ToString());
				JObject jData = new JObject();
				jItem.Add("Data", jData);

				if (item.type == PMItemType.Motion) {
					SaveMotion(jData, item as PMMotion);
				} else {
					SaveFolder(jData, item as PMFolder);
				}
			}
			void SaveMotion(JObject jData, PMMotion motion) {
				JArray jPoints = new JArray();
				jData.Add("Point", jPoints);
				
				for (int pointI = 0; pointI < motion.pointList.Count; ++pointI) {
					JArray jPoint = new JArray();
					jPoints.Add(jPoint);

					PMPoint point = motion.pointList[pointI];
					jPoint.Add(point.mainPoint.ToString());
					jPoint.Add(point.subPoints[0].ToString());
					jPoint.Add(point.subPoints[1].ToString());
				}
			}
			void SaveFolder(JObject jData, PMFolder folder) {
				for (int i = 0; i < folder.childList.Count; ++i) {
					PMItemBase childItem = folder.childList[i];
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
		public static PMFile Load(string filePath) {
			PMFile file = new PMFile(false);
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

			void LoadItemRecursive(JToken jItem, PMFolder parent) {
				JToken jType = jItem["Type"];
				string typeText = jType != null ? jType.ToString() : null;
				string name = (jItem.Parent as JProperty).Name;

				if (typeText == "Motion") {
					LoadMotion(parent, jItem, name);
				} else {
					LoadFolder(parent, jItem, name);
				}
			}
			void LoadMotion(PMFolder parent, JToken jItem, string name) {
				JObject jData = jItem["Data"] as JObject;
				JArray jPoints = jData["Point"] as JArray;

				PMMotion motion = new PMMotion(file);
				motion.parent = parent;
				motion.name = name;
				foreach (JToken jPointToken in jPoints) {
					JArray jPoint = jPointToken as JArray;

					PMPoint point = new PMPoint(
						PVector2.Parse(jPoint[0].ToObject<string>()),
						PVector2.Parse(jPoint[1].ToObject<string>()),
						PVector2.Parse(jPoint[2].ToObject<string>()));
					motion.pointList.Add(point);
				}
				parent.childList.Add(motion);

				file.itemDict.Add(name, motion);
			}
			void LoadFolder(PMFolder parent, JToken jItem, string name) {
				PMFolder folder = new PMFolder(file);
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

		public float GetMotionValue(string motionId, float linearValue, int maxSample = PMMotion.DefaultMaxSample, float tolerance = PMMotion.DefaultMaxTolerance) {
			PMMotion motion = GetMotion(motionId);
			return motion.GetMotionValue(linearValue, maxSample, tolerance);
		}
		public PVector2 GetMotionValue(string motionId, PVector2 linearValue, int maxSample = PMMotion.DefaultMaxSample, float tolerance = PMMotion.DefaultMaxTolerance) {
			PMMotion motion = GetMotion(motionId);
			return new PVector2(
					motion.GetMotionValue(linearValue.x, maxSample, tolerance),
					motion.GetMotionValue(linearValue.y, maxSample, tolerance)
				);
		}
		public PVector3 GetMotionValue(string motionId, PVector3 linearValue, int maxSample = PMMotion.DefaultMaxSample, float tolerance = PMMotion.DefaultMaxTolerance) {
			PMMotion motion = GetMotion(motionId);
			return new PVector3(
					motion.GetMotionValue(linearValue.x, maxSample, tolerance),
					motion.GetMotionValue(linearValue.y, maxSample, tolerance),
					motion.GetMotionValue(linearValue.z, maxSample, tolerance)
				);
		}

		public PMMotion CreateMotionDefault(PMFolder parentFolder = null) {
			if (parentFolder == null)
				parentFolder = rootFolder;

			PMMotion motion = PMMotion.CreateDefault(this);
			motion.parent = parentFolder;
			motion.name = GetNewName(PMItemType.Motion);
			parentFolder.childList.Add(motion);
			itemDict.Add(motion.name, motion);

			return motion;
		}
		public PMMotion CreateMotionEmpty(PMFolder parentFolder = null) {
			if (parentFolder == null)
				parentFolder = rootFolder;

			PMMotion motion = new PMMotion(this);
			motion.parent = parentFolder;
			motion.name = GetNewName(PMItemType.Motion);
			parentFolder.childList.Add(motion);
			itemDict.Add(motion.name, motion);

			return motion;
		}
		public PMFolder CreateFolder(PMFolder parentFolder = null) {
			if (parentFolder == null)
				parentFolder = rootFolder;

			PMFolder folder = new PMFolder(this);
			folder.parent = parentFolder;
			folder.name = GetNewName(PMItemType.Folder);
			parentFolder.childList.Add(folder);
			itemDict.Add(folder.name, folder);

			return folder;
		}
		public void RemoveItem(PMItemBase item) {
			item.parent.childList.Remove(item);
			itemDict.Remove(item.name);
		}

		public PMMotion GetMotion(string motionId) {
			if (motionId == null) {
				throw new Exception("motionId is Null.");
			}
			if (!itemDict.ContainsKey(motionId)) {
				throw new KeyNotFoundException("Not exist motion.");
			}
			PMItemBase item = itemDict[motionId];
			if (item.type != PMItemType.Motion) {
				throw new TypeLoadException("Item isn't Motion type.");
			}
			return item as PMMotion;
		}
		public string GetNewName(PMItemType type) {
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
