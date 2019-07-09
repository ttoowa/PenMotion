using System;
using PendulumMotion.Components;
using PendulumMotion.Components.Items;
using PendulumMotion.System;

namespace PendulumMotion {
	public static class MotionQuery {
		public static void LoadFile(string filePath, string fileId) { 
			if(!CheckLoaded(fileId)) {
				MotionFile file = MotionFile.Load(filePath);
				if (filePath == null) {
					MotionStorage.fileDict.Add(fileId, file);
				} else {
					MotionStorage.defaultFile = file;
				}
			} else {
				throw new Exception("Already loaded same fileID.");
			}
		}
		public static void UnloadFile(string fileId) {
			if(fileId == null) {
				MotionStorage.defaultFile = null;
			} else {
				if(MotionStorage.fileDict.ContainsKey(fileId)) {
					MotionStorage.fileDict.Remove(fileId);
				} else {
					throw new Exception("Not found fileID.");
				}
			}
		}
		public static bool CheckLoaded(string fileId) {
			if(fileId == null) {
				return MotionStorage.defaultFile != null;
			} else {
				return MotionStorage.fileDict.ContainsKey(fileId);
			}
		}

		public static float GetMotionValue(string fileId, string motionId, float linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
			MotionFile file = MotionStorage.GetFile(fileId);
			return file.GetMotionValue(motionId, PMath.Clamp01(linearValue), maxSample, tolerance);
		}
		public static PVector2 GetMotionValue(string fileId, string motionId, PVector2 linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
			MotionFile file = MotionStorage.GetFile(fileId);
			return file.GetMotionValue(motionId, PMath.Clamp01(linearValue), maxSample, tolerance);
		}
		public static PVector3 GetMotionValue(string fileId, string motionId, PVector3 linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
			MotionFile file = MotionStorage.GetFile(fileId);
			return file.GetMotionValue(motionId, PMath.Clamp01(linearValue), maxSample, tolerance);
		}
	}
}
