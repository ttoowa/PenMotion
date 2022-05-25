using System;
using System.Collections.Generic;
using System.Text;
using PenMotion.System;

namespace PenMotion.Datas.Items.Elements {
	public class MotionPoint
	{
		public const float DefaultSubPointOffset = 0.3f;

		public PVector2 MainPoint {
			get; private set;
		}
		public PVector2[] SubPoints {
			get; private set;
		}

		public delegate void MainPointChangedDelegate(PVector2 position);
		public event MainPointChangedDelegate MainPointChanged;

		public delegate void SubPointChangedDelegate(int index, PVector2 position);
		public event SubPointChangedDelegate SubPointChanged;

		public MotionPoint() : this(PVector2.Zero) {
		}
		public MotionPoint(PVector2 mainPoint) : this(
			mainPoint,
			new PVector2(-DefaultSubPointOffset, 0f),
			new PVector2(DefaultSubPointOffset, 0f)) {
		}
		public MotionPoint(PVector2 mainPoint, PVector2 subPointLeft, PVector2 subPointRight)
		{
			this.MainPoint = mainPoint;
			this.SubPoints = new PVector2[] {
				subPointLeft,
				subPointRight,
			};
		}

		public void SetMainPoint(PVector2 position) {
			MainPoint = position;

			MainPointChanged?.Invoke(position);
		}
		public void SetSubPoint(int index, PVector2 position) {
			SubPoints[index] = position;

			SubPointChanged?.Invoke(index, position);
		}

		public PVector2 GetAbsoluteSubPoint(int index) {
			return MainPoint + SubPoints[index];
		}
	}
}
