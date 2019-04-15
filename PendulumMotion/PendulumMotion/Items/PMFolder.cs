using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PendulumMotion.Items {
	public class PMFolder : PMItemBase {

		public List<PMItemBase> childList;

		public PMFolder() : base(PMItemType.Folder) {
			childList = new List<PMItemBase>();
		}
	}
}
