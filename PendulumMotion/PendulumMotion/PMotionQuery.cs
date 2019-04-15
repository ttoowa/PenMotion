using System;
using PendulumMotion.Component;
using PendulumMotion.Items;
using PendulumMotion.System;

namespace PendulumMotion {
	public static class PMotionQuery {
		public static void LoadFile(string filePath, string fileID) { 
			if(!CheckLoaded(fileID)) {
				PMFile file = PMFile.Load(filePath);
				if (filePath == null) {
					PMotionStorage.fileDict.Add(fileID, file);
				} else {
					PMotionStorage.defaultFile = file;
				}
			} else {
				throw new Exception("Already loaded same fileID.");
			}
		}
		public static void UnloadFile(string fileID) {
			if(fileID == null) {
				PMotionStorage.defaultFile = null;
			} else {
				if(PMotionStorage.fileDict.ContainsKey(fileID)) {
					PMotionStorage.fileDict.Remove(fileID);
				} else {
					throw new Exception("Not found fileID.");
				}
			}
		}
		public static bool CheckLoaded(string fileID) {
			if(fileID == null) {
				return PMotionStorage.defaultFile != null;
			} else {
				return PMotionStorage.fileDict.ContainsKey(fileID);
			}
		}

		public static float GetMotionValue(string fileID, string motionID, float linearValue, int maxSample = PMMotion.DefaultMaxSample, float tolerance = PMMotion.DefaultMaxTolerance) {
			PMFile file = PMotionStorage.GetFile(fileID);
			return file.GetMotionValue(motionID, PMath.Clamp01(linearValue), maxSample, tolerance);
		}
		public static PVector2 GetMotionValue(string fileID, string motionID, PVector2 linearValue, int maxSample = PMMotion.DefaultMaxSample, float tolerance = PMMotion.DefaultMaxTolerance) {
			PMFile file = PMotionStorage.GetFile(fileID);
			return file.GetMotionValue(motionID, PMath.Clamp01(linearValue), maxSample, tolerance);
		}
		public static PVector3 GetMotionValue(string fileID, string motionID, PVector3 linearValue, int maxSample = PMMotion.DefaultMaxSample, float tolerance = PMMotion.DefaultMaxTolerance) {
			PMFile file = PMotionStorage.GetFile(fileID);
			return file.GetMotionValue(motionID, PMath.Clamp01(linearValue), maxSample, tolerance);
		}
	}
}
