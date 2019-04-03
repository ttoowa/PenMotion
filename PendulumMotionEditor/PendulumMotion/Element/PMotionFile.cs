using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PendulumMotion.Component
{
	public class PMotionFile
	{
		public Dictionary<string, PMotionData> dataDict;

		public PMotionFile() {
			dataDict = new Dictionary<string, PMotionData>();
		}

		public void Save(string filePath) {

		}
		public void Load(string filePath) {

		}
	}
}
