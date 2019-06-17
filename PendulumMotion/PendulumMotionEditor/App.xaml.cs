using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using PendulumMotionEditor.Views.Windows;

namespace PendulumMotionEditor
{
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
