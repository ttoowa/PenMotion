using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PendulumMotion;
using PendulumMotion.Component;
using PendulumMotion.System;
using GKit;

namespace PendulumMotionTest {
	class Program {
		static void Main(string[] args) {
			float x = 0.5f;
			PVector2 point = new PVector2(0.5f, 0.5f);
			int maxSample = 10;
			float tolerance = 0.0001f;

			//C# vs C++
			Console.WriteLine(GProfiler.ProfileFunction(100, true,
			() => {
				PendulumMotion.System.PSpline.Bezier3_X2Y(0.5f, point, point, point, point, maxSample, tolerance);
			},
			() => {

			}).ToString());

			const string DummyFilePath = "X:/DummyMotion.pmotion";
			PMotionFile file = new PMotionFile();
			PMotionData dummyData = PMotionData.Default();
			file.dataDict.Add("KeyA", dummyData);
			file.dataDict.Add("KeyB", dummyData);
			file.Save(DummyFilePath);

			file = new PMotionFile(DummyFilePath);



			Console.ReadLine();
		}
	}
}
