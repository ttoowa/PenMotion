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
		public const float DefaultSubOffset = 0.3f;
		public const float MainHandleWidth = 20f;
		public const float MainHandleWidthHalf = MainHandleWidth * 0.5f;
		public const float SubHandleWidth = 14f;
		public const float SubHandleWidthHalf = SubHandleWidth * 0.5f;

		public Grid MainHandleView {
			get; private set;
		}
		public Grid[] SubHandleViews {
			get; private set;
		}
		public Line[] SubLineViews {
			get; private set;
		}

		public PMPointView()
		{
			InitializeComponent();

			MainHandleView = this.MainHandle;
			SubHandleViews = new Grid[] {
				SubHandle0,
				SubHandle1,
			};
			SubLineViews = new Line[] {
				SubLine0,
				SubLine1,
			};

			RegisterEvent();

			void RegisterEvent() {
				SetBtnColor(MainHandleView);
				for(int subI=0; subI<SubHandleViews.Length; ++subI) {
					SetBtnColor(SubHandleViews[subI]);
				}
			}
			void SetBtnColor(Grid button) {
				button.SetBtnColor((Shape)button.Children[button.Children.Count - 1], 0.3f);
			}
		}
	}
}
