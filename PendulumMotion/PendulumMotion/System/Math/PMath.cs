using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PendulumMotion.Component;

namespace PendulumMotion.System {
	public static class PMath {
		public static float Clamp(float value, float min = 0f, float max = 1f) {
			return Math.Min(Math.Max(value, min), max);
		}
		public static float Clamp01(float value) {
			return Clamp(value);
		}
		public static PVector2 Clamp(PVector2 value, float min = 0f, float max = 1f) {
			return new PVector2(Clamp(value.x, min, max), Clamp(value.y, min, max));
		}
		public static PVector2 Clamp01(PVector2 value) {
			return Clamp(value);
		}
		public static PVector3 Clamp(PVector3 value, float min = 0f, float max = 1f) {
			return new PVector3(Clamp(value.x, min, max), Clamp(value.y, min, max), Clamp(value.z, min, max));
		}
		public static PVector3 Clamp01(PVector3 value) {
			return Clamp(value);
		}
	}
}
