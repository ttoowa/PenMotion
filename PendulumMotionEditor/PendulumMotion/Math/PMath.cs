using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PendulumMotion.Component;

namespace PendulumMotion.Component.System {
	public static class PMath {
		public static float Clamp(float value, float min = 0f, float max = 1f) {
			return Math.Min(Math.Max(value, min), max);
		}
		public static float Clamp01(float value) {
			return Clamp(value);
		}
		public static Vector2 Clamp(Vector2 value, float min = 0f, float max = 1f) {
			return new Vector2(Clamp(value.x, min, max), Clamp(value.y, min, max));
		}
		public static Vector2 Clamp01(Vector2 value) {
			return Clamp(value);
		}
		public static Vector3 Clamp(Vector3 value, float min = 0f, float max = 1f) {
			return new Vector3(Clamp(value.x, min, max), Clamp(value.y, min, max), Clamp(value.z, min, max));
		}
		public static Vector3 Clamp01(Vector3 value) {
			return Clamp(value);
		}
	}
}
