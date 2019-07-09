using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using PendulumMotion.System;
using PendulumMotion.Components.Items.Elements;

namespace PendulumMotion.Components.Items {
	public class MotionItem : MotionItemBase
	{
		public const int DefaultMaxSample = 12;
		public const float DefaultMaxTolerance = 0.0001f;

		public List<MotionPoint> pointList;

		public static MotionItem CreateDefault(MotionFile ownerFile) {
			MotionItem data = new MotionItem(ownerFile);
			data.AddPoint(new MotionPoint(new PVector2(0f, 0f)));
			data.AddPoint(new MotionPoint(new PVector2(1f, 1f)));
			return data;
		}
		internal MotionItem(MotionFile ownerFile) : base(ownerFile, MotionItemType.Motion) {
			pointList = new List<MotionPoint>();
		}

		public float GetMotionValue(float linearValue, int maxSample = DefaultMaxSample, float tolerance = DefaultMaxTolerance) {
			linearValue = Math.Max(Math.Min(linearValue, 1f), 0f);

			int rightIndex = GetRightPointIndex(linearValue);
			if (rightIndex == -1) {
				if (pointList.Count > 0) {
					//마지막 포인트를 벗어나면 마지막 좌표 반환
					return pointList[pointList.Count - 1].mainPoint.y;
				} else {
					//포인트가 하나이거나 없을 때 1 반환
					return 1;
				}
			}

			MotionPoint left = pointList[rightIndex - 1];
			MotionPoint right = pointList[rightIndex];

			return PSpline.Bezier3_X2Y(linearValue, left.mainPoint, left.GetAbsoluteSubPoint(1), right.GetAbsoluteSubPoint(0), right.mainPoint, maxSample, tolerance);
		}
		public int GetRightPointIndex(float linearValue) {
			int rightIndex = -1;
			for (int i = 1; i < pointList.Count; ++i) {
				if (pointList[i].mainPoint.x >= linearValue) {
					rightIndex = i;
					break;
				}
			}
			return rightIndex;
		}

		public MotionPoint AddPoint() {
			MotionPoint point = new MotionPoint(new PVector2(0f, 0f));
			pointList.Add(point);
			return point;
		}
		public MotionPoint AddPoint(MotionPoint point) {
			pointList.Add(point);
			return point;
		}
		public MotionPoint InsertPoint(int index, MotionPoint point) {
			pointList.Insert(index, point);
			return point;
		}
		public bool RemovePoint(MotionPoint point) {
			return pointList.Remove(point);
		}
	}
}
