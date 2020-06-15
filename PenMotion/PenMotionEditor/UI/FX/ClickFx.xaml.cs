using GKitForWPF;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PenMotionEditor.UI.FX {
	/// <summary>
	/// ClickFx.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ClickFx : UserControl {
		private const float StartAlpha = 0.6f;
		private const float TimeDelta = 0.05f;

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
			const float Duration = 400;

			QuinticEase ease = new QuinticEase() {
				EasingMode = EasingMode.EaseOut,
			};
			DoubleAnimation alphaAnim = new DoubleAnimation(0.3f, 0f, new Duration(TimeSpan.FromMilliseconds(Duration)));
			DoubleAnimation scaleAnim = new DoubleAnimation(0.8f, 1f, new Duration(TimeSpan.FromMilliseconds(Duration)));
			scaleAnim.Completed += Anim_Completed;
			alphaAnim.EasingFunction = ease;
			scaleAnim.EasingFunction = ease;

			FxGroup.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
			FxGroup.BeginAnimation(Canvas.OpacityProperty, alphaAnim);
		}

		private void Anim_Completed(object sender, EventArgs e) {
			Dispose();
		}
	}
}
