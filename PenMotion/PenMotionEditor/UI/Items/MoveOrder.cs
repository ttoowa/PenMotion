using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PenMotion;
using PenMotion.Datas.Items;
using PenMotionEditor.UI.Items;
using GKit;

namespace PenMotionEditor.UI.Items {
	public class MoveOrder {
		public MotionItemBase target;
		public Direction direction;

		public MoveOrder() {

		}
		public MoveOrder(MotionItemBase target) {
			this.target = target;
		}
		public MoveOrder(MotionItemBase target, Direction direction) {
			this.target = target;
			this.direction = direction;
		}
	}
}
