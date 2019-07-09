using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PendulumMotion;
using PenMotionEditor.UI.Items;
using GKit;

namespace PenMotionEditor.UI.Items {
	public class MoveOrder {
		public MotionItemBaseView target;
		public Direction direction;

		public MoveOrder() {

		}
		public MoveOrder(MotionItemBaseView target) {
			this.target = target;
		}
		public MoveOrder(MotionItemBaseView target, Direction direction) {
			this.target = target;
			this.direction = direction;
		}
	}
}
