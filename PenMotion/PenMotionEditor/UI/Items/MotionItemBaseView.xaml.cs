using System;
using System.Collections;
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
using PenMotion.Datas;
using PenMotion.Datas.Items;
using GKit;
using GKit.WPF;
using PenMotionEditor.UI.Tabs;
using PenMotionEditor.UI.Items.Elements;
using PenMotionEditor.UI.Windows;
using GKit.WPF.UI.Controls;

namespace PenMotionEditor.UI.Items {
	public partial class MotionItemBaseView : UserControl, IListItem {
		protected EditorContext EditorContext;
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
		public IListFolder ParentItem {
			get; set;
		}

		public FrameworkElement ItemContext => ContentContext;

		public MotionItemType Type {
			get; private set;
		}
		public MotionItemBase Data {
			get; set;
		}

		public MotionItemBaseView() {
			InitializeComponent();
		}
		public MotionItemBaseView(EditorContext editorContext, MotionItemType type) {
			this.EditorContext = editorContext;
			this.Type = type;

			InitializeComponent();
			Init();
			RegisterEvent();
		}
		private void Init() {
			SetNameTextBoxEditable(false);
		}
		private void RegisterEvent() {
			NameTextBox.KeyDown += NameEditText_KeyDown;
		}
		private void SetNameTextBoxEditable(bool editable) {
			if (editable) {
				NameTextBox_StartEdit();
			} else {
				NameTextBox_EndEdit();
			}
		}

		public void SetDisplaySelected(bool isSelected) {
			ContentPanel.Background = isSelected ? SelectedBG : DefaultBG;
		}

		//Event
		//DataChanged
		internal void Data_NameChanged(string oldName, string newName) {
			NameTextBox.Text = newName;
		}

		//NameText
		private void NameTextBox_StartEdit() {
			NameTextBox.Background = EditTextBG;
			NameTextBox.Cursor = Cursors.IBeam;
			NameTextBox.Focus();
			NameTextBox.IsReadOnly = false;
			NameTextBox.IsHitTestVisible = true;
		}
		private void NameTextBox_EndEdit() {
			NameTextBox.Background = null;
			NameTextBox.Cursor = CursorStorage.cursor_default;
			NameTextBox.IsReadOnly = true;
			NameTextBox.IsHitTestVisible = false;

			string newName = FilterName(NameTextBox.Text);

			if (Data != null) {
				if (!Data.SetName(newName)) {
					ToastMessage.Show("이미 존재하거나 올바르지 않은 이름입니다.");
				}
			}
		}
		private void NameEditText_KeyDown(object sender, KeyEventArgs e) {
			if(e.Key == Key.Return) {
				SetNameTextBoxEditable(false);
				Keyboard.ClearFocus();
			}
		}

		public void SetDisplayName(string name) {
			NameTextBox.Text = name;
		}

		//private void OnFocusedTick() {
		//	if(MouseInput.Left.Down && !NameTextBox.IsMouseOver && !ContentPanel.IsMouseOver) {
		//		SetNameTextBoxEditable(false);
		//	}
		//}

		private string FilterName(string name) {
			return name.Trim();
		}
	}
}
