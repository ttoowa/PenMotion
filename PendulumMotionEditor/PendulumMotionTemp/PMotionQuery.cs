using System;

namespace PendulumMotion
{
	public static class PMotionQuery
	{
		public static float ToMotionTime(this float time) {
			return Math.Min(Math.Max(0f, time), 1f);
		}
	}
}
