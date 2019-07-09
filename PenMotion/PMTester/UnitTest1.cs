using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PendulumMotion;
using PendulumMotion.Component;
using PendulumMotion.Items;
using PendulumMotion.System;

namespace PMTester {
	[TestClass]
	public class UnitTest1 {
		[TestMethod]
		public void SaveLoadDefault() {
			int maxSample = 10;
			float tolerance = 0.0001f;

			const string DummyFilePath = "X:/DummyMotion.pmotion";
			PMFile file = new PMFile();
			file.CreateMotionDefault();
			file.Save(DummyFilePath);

			file = PMFile.Load(DummyFilePath);

			if(!((file.itemDict.Count == 1) &&
			file.rootFolder.childList.Count == 1 &&
			((PMMotion)file.rootFolder.childList[0]).pointList.Count == 2)) {
				throw new Exception("데이터 로드에 실패했습니다.");
			}
		}
	}
}
