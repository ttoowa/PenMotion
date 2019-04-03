using System;
using PendulumMotion.Component;
using PendulumMotion.Component.System;

namespace PendulumMotion {
	public static class PMotionQuery {
		public static float ToMotionValue(string motionID, float value) {
			value = PMath.Clamp01(value);
			return value;
		}
		public static Vector2 ToMotionValue(string motionID, Vector2 value) {
			value.x = PMath.Clamp01(value.x);
			value.y = PMath.Clamp01(value.y);
			return value;
		}
		public static Vector3 ToMotionValue(string motionID, Vector3 value) {
			value.x = PMath.Clamp01(value.x);
			value.y = PMath.Clamp01(value.y);
			value.z = PMath.Clamp01(value.z);
			return value;
		}
		public static Vector2 TestFunc(Vector2 src) {
			return src.Normalized;
		}
	}
}
