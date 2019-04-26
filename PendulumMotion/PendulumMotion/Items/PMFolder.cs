using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PendulumMotion.Component;

namespace PendulumMotion.Items {
	public class PMFolder : PMItemBase {

		public List<PMItemBase> childList;

		internal PMFolder(PMFile ownerFile) : base(ownerFile, PMItemType.Folder) {
			childList = new List<PMItemBase>();
		}
	}
}
