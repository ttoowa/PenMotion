using System;
using System.Collections.Generic;
using System.Text;
using PendulumMotion.System;

namespace PendulumMotion.Items {
	public class PMPoint
	{
		public const float DefaultSubPointOffset = 0.3f;
		public PVector2 mainPoint;
		public PVector2[] subPoints;
		public object view;

		public PMPoint() {
			subPoints = new PVector2[] {
				new PVector2(-DefaultSubPointOffset, 0f),
				new PVector2(DefaultSubPointOffset, 0f),
			};
		}
		public PMPoint(PVector2 mainPoint) {
			this.mainPoint = mainPoint;new PVector2(1f, 1f);
			subPoints = new PVector2[] {
				new PVector2(-DefaultSubPointOffset, 0f),
				new PVector2(DefaultSubPointOffset, 0f),
			};
		}
		public PMPoint(PVector2 mainPoint, PVector2 subPointLeft, PVector2 subPointRight)
		{
			this.mainPoint = mainPoint;
			this.subPoints = new PVector2[] {
				subPointLeft,
				subPointRight,
			};
		}

		public PVector2 GetAbsoluteSubPoint(int index) {
			return mainPoint + subPoints[index];
		}
	}
}
