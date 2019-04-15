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
		public bool IsFilePathAvailable =>string.IsNullOrEmpty(filePath);
		public Dictionary<string, PMMotion> motionDict;
#if OnEditor
		public PMFolder rootFolder;
#endif


		public PMFile() {
			motionDict = new Dictionary<string, PMMotion>();
			rootFolder = new PMFolder();
		}

		public void Save(string filePath) {
			JObject jRoot = new JObject();
			jRoot.Add("Version", SystemInfo.Version);

			JObject jDatas = new JObject();
			jRoot.Add("Datas", jDatas);
			foreach(var dataPair in motionDict) {
				PMMotion data = dataPair.Value;
				JArray jMotion = new JArray();
				jDatas.Add(dataPair.Key, jMotion);

				for(int pointI = 0; pointI < data.pointList.Count; ++pointI) {
					JArray jPoint = new JArray();
					jMotion.Add(jPoint);

					PMPoint point = data.pointList[pointI];
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

			JObject jDatas = jRoot["Datas"] as JObject;
			List<JProperty> jDataList = jDatas.Properties().ToList();
			for (int dataI = 0; dataI < jDataList.Count; ++dataI) {
				JProperty jMotion = jDataList[dataI];
				JArray jPoints = jMotion.Value as JArray;

				PMMotion data = new PMMotion();
				for (int pointI = 0; pointI < jPoints.Count; ++pointI) {
					JArray jPoint = jPoints[pointI] as JArray;

					PMPoint point = new PMPoint(
						PVector2.Parse(jPoint[0].ToObject<string>()),
						PVector2.Parse(jPoint[1].ToObject<string>()),
						PVector2.Parse(jPoint[2].ToObject<string>()));
					data.pointList.Add(point);
				}
				file.motionDict.Add(jMotion.Name, data);
			}

			return file;
		}

		public float GetMotionValue(string motionID, float linearValue, int maxSample = PMMotion.DefaultMaxSample, float tolerance = PMMotion.DefaultMaxTolerance) {
			if (motionID == null) {
				throw new Exception("motionID is Null.");
			}
			if (!motionDict.ContainsKey(motionID)) {
				throw new Exception("Not exist motion.");
			}

			PMMotion data = motionDict[motionID];
			return data.GetMotionValue(linearValue, maxSample, tolerance);
		}
		public PVector2 GetMotionValue(string motionID, PVector2 linearValue, int maxSample = PMMotion.DefaultMaxSample, float tolerance = PMMotion.DefaultMaxTolerance) {
			if (motionID == null) {
				throw new Exception("motionID is Null.");
			}
			if (!motionDict.ContainsKey(motionID)) {
				throw new Exception("Not exist motion.");
			}

			PMMotion data = motionDict[motionID];
			return new PVector2(
				data.GetMotionValue(linearValue.x, maxSample, tolerance),
				data.GetMotionValue(linearValue.y, maxSample, tolerance)
			);
		}
		public PVector3 GetMotionValue(string motionID, PVector3 linearValue, int maxSample = PMMotion.DefaultMaxSample, float tolerance = PMMotion.DefaultMaxTolerance) {
			if (motionID == null) {
				throw new Exception("motionID is Null.");
			}
			if (!motionDict.ContainsKey(motionID)) {
				throw new Exception("Not exist motion.");
			}

			PMMotion data = motionDict[motionID];
			return new PVector3(
				data.GetMotionValue(linearValue.x, maxSample, tolerance),
				data.GetMotionValue(linearValue.y, maxSample, tolerance),
				data.GetMotionValue(linearValue.z, maxSample, tolerance)
			);
		}

		public PMMotion CreateMotion(PMFolder parentFolder = null) {
			if (parentFolder == null)
				parentFolder = rootFolder;

			PMMotion motion = PMMotion.Default;
			motion.parent = parentFolder;
			parentFolder.childList.Add(motion);
			motionDict.Add(GetNewName(PMItemType.Motion), motion);

			return motion;
		}
		public PMFolder CreateFolder(PMFolder parentFolder = null) {
			if (parentFolder == null)
				parentFolder = rootFolder;

			PMFolder folder = new PMFolder();
			folder.parent = parentFolder;
			parentFolder.childList.Add(folder);

			return folder;
		}
		public void RemoveItem(PMItemBase item) {
			item.parent.childList.Remove(item);
			if(item.type == PMItemType.Motion) {
				motionDict.Remove(item.name);
			}
		}

		private string GetNewName(PMItemType type) {
			string nameBase = $"New {type.ToString()} ";
			int num = 1;
			for(; ;) {
				if(motionDict.ContainsKey(nameBase + num)) {
					++num;
					continue;
				} else {
					return nameBase + num;
				}
			}
		}
	}
}
