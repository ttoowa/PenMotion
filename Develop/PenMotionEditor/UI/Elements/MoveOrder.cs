using GKitForWPF;
using PenMotion.Datas.Items;

namespace PenMotionEditor.UI.Elements {
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
