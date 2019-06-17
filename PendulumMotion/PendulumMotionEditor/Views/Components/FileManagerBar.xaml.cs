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

namespace PendulumMotionEditor.Views.Components {
	/// <summary>
	/// FileManager.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class FileManagerBar : UserControl {
		public event Action OnClick_CreateFileButton;
		public event Action OnClick_OpenFileButton;
		public event Action OnClick_SaveFileButton;
		
		public FileManagerBar() {
			InitializeComponent();
			
			if(this.IsDesignMode())
				return;

			RegisterEvents();
		}
		private void RegisterEvents() {
			CreateFileButton.Click += (object sender, RoutedEventArgs e) => { OnClick_CreateFileButton?.Invoke(); };
			OpenFileButton.Click += (object sender, RoutedEventArgs e) => { OnClick_OpenFileButton?.Invoke(); };
			SaveFileButton.Click += (object sender, RoutedEventArgs e) => { OnClick_SaveFileButton?.Invoke(); };
		}

	}
}
