using System;
using System.Collections.Generic;
using System.Text;

namespace PendulumMotion.Component
{
	public class PMotionPoint
	{
		public const float DefaultSubPointOffset = 0.3f;
		public Vector2 mainPoint;
		public Vector2[] subPoints;

		public PMotionPoint() {
			subPoints = new Vector2[] {
				new Vector2(-DefaultSubPointOffset, 0f),
				new Vector2(DefaultSubPointOffset, 0f),
			};
		}
		public PMotionPoint(Vector2 mainPoint) {
			this.mainPoint = mainPoint;new Vector2(1f, 1f);
			subPoints = new Vector2[] {
				new Vector2(-DefaultSubPointOffset, 0f),
				new Vector2(DefaultSubPointOffset, 0f),
			};
		}
		public PMotionPoint(Vector2 mainPoint, Vector2 subPointLeft, Vector2 subPointRight)
		{
			this.mainPoint = mainPoint;
			this.subPoints = new Vector2[] {
				subPointLeft,
				subPointRight,
			};
		}
	}
}
