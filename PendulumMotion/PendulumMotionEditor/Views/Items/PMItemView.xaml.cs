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
using PendulumMotion.Items;
using GKit;

namespace PendulumMotionEditor.Views.Items {
	/// <summary>
	/// MLMotionItem.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class PMItemView : UserControl {
		private static SolidColorBrush DefaultBG = "E0E0E0".ToBrush();
		private static SolidColorBrush SelectedBG = "EACB9E".ToBrush();

		public PMItemView() {
			InitializeComponent();
		}
		public PMItemView(PMItemType type) {
			InitializeComponent();
			switch (type) {
				case PMItemType.Motion:
					ContentPanel.Children.Remove(FolderContent);
					break;
				case PMItemType.Folder:
					ContentPanel.Children.Remove(MotionContent);
					break;
				case PMItemType.RootFolder:
					BackPanel.Children.Remove(ContentContext);
					ChildContext.Margin = new Thickness();
					break;
			}
		}
		public void SetSelected(bool selected) {
			ContentPanel.Background = selected ? SelectedBG : DefaultBG;
		}
	}
}
