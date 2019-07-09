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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GKit;
using GKit.WPF;

namespace PenMotionEditor.UI.FX {
	/// <summary>
	/// ClickFx.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ClickFx : UserControl {
		private const float StartAlpha = 0.6f;
		private const float TimeDelta = 0.05f;

		private GLoopAction tickAction;
		private float time;

		public ClickFx() {
			InitializeComponent();
			if (this.IsDesignMode())
				return;

			StartMotion();
		}
		public void Dispose() {
			this.DetachParent();
		}

		private void StartMotion() {
			DoubleAnimation anim = new DoubleAnimation(0f, 1f, new Duration(TimeSpan.FromMilliseconds(600)));
			anim.Completed += Anim_Completed;
			BeginAnimation(ScaleTransform.ScaleXProperty, anim);
		}

		private void Anim_Completed(object sender, EventArgs e) {
			Dispose();
		}
	}
}
