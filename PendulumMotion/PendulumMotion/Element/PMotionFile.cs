using Newtonsoft.Json.Linq;
using PendulumMotion.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PendulumMotion.Component {
	public class PMotionFile
	{
		public Dictionary<string, PMotionData> dataDict;

		public PMotionFile() {
			dataDict = new Dictionary<string, PMotionData>();
		}
		public PMotionFile(string filePath) : this() {
			Load(filePath);
		}

		public void Save(string filePath) {
			JObject jRoot = new JObject();
			jRoot.Add("Version", VersionInfo.Version);

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
		public void Load(string filePath) {
			dataDict.Clear();

			string jsonString;
			using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
				using (StreamReader reader = new StreamReader(fileStream, Encoding.UTF8)) {
					jsonString = reader.ReadToEnd();
				}
			}

			JObject jRoot = JObject.Parse(jsonString);

			JObject jDatas = jRoot["Datas"] as JObject;
			List<JProperty> jDataList = jDatas.Properties().ToList();
			for(int dataI =0; dataI < jDataList.Count; ++dataI) {
				JProperty jMotion = jDataList[dataI];
				JArray jPoints = jMotion.Value as JArray;

				PMotionData data = new PMotionData();
				for(int pointI = 0; pointI < jPoints.Count; ++pointI) {
					JArray jPoint = jPoints[pointI] as JArray;

					PMotionPoint point = new PMotionPoint(
						PVector2.Parse(jPoint[0].ToObject<string>()), 
						PVector2.Parse(jPoint[1].ToObject<string>()), 
						PVector2.Parse(jPoint[2].ToObject<string>()));
					data.pointList.Add(point);
				}
				dataDict.Add(jMotion.Name, data);
			}
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
		public float GetMotionValue(string motionID, float linearValue, int maxSample, float tolerance) {
			if (motionID == null) {
				throw new Exception("motionID is Null.");
			}
			if (!dataDict.ContainsKey(motionID)) {
				throw new Exception("Not exist motion.");
			}

			PMotionData data = dataDict[motionID];
			return GetMotionValue(data, linearValue, maxSample, tolerance);
		}
		public PVector2 GetMotionValue(string motionID, PVector2 linearValue, int maxSample, float tolerance) {
			if (motionID == null) {
				throw new Exception("motionID is Null.");
			}
			if (!dataDict.ContainsKey(motionID)) {
				throw new Exception("Not exist motion.");
			}

			PMotionData data = dataDict[motionID];
			return new PVector2(
				GetMotionValue(data, linearValue.x, maxSample, tolerance),
				GetMotionValue(data, linearValue.y, maxSample, tolerance)
			);
		}
		public PVector3 GetMotionValue(string motionID, PVector3 linearValue, int maxSample, float tolerance) {
			if (motionID == null) {
				throw new Exception("motionID is Null.");
			}
			if (!dataDict.ContainsKey(motionID)) {
				throw new Exception("Not exist motion.");
			}

			PMotionData data = dataDict[motionID];
			return new PVector3(
				GetMotionValue(data, linearValue.x, maxSample, tolerance),
				GetMotionValue(data, linearValue.y, maxSample, tolerance),
				GetMotionValue(data, linearValue.z, maxSample, tolerance)
			);
		}
		private float GetMotionValue(PMotionData data, float linearValue, int maxSample, float tolerance) {
			int rightIndex = -1;

			for (int i = 1; i < data.pointList.Count; ++i) {
				if (data.pointList[i].mainPoint.x >= linearValue) {
					rightIndex = i;
					break;
				}
			}
			if (rightIndex == -1) {
				if (data.pointList.Count > 0) {
					//마지막 포인트를 벗어나면 마지막 좌표 반환
					return data.pointList[data.pointList.Count - 1].mainPoint.y;
				} else {
					//포인트가 하나이거나 없을 때 1 반환
					return 1;
				}
			}

			PMotionPoint left = data.pointList[rightIndex - 1];
			PMotionPoint right = data.pointList[rightIndex];

			return PSpline.Bezier3_X2Y(linearValue, left.mainPoint, left.subPoints[1], right.subPoints[0], right.mainPoint, maxSample, tolerance);
		}
	}
}
