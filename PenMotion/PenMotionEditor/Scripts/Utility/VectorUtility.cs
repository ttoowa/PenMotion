using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PenMotion.System;
using GKitForWPF;

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
