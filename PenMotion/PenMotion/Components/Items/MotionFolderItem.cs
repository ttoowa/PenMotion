using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PendulumMotion.Components.Items {
	public class MotionFolderItem : MotionItemBase {

		public bool HasChild => childList.Count > 0;
		public List<MotionItemBase> childList;

		internal MotionFolderItem(MotionFile ownerFile) : base(ownerFile, MotionItemType.Folder) {
			childList = new List<MotionItemBase>();
		}
		public void AddChild(MotionItemBase child) {
			childList.Add(child);
			child.parent = this;
		}
		public void InsertChild(int index, MotionItemBase child) {
			childList.Insert(index, child);
			child.parent = this;
		}
		public void RemoveChild(MotionItemBase child) {
			childList.Remove(child);
			child.parent = null;
		}
	}
}
