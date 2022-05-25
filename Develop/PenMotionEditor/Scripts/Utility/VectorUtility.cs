using GKitForWPF;
using PenMotion.System;

namespace PenMotionEditor {
	public static class VectorUtlity {
		public static PVector2 ToPVector2(this Vector2 value) {
			return new PVector2(value.x, value.y);
		}
		public static Vector2 ToVector2(this PVector2 value) {
			return new Vector2(value.x, value.y);
		}
	}
}
