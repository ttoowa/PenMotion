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
using PendulumMotion.Components;
using PendulumMotion.Components.Items;
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
		
		public bool IsRoot => ParentItemView == null;
		public MotionItemType Type {
			get; private set;
		}

		public MotionItemBase Data {
			get; set;
		}

		public MotionFolderItemView ParentItemView {
			get; protected set;
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
			NameTextBox.KeyDown += NameEditText_OnKeyDown;
			ContentPanel.MouseDown += ItemContentPanel_OnMouseDown;
		}

		public void DetachParent() {
			if(ParentItemView != null) {
				ParentItemView.RemoveChild(this);
			}
		}

		public bool SetName(string name) {
			bool result = Data.Rename(name);
			UpdateNameTextBox();
			return result;
		}
		public void SetSelected(bool selected) {
			ContentPanel.Background = selected ? SelectedBG : DefaultBG;
		}
		public void SetNameTextBoxEditable(bool editable) {
			if (editable) {
				NameTextBox_StartEdit();
			} else {
				NameTextBox_EndEdit();
			}
		}

		private void UpdateNameTextBox() {
			NameTextBox.Text = Data.name;
		}

		//Event
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

			if (!Data.Rename(newName)) {
				ToastMessage.Show("이미 존재하거나 올바르지 않은 이름입니다.");
			}
			UpdateNameTextBox();
		}
		private void OnFocusedTick() {
			if(MouseInput.Left.Down && !NameTextBox.IsMouseOver && !ContentPanel.IsMouseOver) {
				SetNameTextBoxEditable(false);
			}
		}
		private void NameEditText_OnKeyDown(object sender, KeyEventArgs e) {
			if(e.Key == Key.Return) {
				SetNameTextBoxEditable(false);
				Keyboard.ClearFocus();
			}
		}

		private void ItemContentPanel_OnMouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ClickCount == 1) {
				if (KeyInput.GetKeyHold(WinKey.LeftControl) || KeyInput.GetKeyHold(WinKey.RightControl)) {
					//Ctrl 단독 선택 추가/제거
					if (MotionTab.IsSelected(this)) {
						MotionTab.UnselectItemSingle(this);
					} else {
						MotionTab.SelectItemAdd(this);
					}
				} else if (KeyInput.GetKeyHold(WinKey.LeftShift) || KeyInput.GetKeyHold(WinKey.RightShift)) {
					//Shift 드래그 선택
				} else {
					//일반 클릭
					LoopEngine.AddGRoutine(ItemContentPanel_OnMouseDrag());
				}
			} else if (e.ClickCount == 2) {
				//더블클릭

				if (MotionTab.selectedItemSet.Count < 2) {
					SetNameTextBoxEditable(true);
				}
			}
			e.Handled = true;
		}
		private IEnumerator ItemContentPanel_OnMouseDrag() {
			for (; ; ) {
				if (!MouseInput.Left.Hold) {
					MotionTab.SelectItemSingle(this);
					yield break;
				}
				if (!IsMouseOverY(ContentPanel)) {
					if (!MotionTab.IsSelected(this)) {
						MotionTab.SelectItemSingle(this);
					}
					LoopEngine.AddLoopAction(ItemContentPanel_OnMouseDrag_ForMove, GLoopCycle.EveryFrame, GWhen.MouseUpRemove);
					yield break;
				}

				yield return new GWait(GTimeUnit.Frame, 1);
			}
		}
		private void ItemContentPanel_OnMouseDrag_ForMove() {
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
