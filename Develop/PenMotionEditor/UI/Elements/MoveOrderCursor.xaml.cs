using System.Windows.Controls;

namespace PenMotionEditor.UI.Elements {
	/// <summary>
	/// MoveOrderCursor.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MoveOrderCursor : UserControl {
		public MoveOrderCursor() {
			InitializeComponent();
		}
		public void SetNameText(string name) {
			NameTextBlock.Text = name;
		}
	}
}
