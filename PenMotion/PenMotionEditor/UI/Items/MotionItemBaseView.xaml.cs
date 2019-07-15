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

namespace PenMotionEditor.UI.Items {
	public partial class MotionItemBaseView : UserControl {
		protected EditorContext EditorContext;
		protected MotionTab MotionTab => EditorContext.MotionTab;
		protected GraphEditorTab GraphEditorTab => EditorContext.GraphEditorTab;
		protected GLoopEngine LoopEngine => EditorContext.LoopEngine;
		protected CursorStorage CursorStorage => EditorContext.CursorStorage;
		protected MotionFile EditingFile => EditorContext.EditingFile;

		private static SolidColorBrush DefaultBG = "4F4F4F".ToBrush();
		private static SolidColorBrush SelectedBG = "787878".ToBrush();
		private static SolidColorBrush EditTextBG = "383838".ToBrush();
		
		public MotionItemType Type {
			get; private set;
		}

		public MotionItemBase Data {
			get; set;
		}

		private GLoopAction focusedLoopAction;

		private MotionItemBaseView() {
			InitializeComponent();
			//for WPFDesigner
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
			ContentPanel.MouseDown += ItemContentPanel_MouseDown;
		}
		private void SetNameTextBoxEditable(bool editable) {
			if (editable) {
				NameTextBox_StartEdit();
			} else {
				NameTextBox_EndEdit();
			}
		}

		public void SetSelected(bool selected) {
			ContentPanel.Background = selected ? SelectedBG : DefaultBG;
		}

		//Event
		//DataChanged
		internal void Data_NameChanged(string oldName, string newName) {
			NameTextBox.Text = Data.Name;
		}

		//NameText
		private void NameTextBox_StartEdit() {
			NameTextBox.Background = EditTextBG;
			NameTextBox.Cursor = Cursors.IBeam;
			NameTextBox.Focus();
			NameTextBox.IsReadOnly = false;
			NameTextBox.IsHitTestVisible = true;

			if (focusedLoopAction == null) {
				focusedLoopAction = LoopEngine.AddLoopAction(OnFocusedTick);
			}
		}
		private void NameTextBox_EndEdit() {
			NameTextBox.Background = null;
			NameTextBox.Cursor = CursorStorage.cursor_default;
			NameTextBox.IsReadOnly = true;
			NameTextBox.IsHitTestVisible = false;

			if (focusedLoopAction != null) {
				focusedLoopAction.Stop();
				focusedLoopAction = null;
			}

			string newName = FilterName(NameTextBox.Text);

			if (Data != null) {
				if (!Data.SetName(newName)) {
					ToastMessage.Show("이미 존재하거나 올바르지 않은 이름입니다.");
				}
			}
		}
		private void OnFocusedTick() {
			if(MouseInput.Left.Down && !NameTextBox.IsMouseOver && !ContentPanel.IsMouseOver) {
				SetNameTextBoxEditable(false);
			}
		}
		private void NameEditText_KeyDown(object sender, KeyEventArgs e) {
			if(e.Key == Key.Return) {
				SetNameTextBoxEditable(false);
				Keyboard.ClearFocus();
			}
		}
		//Mouse
		private void ItemContentPanel_MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton != MouseButton.Left)
				return;

			if (e.ClickCount == 1) {
				if (KeyInput.GetKeyHold(WinKey.LeftControl) || KeyInput.GetKeyHold(WinKey.RightControl)) {
					//Ctrl 단독 선택 추가/제거
					if (MotionTab.IsSelected(Data)) {
						MotionTab.UnselectItemSingle(Data);
					} else {
						MotionTab.SelectItemAdd(Data);
					}
				} else if (KeyInput.GetKeyHold(WinKey.LeftShift) || KeyInput.GetKeyHold(WinKey.RightShift)) {
					//Shift 드래그 선택
				} else {
					//일반 클릭
					LoopEngine.AddGRoutine(ItemContentPanel_MouseDrag());
				}
			} else if (e.ClickCount == 2) {
				//더블클릭

				if (MotionTab.selectedItemSet.Count < 2) {
					SetNameTextBoxEditable(true);
				}
			}
			e.Handled = true;
		}
		private IEnumerator ItemContentPanel_MouseDrag() {
			for (; ; ) {
				if (!MouseInput.Left.Hold) {
					//Just click
					MotionTab.SelectItemSingle(Data);
					yield break;
				}
				if (!IsMouseOverY(ContentPanel)) {
					//Move item
					if (!MotionTab.IsSelected(Data)) {
						MotionTab.SelectItemSingle(Data);
					}
					LoopEngine.AddLoopAction(ItemContentPanel_MouseDrag_ForMove, GLoopCycle.EveryFrame, GWhen.MouseUpRemove);
					yield break;
				}

				yield return new GWait(GTimeUnit.Frame, 1);
			}
		}
		private void ItemContentPanel_MouseDrag_ForMove() {
			if (MotionTab.selectedItemSet.Count == 0)
				return;

			MoveOrder moveOrder = MotionTab.GetMoveOrder();
			if (moveOrder != null) {
				if (!MouseInput.Left.Hold) {
					MotionTab.HideDstCursorView();
					MotionTab.ApplyMoveOrder(moveOrder);
				} else {
					MotionTab.UpdateDstCursorView(moveOrder);
				}
			} else {
				MotionTab.HideDstCursorView();
			}

			string selectedName = MotionTab.selectedItemSet.Count == 1 ?
				MotionTab.selectedItemSet.Select(item=>item.Name).ToArray()[0] : $"{MotionTab.selectedItemSet.Count} Items";

			MotionTab.UpdateDstItemText(selectedName);
			MotionTab.UpdateDstItemView();
		}

		private string FilterName(string name) {
			return name.Trim();
		}

		private bool IsMouseOverY(Panel itemPanel) {
			float panelTop = itemPanel.GetAbsolutePosition().y;
			return MouseInput.AbsolutePosition.y > panelTop && MouseInput.AbsolutePosition.y < panelTop + itemPanel.ActualHeight;
		}
	}
}
