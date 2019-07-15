using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PenMotion.System;

namespace PenMotion.Datas.Items {
	public class MotionItemBase {
		public MotionFile OwnerFile {
			get; private set;
		}
		public MotionFolderItem Parent {
			get; set;
		}

		public bool IsRoot => Parent == null;

		public MotionItemType Type {
			get; protected set;
		}
		public string Name {
			get; private set;
		}

		public delegate void NameChangedDelegate(string oldName, string newName);
		public event NameChangedDelegate NameChanged;

		public MotionItemBase(MotionFile ownerFile, MotionItemType type) {
			this.OwnerFile = ownerFile;
			this.Type = type;
		}

		public bool SetName(string newName, bool force = false) {
			string oldName = Name;

			if(!force) {
				if (oldName == newName) {
					NameChanged?.Invoke(oldName, newName);
					return true;
				}
				if(string.IsNullOrEmpty(newName) || OwnerFile.itemDict.ContainsKey(newName))
					return false;

				if(!string.IsNullOrEmpty(oldName) && OwnerFile.itemDict.ContainsKey(oldName)) {
					OwnerFile.itemDict.Remove(oldName);
				}
			}

			OwnerFile.itemDict.Add(newName, this);
			Name = newName;

			NameChanged?.Invoke(oldName, newName);
			return true;
		}
	}
}
