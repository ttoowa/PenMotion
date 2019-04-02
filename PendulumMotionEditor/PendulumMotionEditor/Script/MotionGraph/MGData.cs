using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SweepMotionEditor
{
	public class MGData
	{
		private const int MinHandleCount = 2;

		private List<MGPoint> handleList;

		public MGData() {
			handleList = new List<MGPoint>();

			for (int i = 0; i < MinHandleCount; ++i) {
				handleList.Add(new MGPoint());
			}
		}
		public float GetMotionTime(float time) {
			return time;
		}
	}
}
