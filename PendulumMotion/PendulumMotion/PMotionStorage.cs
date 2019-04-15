using System;
using System.Collections.Generic;
using System.Text;

namespace PendulumMotion.Component
{
	//에디터에서는 사용하지 않는다.
	//라이브러리를 사용하는 클라이언트에서 여러 모션파일을 로드하기 위해 여기에 보관한다.
	internal static class PMotionStorage
	{
		internal static Dictionary<string, PMFile> fileDict = new Dictionary<string, PMFile>();
		internal static PMFile defaultFile;

		internal static PMFile GetFile(string fileID) {
			if (fileID == null) {
				if (defaultFile != null) {
					return defaultFile;
				} else {
					throw new Exception("Not exist motion.");
				}
			} else {
				if (fileDict.ContainsKey(fileID)) {
					return fileDict[fileID];
				} else {
					throw new Exception("Not exist motion.");
				}
			}
		}
	}
}