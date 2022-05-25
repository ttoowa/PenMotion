using PenMotionEditor.UI.Windows;
using System.Windows;

namespace PenMotionEditor {
	/// <summary>
	/// App.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class App : Application {
		private void OnStartup(object sender, StartupEventArgs e) {
			MainWindow window = new MainWindow();
			window.Show();
		}
	}
}
