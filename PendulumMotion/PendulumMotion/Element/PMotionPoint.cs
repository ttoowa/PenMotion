using System;
using System.Collections.Generic;
using System.Text;

namespace PendulumMotion.Component
{
	public class PMotionPoint
	{
		public const float DefaultSubPointOffset = 0.3f;
		public PVector2 mainPoint;
		public PVector2[] subPoints;

		public PMotionPoint() {
			subPoints = new PVector2[] {
				new PVector2(-DefaultSubPointOffset, 0f),
				new PVector2(DefaultSubPointOffset, 0f),
			};
		}
		public PMotionPoint(PVector2 mainPoint) {
			this.mainPoint = mainPoint;new PVector2(1f, 1f);
			subPoints = new PVector2[] {
				new PVector2(-DefaultSubPointOffset, 0f),
				new PVector2(DefaultSubPointOffset, 0f),
			};
		}
		public PMotionPoint(PVector2 mainPoint, PVector2 subPointLeft, PVector2 subPointRight)
		{
			this.mainPoint = mainPoint;
			this.subPoints = new PVector2[] {
				subPointLeft,
				subPointRight,
			};
		}
	}
}
