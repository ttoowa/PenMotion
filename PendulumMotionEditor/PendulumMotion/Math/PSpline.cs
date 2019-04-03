using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PendulumMotion.Component;

namespace PendulumMotion.Component.System {
	public static class PSpline {
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
#if UNITY
				public static bool RegistDeltaMotionPath(string resourcePath, string key) {
					return RegistDeltaMotion(resourcePath.GetResource<TextAsset>().bytes, key);
				}
#endif
		public static Vector2 Bezier3(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3) {
			float t2 = t * t;
			float t3 = t2 * t;
			float tInv = 1 - t;
			float tInv2 = tInv * tInv;
			float tInv3 = tInv2 * tInv;

			Vector2 pos = tInv3 * p0;
			pos += 3 * tInv2 * t * p1;
			pos += 3 * tInv * t2 * p2;
			pos += t3 * p3;

			return pos;
		}
		public static float Bezier3_X2Y(float x, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int maxLoopCount = 10) {
			x = PMath.Clamp(x);
			float xTolerance = 0.001f;

			float lower = 0f;
			float upper = 1f;
			float percent = (upper + lower) * 0.5f;

			int loopCount = 0;
			Vector2 result = Bezier3(percent, p0, p1, p2, p3);
			while (Math.Abs(x - result.x) > xTolerance) {
				if (++loopCount > maxLoopCount) {
					break;
				}
				if (x > result.x) {
					lower = percent;
				} else {
					upper = percent;
				}
				percent = (upper + lower) * 0.5f;

				result = Bezier3(percent, p0, p1, p2, p3);
			}
			return result.y;
		}
		//public static float CalcDeltaMotion(string key, float x, int maxLoopCount = 10) {
		//	if (string.IsNullOrEmpty(key)) {
		//		throw new Exception("모션 키가 null입니다.");
		//	}
		//	if (!deltaDataDict.ContainsKey(key)) {
		//		throw new Exception("모션 키 " + key + " 가 등록되어 있지 않습니다.");
		//	}
		//	GPoint[] points = deltaDataDict[key];
		//	int rightIndex = -1;

		//	for (int i = 1; i < points.Length; ++i) {
		//		if (points[i].points[1].x >= x) {
		//			rightIndex = i;
		//			break;
		//		}
		//	}
		//	if (rightIndex == -1) {
		//		if (points.Length > 0) {
		//			//마지막 포인트를 벗어났을 때
		//			return points[points.Length - 1].points[1].y;
		//		} else {
		//			//포인트가 하나이거나 없을 때
		//			return 1;
		//		}
		//	}

		//	GPoint left = points[rightIndex - 1];
		//	GPoint right = points[rightIndex];

		//	return BSpline2D.Bezier3_X2Y(x, left.points[1], left.points[2], right.points[0], right.points[1], maxLoopCount);
		//}
	}
}
