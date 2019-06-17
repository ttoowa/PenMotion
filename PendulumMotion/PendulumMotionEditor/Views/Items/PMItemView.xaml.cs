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
using PendulumMotionEditor.Views.Windows;
using GKit;
using GKit.WPF;

namespace PendulumMotionEditor.Views.Items {
	/// <summary>
	/// MLMotionItem.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class PMItemView : UserControl {
		private static Root Root => Root.Instance;
		private static GLoopEngine LoopEngine => Root.loopEngine;
		private static CursorStorage CursorStorage => Root.cursorStorage;

		private static SolidColorBrush DefaultBG = "4F4F4F".ToBrush();
		private static SolidColorBrush SelectedBG = "787878".ToBrush();
		private static SolidColorBrush GraphLineColor = "A89676".ToBrush();
		private static SolidColorBrush EditTextBG = "383838".ToBrush();
		private const int GraphResolution = 16;

		public PMItemBase ownerItem;
		private Line[] graphLines;
		private GLoopAction focusedLoopAction;
		private bool onFocus;
		private string prevName;

		private PMItemView() {
			InitializeComponent();
			//for WPFDesigner
		}
		public PMItemView(PMItemBase ownerItem, PMItemType type) {
			this.ownerItem = ownerItem;

			InitializeComponent();
			Init();
			RegisterEvent();

			switch (type) {
				case PMItemType.Motion:
					ContentPanel.Children.Remove(FolderContent);
					CreatePreviewGraph();
					break;
				case PMItemType.Folder:
					ContentPanel.Children.Remove(MotionContent);
					break;
			}

			void Init() {
				SetNameEditTextVisible(false, true, false);
			}
			void RegisterEvent() {
				NameEditText.KeyDown += OnKeyDown_NameEditText;
			}
		}
		public void SetRootFolder() {
			BackPanel.Children.Remove(ContentContext);
			ChildContext.Margin = new Thickness();
		}
		public void SetName(string name) {
			NameEditText.Text = name;
		}
		public void SetSelected(bool selected) {
			ContentPanel.Background = selected ? SelectedBG : DefaultBG;
		}
		public void SetNameEditTextVisible(bool visible, bool force = false, bool callNameChangedEvent = true) {
			if (onFocus == visible && !force)
				return;
			onFocus = visible;

			if (visible) {
				prevName = NameEditText.Text;
				NameEditText.Background = EditTextBG;
				NameEditText.Cursor = Cursors.IBeam;
				NameEditText.Focus();
				NameEditText.IsReadOnly = false;
				NameEditText.IsHitTestVisible = true;
				
				RegisterFocusedLoopAction();
			} else {
				NameEditText.Background = null;
				NameEditText.Cursor = CursorStorage.cursor_default;
				NameEditText.IsReadOnly = true;
				NameEditText.IsHitTestVisible = false;

				UnregisterFocusedLoopAction();

				if(callNameChangedEvent) {
					OnNameEditComplete();
				}
			}
		}

		//Event
		private void RegisterFocusedLoopAction() {
			if (focusedLoopAction != null)
				return;
			focusedLoopAction = LoopEngine.AddLoopAction(OnFocusedTick);
		}
		private void UnregisterFocusedLoopAction() {
			if (focusedLoopAction == null)
				return;
			focusedLoopAction.Stop();
			focusedLoopAction = null;
		}
		private void OnNameEditComplete() {
			if (CheckAvailableName(NameEditText.Text)) {
				string newName = FilterName(NameEditText.Text);
				ownerItem.Rename(newName);
				NameEditText.Text = newName;
			} else if(prevName != NameEditText.Text) {
				NameEditText.Text = prevName;
				ToastMessage.Show("이미 존재하는 이름입니다.");
			}
		}
		private void OnFocusedTick() {
			if(MouseInput.Left.Down && !NameEditText.IsMouseOver && !ContentPanel.IsMouseOver) {
				SetNameEditTextVisible(false);
			}
		}
		private void OnKeyDown_NameEditText(object sender, KeyEventArgs e) {
			if(e.Key == Key.Return) {
				SetNameEditTextVisible(false);
				Keyboard.ClearFocus();
			}
		}
		private bool CheckAvailableName(string name) {
			return !ownerItem.ownerFile.itemDict.ContainsKey(name);
		}
		private string FilterName(string name) {
			return name.Trim();
		}

		//MotionItem
		public void CreatePreviewGraph() {
			graphLines = new Line[GraphResolution];

			for (int i = 0; i < graphLines.Length; ++i) {
				Line line = graphLines[i] = new Line();
				SetLineStyle(line);

				PreviewGraphContext.Children.Add(line);
			}

			void SetLineStyle(Line line) {
				line.Stroke = GraphLineColor;
				line.StrokeThickness = 1.5d;
			}
		}
		public void UpdatePreviewGraph(PMMotion ownerMotion) {
			float previewRectWidth = (float)PreviewGraphContext.ActualWidth;
			float previewRectHeight = (float)PreviewGraphContext.ActualHeight;
			for (int graphLineI = 0; graphLineI < graphLines.Length; ++graphLineI) {
				float motionValue = GetMotionValue(graphLineI);
				float nextMotionValue = GetMotionValue(graphLineI + 1);

				Line line = graphLines[graphLineI];
				line.X1 = graphLineI * previewRectWidth / GraphResolution;
				line.X2 = (graphLineI + 1) * previewRectWidth / GraphResolution;
				line.Y1 = previewRectHeight - motionValue * previewRectHeight;
				line.Y2 = previewRectHeight - nextMotionValue * previewRectHeight;

				float GetMotionValue(int index) {
					float linearValue = (float)index / graphLines.Length;
					return ownerMotion.GetMotionValue(linearValue);
				}
			}
		}
	}
}
