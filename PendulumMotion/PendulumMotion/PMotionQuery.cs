using System;
using PendulumMotion.Component;
using PendulumMotion.Items;
using PendulumMotion.System;

namespace PendulumMotion {
	public static class PMotionQuery {
		public static void LoadFile(string filePath, string fileId) { 
			if(!CheckLoaded(fileId)) {
				PMFile file = PMFile.Load(filePath);
				if (filePath == null) {
					PMotionStorage.fileDict.Add(fileId, file);
				} else {
					PMotionStorage.defaultFile = file;
				}
			} else {
				throw new Exception("Already loaded same fileID.");
			}
		}
		public static void UnloadFile(string fileId) {
			if(fileId == null) {
				PMotionStorage.defaultFile = null;
			} else {
				if(PMotionStorage.fileDict.ContainsKey(fileId)) {
					PMotionStorage.fileDict.Remove(fileId);
				} else {
					throw new Exception("Not found fileID.");
				}
			}
		}
		public static bool CheckLoaded(string fileId) {
			if(fileId == null) {
				return PMotionStorage.defaultFile != null;
			} else {
				return PMotionStorage.fileDict.ContainsKey(fileId);
			}
		}

		public static float GetMotionValue(string fileId, string motionId, float linearValue, int maxSample = PMMotion.DefaultMaxSample, float tolerance = PMMotion.DefaultMaxTolerance) {
			PMFile file = PMotionStorage.GetFile(fileId);
			return file.GetMotionValue(motionId, PMath.Clamp01(linearValue), maxSample, tolerance);
		}
		public static PVector2 GetMotionValue(string fileId, string motionId, PVector2 linearValue, int maxSample = PMMotion.DefaultMaxSample, float tolerance = PMMotion.DefaultMaxTolerance) {
			PMFile file = PMotionStorage.GetFile(fileId);
			return file.GetMotionValue(motionId, PMath.Clamp01(linearValue), maxSample, tolerance);
		}
		public static PVector3 GetMotionValue(string fileId, string motionId, PVector3 linearValue, int maxSample = PMMotion.DefaultMaxSample, float tolerance = PMMotion.DefaultMaxTolerance) {
			PMFile file = PMotionStorage.GetFile(fileId);
			return file.GetMotionValue(motionId, PMath.Clamp01(linearValue), maxSample, tolerance);
		}
	}
}
