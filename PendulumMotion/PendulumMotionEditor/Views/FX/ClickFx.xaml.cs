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
using GKit.WPF;

namespace PendulumMotionEditor.Views.FX {
	/// <summary>
	/// ClickFx.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ClickFx : UserControl {
		private static Root Root => Root.Instance;
		private static GLoopEngine LoopEngine => Root.loopEngine;

		private const float StartAlpha = 0.6f;
		private const float TimeDelta = 0.05f;

		private GLoopAction tickAction;
		private float time;

		public ClickFx() {
			InitializeComponent();
			if (this.IsDesignMode())
				return;

			FxGroup.Visibility = Visibility.Collapsed;
			tickAction = LoopEngine.AddLoopAction(OnTick);
		}
		public void Dispose() {
			this.DetachParent();
		}

		private void OnTick() {
			time += TimeDelta;
			float motionTime = Mathf.Pow(time, 0.6f);
			if(time >= 1f) {
				time = 1f;
				tickAction.Stop();
			}

			float alpha = (1f - motionTime) * StartAlpha;
			float scale = motionTime;

			FxGroup.Opacity = alpha;
			FxGroup.RenderTransform = new ScaleTransform(scale, scale);
			FxGroup.Visibility = Visibility.Visible;
		}
	}
}
