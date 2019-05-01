using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PendulumMotion.Component;

namespace PendulumMotion.Items {
	public class PMFolder : PMItemBase {

		public bool HasChild => childList.Count > 0;
		public List<PMItemBase> childList;

		internal PMFolder(PMFile ownerFile) : base(ownerFile, PMItemType.Folder) {
			childList = new List<PMItemBase>();
		}
		public void AddChild(PMItemBase child) {
			//Without view
			childList.Add(child);
			child.parent = this;
		}
		public void InsertChild(int index, PMItemBase child) {
			childList.Insert(index, child);
			child.parent = this;
		}
	}
}
