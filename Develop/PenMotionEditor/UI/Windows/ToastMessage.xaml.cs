using GKitForWPF;
using PenMotionEditor.UI.Tabs;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PenMotionEditor.UI.Windows {
	public partial class ToastMessage : Window {
		private const int WS_EX_TRANSPARENT = 0x00000020;
		private const int GWL_EXSTYLE = (-20);
		private const int WM_NCHITTEST = 0x0084;
		[DllImport("user32.dll")]
		public static extern int GetWindowLong(IntPtr hwnd, int index);
		[DllImport("user32.dll")]
		public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

		private const float DefaultDelaySec = 0.8f;
		private const float AlphaDelta = 0.05f;

		private float delaySec;

		public static void Show(string text, float delaySec = DefaultDelaySec) {
			ToastMessage toastMessage = new ToastMessage(text, delaySec);
			toastMessage.Show();
		}
		private ToastMessage() {
			InitializeComponent();
			//for WPFDesigner
		}
		private ToastMessage(string text, float delaySec) {
			InitializeComponent();
			SetAlpha(0f);
			this.delaySec = delaySec;
			this.MessageText.Text = text;
			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e) {
			Width = MessageText.ActualWidth + 200f;
			MotionEditorContext.Instance.LoopEngine.AddGRoutine(ToastRoutine(delaySec));
		}
		protected override void OnSourceInitialized(EventArgs e) {
			base.OnSourceInitialized(e);

			// Get this window's handle
			IntPtr hwnd = new WindowInteropHelper(this).Handle;

			// Change the extended window style to include WS_EX_TRANSPARENT
			int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
			SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
		}

		private IEnumerator ToastRoutine(float delaySec) {
			for (float alpha = 0f; alpha < 1f; alpha += AlphaDelta * 3f) {
				SetAlpha(alpha);

				yield return new GWait(GTimeUnit.Frame, 1);
			}
			SetAlpha(1f);

			yield return new GWait(GTimeUnit.Second, delaySec);

			for (float alpha = 1f; alpha > 0f; alpha -= AlphaDelta) {
				SetAlpha(alpha);

				yield return new GWait(GTimeUnit.Frame, 1);
			}
			Close();
		}
		private void SetAlpha(float alpha) {
			//Dispatcher.BeginInvoke(new Action(() => {
			//}));
			Opacity = alpha;
		}
	}
}
