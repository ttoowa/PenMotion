using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PendulumMotion.Component;

namespace PendulumMotion.System {
	public static class PSpline {
		public static PVector2 Bezier3(float t, PVector2 p0, PVector2 p1, PVector2 p2, PVector2 p3) {
			float t2 = t * t;
			float t3 = t2 * t;
			float tInv = 1 - t;
			float tInv2 = tInv * tInv;
			float tInv3 = tInv2 * tInv;

			PVector2 pos = tInv3 * p0;
			pos += 3 * tInv2 * t * p1;
			pos += 3 * tInv * t2 * p2;
			pos += t3 * p3;

			return pos;
		}
		public static float Bezier3_X2Y(float x, PVector2 p0, PVector2 p1, PVector2 p2, PVector2 p3, int maxSample, float tolerance) {
			x = PMath.Clamp(x);

			float lower = 0f;
			float upper = 1f;
			float mid = (upper + lower) * 0.5f;

			PVector2 result = Bezier3(mid, p0, p1, p2, p3);
			for(int sampleI = 0; sampleI < maxSample; ++sampleI) {
				if (Math.Abs(x - result.x) <= tolerance)
					break;

				if (x > result.x) {
					lower = mid;
				} else {
					upper = mid;
				}
				mid = (upper + lower) * 0.5f;

				result = Bezier3(mid, p0, p1, p2, p3);
			}
			return result.y;
		}
	}
}
