using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PendulumMotion;
using PendulumMotion.Items;
using GKit;

namespace PendulumMotionEditor.Views.Items {
	public class MoveOrder {
		public PMItemBase target;
		public Direction direction;

		public MoveOrder() {

		}
		public MoveOrder(PMItemBase target) {
			this.target = target;
		}
		public MoveOrder(PMItemBase target, Direction direction) {
			this.target = target;
			this.direction = direction;
		}
	}
}
