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
using PendulumMotion.Component;

namespace PendulumMotionEditor.Views.Elements {
	/// <summary>
	/// MLMotionItem.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MLMotionItem : UserControl {
		public PMotionData motionData;

		public MLMotionItem() {
			InitializeComponent();

			this.motionData = PMotionData.Default;
		}
		public MLMotionItem(PMotionData motionData) {
			InitializeComponent();

			this.motionData = motionData;
		}
	}
}
