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

namespace PendulumMotionEditor.Views.Items
{
	/// <summary>
	/// MGHandle.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class PMPointView : UserControl
	{
		private const float DefaultSubOffset = 0.3f;
		private const float MainHandleWidth = 20f;
		private const float MainHandleWidthHalf = MainHandleWidth * 0.5f;
		private const float SubHandleWidth = 12f;
		private const float SubHandleWidthHalf = SubHandleWidth * 0.5f;

		public Rectangle MainHandleView {
			get; private set;
		}
		public Ellipse[] SubHandleViews {
			get; private set;
		}
		public Line[] SubLineViews {
			get; private set;
		}

		public PMPointView()
		{
			InitializeComponent();

			MainHandleView = this.MainHandle;
			SubHandleViews = new Ellipse[] {
				SubHandle0,
				SubHandle1,
			};
			SubLineViews = new Line[] {
				SubLine0,
				SubLine1,
			};
		}
	}
}
