using System;
using System.Collections;
using System.Collections.Generic;

namespace PendulumMotion.Component
{
	public class PMotionData
	{
		public List<PMotionPoint> pointList;

		public PMotionData() {
			pointList = new List<PMotionPoint>()
			{
				new PMotionPoint(new Vector2(0f, 0f)),
				new PMotionPoint(new Vector2(1f, 1f)),
			};
		}
		//public PMotionData(data) {
		//	pointList = new List<PMotionPoint>();
		//data_Init
		//}
	}
}
