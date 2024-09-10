using System;
using PenMotion.Datas;
using PenMotion.Datas.Items;
using PenMotion.System;

namespace PenMotion {
	public static class MotionQuery {
		public static void LoadFile(string filePath, string fileId = null) { 
			if(!CheckLoaded(fileId)) {
				MotionFile file = new MotionFile();
				file.Load(filePath);

				if (fileId == null) {
					MotionStorage.defaultFile = file;
				} else {
					MotionStorage.fileDict.Add(fileId, file);
				}
			} else {
				throw new Exception("Already loaded same fileID.");
			}
		}
		public static void UnloadFile(string fileId = null) {
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
		public static bool CheckLoaded(string fileId = null) {
			if(fileId == null) {
				return MotionStorage.defaultFile != null;
			} else {
				return MotionStorage.fileDict.ContainsKey(fileId);
			}
		}

		public static float GetMotionValueByName(string motionId, float linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
			return GetMotionValueByName(null, motionId, linearValue, maxSample, tolerance);
		}
		public static PVector2 GetMotionValueByName(string motionId, PVector2 linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
			return GetMotionValueByName(null, motionId, linearValue, maxSample, tolerance);
		}
		public static PVector3 GetMotionValueByName(string motionId, PVector3 linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
			return GetMotionValueByName(null, motionId, linearValue, maxSample, tolerance);
		}

		public static float GetMotionValueByName(string fileId, string motionId, float linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
			MotionFile file = MotionStorage.GetFile(fileId);
			return file.GetMotionValueByName(motionId, PMath.Clamp01(linearValue), maxSample, tolerance);
		}
		public static PVector2 GetMotionValueByName(string fileId, string motionId, PVector2 linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
			MotionFile file = MotionStorage.GetFile(fileId);
			return file.GetMotionValueByName(motionId, PMath.Clamp01(linearValue), maxSample, tolerance);
		}
		public static PVector3 GetMotionValueByName(string fileId, string motionId, PVector3 linearValue, int maxSample = MotionItem.DefaultMaxSample, float tolerance = MotionItem.DefaultMaxTolerance) {
			MotionFile file = MotionStorage.GetFile(fileId);
			return file.GetMotionValueByName(motionId, PMath.Clamp01(linearValue), maxSample, tolerance);
		}
	}
}
