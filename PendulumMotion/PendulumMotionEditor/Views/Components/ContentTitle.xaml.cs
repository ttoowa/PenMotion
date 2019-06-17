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

namespace PendulumMotionEditor.Views.Components {
	/// <summary>
	/// ContentPanel.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ContentTitle : UserControl {
		public string TitleText {
			get {
				return (string)GetValue(TitleTextProperty);
			} set {
				SetValue(TitleTextProperty, value);
			}
		}

		public static readonly DependencyProperty TitleTextProperty = DependencyProperty.Register(nameof(TitleText), typeof(string), typeof(ContentTitle), new PropertyMetadata(string.Empty));

		public ContentTitle() {
			InitializeComponent();
		}
	}
}
