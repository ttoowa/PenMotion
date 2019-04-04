using System;
using System.Collections;
using System.Collections.Generic;

namespace PendulumMotion.Component
{
	public class PMotionData
	{
		public List<PMotionPoint> pointList;

		public PMotionData() {
			pointList = new List<PMotionPoint>();
		}
		public static PMotionData Default() {
			PMotionData data = new PMotionData();
			data.pointList.Add(new PMotionPoint(new PVector2(0f, 0f)));
			data.pointList.Add(new PMotionPoint(new PVector2(1f, 1f)));
			return data;
		}
	}
}
