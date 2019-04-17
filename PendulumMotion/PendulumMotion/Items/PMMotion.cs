using System;
using System.Collections;
using System.Collections.Generic;
using PendulumMotion.System;

namespace PendulumMotion.Items {
	public class PMMotion : PMItemBase
	{
		public const int DefaultMaxSample = 12;
		public const float DefaultMaxTolerance = 0.0001f;

		public List<PMPoint> pointList;

		internal PMMotion() : base(PMItemType.Motion) {
			pointList = new List<PMPoint>();
		}
		public static PMMotion Default {
			get {
				PMMotion data = new PMMotion();
				data.AddPoint(new PMPoint(new PVector2(0f, 0f)));
				data.AddPoint(new PMPoint(new PVector2(1f, 1f)));
				return data;
			}
		}

		public float GetMotionValue(float linearValue, int maxSample = DefaultMaxSample, float tolerance = DefaultMaxTolerance) {
			int rightIndex = -1;

			for (int i = 1; i < pointList.Count; ++i) {
				if (pointList[i].mainPoint.x >= linearValue) {
					rightIndex = i;
					break;
				}
			}
			if (rightIndex == -1) {
				if (pointList.Count > 0) {
					//마지막 포인트를 벗어나면 마지막 좌표 반환
					return pointList[pointList.Count - 1].mainPoint.y;
				} else {
					//포인트가 하나이거나 없을 때 1 반환
					return 1;
				}
			}

			PMPoint left = pointList[rightIndex - 1];
			PMPoint right = pointList[rightIndex];

			return PSpline.Bezier3_X2Y(linearValue, left.mainPoint, left.GetAbsoluteSubPoint(1), right.GetAbsoluteSubPoint(0), right.mainPoint, maxSample, tolerance);
		}
		public PMPoint AddPoint() {
			PMPoint point = new PMPoint(new PVector2(0f, 0f));
			pointList.Add(point);
			return point;
		}
		public PMPoint AddPoint(PMPoint point) {
			pointList.Add(point);
			return point;
		}
		public PMPoint InsertPoint(PMPoint point, int index) {
			pointList.Insert(index, point);
			return point;
		}
		public bool RemovePoint(PMPoint point) {
			return pointList.Remove(point);
		}
	}
}
