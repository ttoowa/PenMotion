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


		public PMFile() {
			itemDict = new Dictionary<string, PMItemBase>();
			rootFolder = new PMFolder(this);
		}

		public void Save(string filePath) {
			JObject jRoot = new JObject();
			jRoot.Add("Version", SystemInfo.Version);

			//MotionTree
			JObject jMotionTree = new JObject();
			jRoot.Add("MotionTree", jMotionTree);
			AddChildRecursive(jMotionTree, rootFolder);

			void AddChildRecursive(JObject jParent, PMFolder parent) {
				for (int i=0; i< parent.childList.Count; ++i) {
					PMItemBase child = parent.childList[i];
					JObject jChild = new JObject();
					jParent.Add(child.name, jChild);
					jChild.Add("Type", child.type.ToString());

					switch(child.type) {
						case PMItemType.Motion:
							SaveMotion(jChild, (PMMotion)child);
							break;
						case PMItemType.RootFolder:
						case PMItemType.Folder:
							AddChildRecursive(jChild, (PMFolder)child);
							break;
					}
				}
			}
			void SaveMotion(JObject jChild, PMMotion motion) {
				JArray jMotion = new JArray();
				jChild.Add("Data", jMotion);

				for (int pointI = 0; pointI < motion.pointList.Count; ++pointI) {
					JArray jPoint = new JArray();
					jMotion.Add(jPoint);

					PMPoint point = motion.pointList[pointI];
					jPoint.Add(point.mainPoint.ToString());
					jPoint.Add(point.subPoints[0].ToString());
					jPoint.Add(point.subPoints[1].ToString());
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
			PMFile file = new PMFile();
			file.filePath = filePath;

			string jsonString;
			using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
				using (StreamReader reader = new StreamReader(fileStream, Encoding.UTF8)) {
					jsonString = reader.ReadToEnd();
				}
			}

			JObject jRoot = JObject.Parse(jsonString);

			//MotionTree
			JObject jMotionTree = jRoot["MotionTree"] as JObject;
			LoadItemRecursive(jMotionTree, file.rootFolder);

			void LoadItemRecursive(JToken jParent, PMFolder parent) {
				foreach(JToken jChildPropToken in jParent.Children()) {
					JProperty jChildProp = jChildPropToken as JProperty;
					JObject jChild = jChildProp.Value as JObject;
					string childName = jChildProp.Name;
					switch((PMItemType)Enum.Parse(typeof(PMItemType), jChild["Type"].ToString())) {
						case PMItemType.Motion:
							LoadMotion(parent, jChild, childName);
							break;
						case PMItemType.RootFolder:
						case PMItemType.Folder:
							LoadFolder(parent, jParent, jChild, childName);
							break;
					}
				}
			}
			void LoadMotion(PMFolder parent, JToken jChild, string name) {
				JArray jMotion = jChild["Data"] as JArray;

				PMMotion motion = new PMMotion(file);
				motion.name = name;
				for (int pointI = 0; pointI < jMotion.Count; ++pointI) {
					JArray jPoint = jMotion[pointI] as JArray;

					PMPoint point = new PMPoint(
						PVector2.Parse(jPoint[0].ToObject<string>()),
						PVector2.Parse(jPoint[1].ToObject<string>()),
						PVector2.Parse(jPoint[2].ToObject<string>()));
					motion.pointList.Add(point);
				}
				parent.childList.Add(motion);


				file.itemDict.Add(name, motion);
			}
			void LoadFolder(PMFolder parent, JToken jParent, JToken jFolder, string name) {
				PMFolder folder = new PMFolder(file);
				folder.name = name;
				parent.childList.Add(folder);
				LoadItemRecursive(jParent, folder);

				file.itemDict.Add(name, folder);
			}

			return file;
		}

		public float GetMotionValue(string motionID, float linearValue, int maxSample = PMMotion.DefaultMaxSample, float tolerance = PMMotion.DefaultMaxTolerance) {
			if (motionID == null) {
				throw new Exception("motionID is Null.");
			}
			if (!itemDict.ContainsKey(motionID)) {
				throw new Exception("Not exist motion.");
			}

			if (CheckAvailableKey(motionID)) {
				PMMotion data = itemDict[motionID] as PMMotion;
				return data.GetMotionValue(linearValue, maxSample, tolerance);
			} else {
				throw new KeyNotFoundException();
			}
		}
		public PVector2 GetMotionValue(string motionID, PVector2 linearValue, int maxSample = PMMotion.DefaultMaxSample, float tolerance = PMMotion.DefaultMaxTolerance) {
			if (motionID == null) {
				throw new Exception("motionID is Null.");
			}
			if (!itemDict.ContainsKey(motionID)) {
				throw new Exception("Not exist motion.");
			}

			if (CheckAvailableKey(motionID)) {
				PMMotion data = itemDict[motionID] as PMMotion;
				return new PVector2(
					data.GetMotionValue(linearValue.x, maxSample, tolerance),
					data.GetMotionValue(linearValue.y, maxSample, tolerance)
				);
			} else {
				throw new KeyNotFoundException();
			}
		}
		public PVector3 GetMotionValue(string motionID, PVector3 linearValue, int maxSample = PMMotion.DefaultMaxSample, float tolerance = PMMotion.DefaultMaxTolerance) {
			if (motionID == null) {
				throw new Exception("motionID is Null.");
			}
			if (!itemDict.ContainsKey(motionID)) {
				throw new Exception("Not exist motion.");
			}
			if (CheckAvailableKey(motionID)) {
				PMMotion data = itemDict[motionID] as PMMotion;
				return new PVector3(
					data.GetMotionValue(linearValue.x, maxSample, tolerance),
					data.GetMotionValue(linearValue.y, maxSample, tolerance),
					data.GetMotionValue(linearValue.z, maxSample, tolerance)
				);
			} else {
				throw new KeyNotFoundException();
			}
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

			return folder;
		}
		public void RemoveItem(PMItemBase item) {
			item.parent.childList.Remove(item);
			if(item.type == PMItemType.Motion) {
				itemDict.Remove(item.name);
			}
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
		private bool CheckAvailableKey(string key) {
			return itemDict.ContainsKey(key) && itemDict[key].type == PMItemType.Motion;
		}
	}
}
