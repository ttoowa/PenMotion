using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PenMotion.Datas.Items {
	public class MotionFolderItem : MotionItemBase {
		public delegate void ChildInsertedDelegate(int index, MotionItemBase childItem);
		public event ChildInsertedDelegate ChildInserted;

		public delegate void ChildRemovedDelegate(MotionItemBase childItem);
		public event ChildRemovedDelegate ChildRemoved;

		public bool HasChild => childList.Count > 0;
		public List<MotionItemBase> childList;

		internal MotionFolderItem(MotionFile ownerFile) : base(ownerFile, MotionItemType.Folder) {
			childList = new List<MotionItemBase>();
		}
		public void AddChild(MotionItemBase child) {
			InsertChild(childList.Count, child);
		}
		public void InsertChild(int index, MotionItemBase child) {
			childList.Insert(index, child);
			child.Parent = this;

			ChildInserted?.Invoke(index, child);
		}
		public void RemoveChild(MotionItemBase child) {
			childList.Remove(child);
			child.Parent = null;

			ChildRemoved?.Invoke(child);
		}
	}
}
