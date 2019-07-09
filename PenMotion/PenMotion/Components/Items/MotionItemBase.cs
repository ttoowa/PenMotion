using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PendulumMotion.System;

namespace PendulumMotion.Components.Items {
	public class MotionItemBase {
		public bool IsRoot => parent == null;
		public MotionFile ownerFile;
		public MotionFolderItem parent;
		public MotionItemType type;
		public string name;

		public MotionItemBase(MotionFile ownerFile, MotionItemType type) {
			this.ownerFile = ownerFile;
			this.type = type;
		}
		public bool Rename(string newName) {
			if(string.IsNullOrEmpty(newName) || ownerFile.itemDict.ContainsKey(newName)) {
				return false;
			} 

			if(string.IsNullOrEmpty(newName) && ownerFile.itemDict.ContainsKey(name)) {
				ownerFile.itemDict.Remove(name);
			}
			ownerFile.itemDict.Add(newName, this);
			name = newName;
			return true;
		}
	}
}
