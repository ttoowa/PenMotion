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
		private static Root Root => Root.Instance;
		private static GLoopEngine LoopEngine => Root.loopEngine;
		private static CursorStorage CursorStorage => Root.cursorStorage;

		private static SolidColorBrush DefaultBG = "E0E0E0".ToBrush();
		private static SolidColorBrush SelectedBG = "EACB9E".ToBrush();
		private static SolidColorBrush GraphLineColor = "B09753".ToBrush();
		private static SolidColorBrush EditTextBG = "FFFFFF".ToBrush();
		private const int GraphResolution = 10;

		private Line[] graphLines;
		private GLoopAction focusedLoopAction;
		private bool onFocus;
		private string prevName;

		public PMItemView() {
			InitializeComponent();
		}
		public PMItemView(PMItemType type) {
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
				case PMItemType.RootFolder:
					BackPanel.Children.Remove(ContentContext);
					ChildContext.Margin = new Thickness();
					break;
			}

			void Init() {
				SetNameEditTextVisible(false, true);
			}
			void RegisterEvent() {
				NameEditText.KeyDown += OnKeyDown_NameEditText;
			}
		}

		public void SetName(string name) {
			NameEditText.Text = name;
		}
		public void SetSelected(bool selected) {
			ContentPanel.Background = selected ? SelectedBG : DefaultBG;
		}
		public void SetNameEditTextVisible(bool visible, bool force = false) {
			if(visible) {
				if (onFocus && !force)
					return;
				onFocus = true;

				prevName = NameEditText.Text;
				NameEditText.Background = EditTextBG;
				NameEditText.Cursor = Cursors.IBeam;
				NameEditText.Focus();
				NameEditText.IsReadOnly = false;
				NameEditText.IsHitTestVisible = true;
				
				RegisterFocusedLoopAction();
			} else {
				if (!onFocus && !force)
					return;
				onFocus = false;

				NameEditText.Background = null;
				NameEditText.Cursor = CursorStorage.cursor_default;
				NameEditText.IsReadOnly = true;
				NameEditText.IsHitTestVisible = false;

				UnregisterFocusedLoopAction();

				if (CheckAvailableName(NameEditText.Text)) {
					FilterName();
				} else {
					NameEditText.Text = prevName;
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
		private void OnFocusedTick() {
			if(MouseInput.LeftDown && !NameEditText.IsMouseOver && !ContentPanel.IsMouseOver) {
				SetNameEditTextVisible(false);
			}
		}
		private void OnKeyDown_NameEditText(object sender, KeyEventArgs e) {
			if(e.Key == Key.Return) {
				SetNameEditTextVisible(false);
			}
		}
		private bool CheckAvailableName(string name) {
			return true;
		}
		private void FilterName() {
			string name = NameEditText.Text;
			name = name.Trim();
			NameEditText.Text = name;
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
