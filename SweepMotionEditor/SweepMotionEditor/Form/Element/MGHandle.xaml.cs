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

namespace SweepMotionEditor.Form.Element
{
	/// <summary>
	/// MGHandle.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MGHandle : UserControl
	{
		public const float MainHandleWidth = 20f;
		public const float MainHandleWidthHalf = MainHandleWidth * 0.5f;

		private Property<Vector2> position;
		private SubHandle[] subHandles;

		public MGHandle()
		{
			InitializeComponent();

			position = new Property<Vector2>();
			subHandles = new SubHandle[] {
				new SubHandle(subHandle0, subLine0),
				new SubHandle(subHandle1, subLine1),
			};

			//Add event
			position.OnValueChanged += OnValueChanged_position;
		}

		//Update랑 종속 바꾸기
		private void OnValueChanged_position(Vector2 before, Vector2 newValue)
		{
			Canvas.SetLeft(this, newValue.x);
			Canvas.SetTop(this, newValue.y);
		}

		public void SetMainHandle(Vector2 position) {
			Canvas.SetLeft(this, position.x);
			Canvas.SetTop(this, position.y);
		}
		public void SetSubHandle(int index, Vector2 position) {
			subHandles[index].position.Value = position;
		}
		private void Update() {
			position.RunEvent();
			for(int i=0; i<subHandles.Length; ++i) {
				SubHandle subHandle = subHandles[i];
				subHandle.Update();
			}
		}
	}
	public class SubHandle {
		private const float Width = 12f;
		private const float WidthHalf = Width * 0.5f;

		public Property<Vector2> position;
		public Ellipse handle;
		public Line line;

		public SubHandle(Ellipse handle, Line line) {
			position = new Property<Vector2>();
			this.handle = handle;
			this.line = line;

			//Add event
			position.OnValueChanged += OnValueChanged_position;
		}
		public void Update() {
			position.RunEvent();
		}

		private void OnValueChanged_position(Vector2 before, Vector2 newValue)
		{
			//Handle
			Canvas.SetLeft(handle, -WidthHalf + newValue.x);
			Canvas.SetTop(handle, -WidthHalf + newValue.y);
			//Line
			line.X2 = position.Value.x;
			line.Y2 = position.Value.y;
		}
	}
}
