using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PendulumMotion.System {
	public struct PRect {
		public float xMin;
		public float xMax;
		public float yMin;
		public float yMax;
		public float Width => xMax - xMin;
		public float Height => yMax - yMin;


		public PRect(float xMin, float yMin, float xMax, float yMax) {
			this.xMin = xMin;
			this.yMin = yMin;
			this.xMax = xMax;
			this.yMax = yMax;
		}
	}
}
