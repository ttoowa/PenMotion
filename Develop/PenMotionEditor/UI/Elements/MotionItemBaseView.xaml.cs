using GKitForWPF;
using GKitForWPF.Graphics;
using GKitForWPF.UI.Controls;
using PenMotion.Datas;
using PenMotion.Datas.Items;
using PenMotionEditor.UI.Tabs;
using PenMotionEditor.UI.Windows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PenMotionEditor.UI.Elements {
	public partial class MotionItemBaseView : UserControl, ITreeItem {
		protected MotionEditorContext EditorContext;
		protected MotionTab MotionTab => EditorContext.MotionTab;
		protected GraphEditorTab GraphEditorTab => EditorContext.GraphEditorTab;
		protected GLoopEngine LoopEngine => EditorContext.LoopEngine;
		protected CursorStorage CursorStorage => EditorContext.CursorStorage;
		protected MotionFile EditingFile => EditorContext.EditingFile;

		private static SolidColorBrush DefaultBG = "4F4F4F".ToBrush();
		private static SolidColorBrush SelectedBG = "787878".ToBrush();
		private static SolidColorBrush EditTextBG = "383838".ToBrush();

		public string DisplayName => NameTextBox.Text;

		public bool IsRoot => Data.IsRoot;
		public ITreeFolder ParentItem {
			get; set;
		}

		public FrameworkElement ItemContext => ContentContext;

		public MotionItemType Type {
			get; private set;
		}
		public MotionItemBase Data {
			get; set;
		}

		// [ Constructor ]
		public MotionItemBaseView() {
			InitializeComponent();
		}
		public MotionItemBaseView(MotionEditorContext editorContext, MotionItemType type) {
			this.EditorContext = editorContext;
			this.Type = type;

			InitializeComponent();

			// Register event
			NameTextBox.KeyDown += NameEditText_KeyDown;
			NameTextBox.TextEdited += NameTextBox_TextEdited;
		}

		// [ Event ]
		// DataChanged
		internal void Data_NameChanged(string oldName, string newName) {
			NameTextBox.Text = newName;
		}

		// NameText
		private void NameEditText_KeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Return) {
				NameTextBox.EndEditing();
				Keyboard.ClearFocus();
			}
		}

		private void NameTextBox_TextEdited(string oldText, string newText, ref bool cancelEdit) {
			newText = FilterName(newText);

			if (Data != null) {
				if (!Data.SetName(newText)) {
					ToastMessage.Show("이미 존재하거나 올바르지 않은 이름입니다.");
					cancelEdit = true;
				}
			}
		}
		public void SetSelected(bool isSelected) {
			ContentPanel.Background = isSelected ? SelectedBG : DefaultBG;
		}
		public void SetDisplayName(string name) {
			NameTextBox.Text = name;
		}

		private string FilterName(string name) {
			return name.Trim();
		}
	}
}
