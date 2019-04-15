using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PendulumMotion.Items {
	public class PMItemBase {
		public bool IsRoot => parent == null;
		public PMFolder parent;
		public PMItemType type;
		public string name;
		#if OnEditor
		public object view;
		#endif

		public PMItemBase(PMItemType type) {
			this.type = type;
		}
	}
}
