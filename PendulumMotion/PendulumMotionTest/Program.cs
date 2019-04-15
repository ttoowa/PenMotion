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
using PendulumMotion.Items;
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
			PMFile file = new PMFile();
			PMMotion dummyData = PMMotion.Default;
			file.motionDict.Add("KeyA", dummyData);
			file.motionDict.Add("KeyB", dummyData);
			file.Save(DummyFilePath);

			//file = new PMotionFile(DummyFilePath);



			Console.ReadLine();
		}
	}
}
