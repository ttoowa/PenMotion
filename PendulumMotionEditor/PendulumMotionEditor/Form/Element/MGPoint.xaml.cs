using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GKit;

namespace SweepMotionEditor
{
	/// <summary>
	/// MGHandle.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MGPoint : UserControl
	{
		private const float DefaultSubOffset = 0.3f;
		private const float MainHandleWidth = 20f;
		private const float MainHandleWidthHalf = MainHandleWidth * 0.5f;
		private const float SubHandleWidth = 12f;
		private const float SubHandleWidthHalf = SubHandleWidth * 0.5f;

		public Property<Vector2> mainHandlePos;
		public Property<Vector2>[] subHandlePoss;
		private Rectangle mainHandle;
		private Ellipse[] subHandles;
		private Line[] lines;

		public MGPoint()
		{
			InitializeComponent();

			mainHandlePos = new Property<Vector2>();
			subHandlePoss = new Property<Vector2>[] {
				new Property<Vector2>(new Vector2(-DefaultSubOffset, 0f)),
				new Property<Vector2>(new Vector2(DefaultSubOffset, 0f)),
			};
			mainHandle = MainHandle;
			subHandles = new Ellipse[] {
				SubHandle0,
				SubHandle1,
			};
			lines = new Line[] {
				SubLine0,
				SubLine1,
			};

			//Add event
			mainHandlePos.OnValueChanged += OnValueChanged_mainHandlePos;
			for(int i=0; i<subHandlePoss.Length; ++i) {
				subHandlePoss[i].OnValueChanged += OnValueChanged_subHandlePos;
			}
		}

		public void UpdateAll() {
			UpdateMainHandle();
			UpdateSubHandle();
		}
		public void UpdateMainHandle() {
			Thickness margin = this.Margin;
			margin.Left = mainHandlePos.Value.x;
			margin.Bottom = mainHandlePos.Value.y;
			this.Margin = margin;
		}
		public void UpdateSubHandle() {
			for(int i=0; i<subHandles.Length; ++i) {
				Ellipse subHandle = subHandles[i];
				Line line = lines[i];
				Property<Vector2> subHandlePos = subHandlePoss[i];

				//Handle
				Canvas.SetLeft(subHandle, -SubHandleWidthHalf + subHandlePos.Value.x);
				Canvas.SetTop(subHandle, -SubHandleWidthHalf + subHandlePos.Value.y);
				//Line
				line.X2 = subHandlePos.Value.x;
				line.Y2 = subHandlePos.Value.y;
			}
		}

		private void OnValueChanged_mainHandlePos(Vector2 before, Vector2 newValue)
		{
			UpdateMainHandle();
		}
		private void OnValueChanged_subHandlePos(Vector2 before, Vector2 newValue)
		{
			UpdateSubHandle();
		}
	}
}
