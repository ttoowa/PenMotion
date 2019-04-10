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

namespace PendulumMotion.Component {
	public class PMotionFile
	{
		public string filePath;
		public bool IsFilePathAvailable =>string.IsNullOrEmpty(filePath);
		public Dictionary<string, PMotionData> dataDict;

		public PMotionFile() {
			dataDict = new Dictionary<string, PMotionData>();
		}

		public void Save(string filePath) {
			JObject jRoot = new JObject();
			jRoot.Add("Version", SystemInfo.Version);

			JObject jDatas = new JObject();
			jRoot.Add("Datas", jDatas);
			foreach(var dataPair in dataDict) {
				PMotionData data = dataPair.Value;
				JArray jMotion = new JArray();
				jDatas.Add(dataPair.Key, jMotion);

				for(int pointI = 0; pointI < data.pointList.Count; ++pointI) {
					JArray jPoint = new JArray();
					jMotion.Add(jPoint);

					PMotionPoint point = data.pointList[pointI];
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
		public static PMotionFile Load(string filePath) {
			PMotionFile file = new PMotionFile();
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

				PMotionData data = new PMotionData();
				for (int pointI = 0; pointI < jPoints.Count; ++pointI) {
					JArray jPoint = jPoints[pointI] as JArray;

					PMotionPoint point = new PMotionPoint(
						PVector2.Parse(jPoint[0].ToObject<string>()),
						PVector2.Parse(jPoint[1].ToObject<string>()),
						PVector2.Parse(jPoint[2].ToObject<string>()));
					data.pointList.Add(point);
				}
				file.dataDict.Add(jMotion.Name, data);
			}

			return file;
		}

		//public static bool RegistDeltaMotion(byte[] jsonBytes, string key) {
		//	string jsonString;
		//	using (MemoryStream memoryStream = new MemoryStream(jsonBytes)) {
		//		using (StreamReader reader = new StreamReader(memoryStream, Encoding.UTF8)) {
		//			jsonString = reader.ReadToEnd();
		//		}
		//	}
		//	return RegistDeltaMotion(jsonString, key);
		//}
		//public static bool RegistDeltaMotion(string jsonString, string key) {
		//	Queue<GPoint> pointQueue = new Queue<GPoint>();

		//	try {
		//		JObject jRoot = JObject.Parse(jsonString);

		//		JArray jPoints = jRoot.SafeGetValue<JArray>("Points", null);
		//		if (jPoints.Count <= 1) {
		//			throw new Exception("모션 데이터가 손상되었습니다.");
		//		}
		//		for (int i = 0; i < jPoints.Count; ++i) {
		//			JObject jPoint = jPoints[i] as JObject;

		//			GPoint point = new GPoint(new Vector2[] {
		//					jPoint.SafeGetValue<string>("p0", null).ToVector2(),
		//					jPoint.SafeGetValue<string>("p1", null).ToVector2(),
		//					jPoint.SafeGetValue<string>("p2", null).ToVector2(),
		//				});

		//			pointQueue.Enqueue(point);
		//		}


		//		if (deltaDataDict.ContainsKey(key)) {
		//			throw new Exception("이미 같은 이름의 모션 키가 등록되어 있습니다.");
		//		}
		//		deltaDataDict.Add(key, pointQueue.ToArray());

		//		return true;
		//	} catch (Exception ex) {
		//		("모션 데이터를 파싱하는 도중 오류가 발생했습니다. " + ex.ToString()).Log(GLogLevel.Error);
		//		return false;
		//	}
		//}
		public float GetMotionValue(string motionID, float linearValue, int maxSample = PMotionData.DefaultMaxSample, float tolerance = PMotionData.DefaultMaxTolerance) {
			if (motionID == null) {
				throw new Exception("motionID is Null.");
			}
			if (!dataDict.ContainsKey(motionID)) {
				throw new Exception("Not exist motion.");
			}

			PMotionData data = dataDict[motionID];
			return data.GetMotionValue(linearValue, maxSample, tolerance);
		}
		public PVector2 GetMotionValue(string motionID, PVector2 linearValue, int maxSample = PMotionData.DefaultMaxSample, float tolerance = PMotionData.DefaultMaxTolerance) {
			if (motionID == null) {
				throw new Exception("motionID is Null.");
			}
			if (!dataDict.ContainsKey(motionID)) {
				throw new Exception("Not exist motion.");
			}

			PMotionData data = dataDict[motionID];
			return new PVector2(
				data.GetMotionValue(linearValue.x, maxSample, tolerance),
				data.GetMotionValue(linearValue.y, maxSample, tolerance)
			);
		}
		public PVector3 GetMotionValue(string motionID, PVector3 linearValue, int maxSample = PMotionData.DefaultMaxSample, float tolerance = PMotionData.DefaultMaxTolerance) {
			if (motionID == null) {
				throw new Exception("motionID is Null.");
			}
			if (!dataDict.ContainsKey(motionID)) {
				throw new Exception("Not exist motion.");
			}

			PMotionData data = dataDict[motionID];
			return new PVector3(
				data.GetMotionValue(linearValue.x, maxSample, tolerance),
				data.GetMotionValue(linearValue.y, maxSample, tolerance),
				data.GetMotionValue(linearValue.z, maxSample, tolerance)
			);
		}
	}
}
