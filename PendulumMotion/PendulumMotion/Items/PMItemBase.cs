using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PendulumMotion.Component;
using PendulumMotion.System;

namespace PendulumMotion.Items {
	public class PMItemBase {
		public bool IsRoot => parent == null;
		public bool IsFolder => type == PMItemType.Folder;
		public PMFile ownerFile;
		public PMFolder parent;
		public PMItemType type;
		public string name;
		public object view;

		public PMItemBase(PMFile ownerFile, PMItemType type) {
			this.ownerFile = ownerFile;
			this.type = type;
		}
		public bool Rename(string newName) {
			if(newName == null || ownerFile.itemDict.ContainsKey(newName)) {
				return false;
			} else {
				if(name != null && ownerFile.itemDict.ContainsKey(name)) {
					ownerFile.itemDict.Remove(name);
				}
				ownerFile.itemDict.Add(newName, this);
				name = newName;
				return true;
			}
		}
	}
}
