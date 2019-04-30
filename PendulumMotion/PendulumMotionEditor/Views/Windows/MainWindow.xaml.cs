using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
using Microsoft.Win32;
using GKit;
using PendulumMotion;
using PendulumMotion.Component;
using PendulumMotion.Items;
using PendulumMotion.System;
using PendulumMotionEditor.Views.Items;

namespace PendulumMotionEditor.Views.Windows
{
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window
	{
		private static Root Root;
		private static GLoopEngine LoopEngine => Root.loopEngine;
		private static CursorStorage CursorStorage => Root.cursorStorage;

		private int PreviewFps => Mathf.Clamp(PreviewFpsEditText.textBox.Text.Parse2Int(60), 1, 1000);
		private float PreviewSeconds => Mathf.Clamp(PreviewSecondsEditText.textBox.Text.Parse2Float(1f), 0.02f, 1000f);
		private float previewTime;
		private bool OnSaveMarked => editingFile == null || editingFile.ShowSaveMessage();

		//Preview
		private GLoopEngine previewLoopEngine;
		private Stopwatch previewWatch;
		private float UpdateFPSTimer;
		
		public bool OnEditing => editingFile != null;
		public EditableMotionFile editingFile;

		public MainWindow()
		{
			InitializeComponent();
			if (this.CheckDesignMode())
				return;

			Root = new Root(this);
			Loaded += OnLoad;
		}

		//Event
		private void OnLoad(object sender, RoutedEventArgs e)
		{
			Init();
			RegisterEvents();
			
			void Init() {
				previewLoopEngine = new GLoopEngine(registInput:false);
				previewLoopEngine.MaxOverlapFrame = 3;
				previewWatch = new Stopwatch();

				Cursor = CursorStorage.cursor_default;
				SetContentContextVisible(false);
				MLItemMovePointer.Visibility = Visibility.Collapsed;
				PreviewFpsEditText.textBox.SetOnlyIntInput();
				PreviewSecondsEditText.textBox.SetOnlyFloatInput();
				PreviewFpsEditText.textBox.Text = 60.ToString();
				PreviewSecondsEditText.textBox.Text = 1.ToString();

				previewLoopEngine.StartLoop();
				previewLoopEngine.AddLoopAction(OnPreviewTick);

				LoopEngine.AddGRoutine(UpdateItemPreviewRoutine());
			}
			void RegisterEvents() {
				Closing += OnClosing;
				const int TimeTextBoxMaxLength = 5;
				EditPanel.SizeChanged += OnSizeChanged_EditPanel;
				PreviewFpsEditText.LostFocus += OnLostFocus_PreviewFpsEditText;
				PreviewFpsEditText.KeyDown += OnKeyDown_PreviewFpsEditText;
				PreviewFpsEditText.textBox.MaxLength = TimeTextBoxMaxLength;
				PreviewSecondsEditText.LostFocus += OnLostFocus_PreviewSecondsEditText;
				PreviewSecondsEditText.KeyDown += OnKeyDown_PreviewSecondsEditText;
				PreviewSecondsEditText.textBox.MaxLength = TimeTextBoxMaxLength;

				//Button reaction
				Grid[] btns = new Grid[] {
					TMNewFileButton,
					TMOpenFileButton,
					TMSaveFileButton,
					MLCreateMotionButton,
					MLCreateFolderButton,
					MLRemoveButton,
					MLCopyButton,
				};
				for(int i=0; i<btns.Length; ++i) {
					Grid btn = btns[i];
					btn.SetBtnColor((Border)btn.Children[btn.Children.Count - 1]);
				}

				//Topmenu button
				TMNewFileButton.SetOnClick(OnClick_TMNewFileButton);
				TMOpenFileButton.SetOnClick(OnClick_TMOpenFileButton);
				TMSaveFileButton.SetOnClick(OnClick_TMSaveFileButton);

				//Motionlist button
				MLCreateMotionButton.SetOnClick(OnClick_MLCreateMotionButton);
				MLCreateFolderButton.SetOnClick(OnClick_MLCreateFolderButton);
				MLRemoveButton.SetOnClick(OnClick_MLRemoveButton);
				MLCopyButton.SetOnClick(OnClick_MLCopyButton);

			}
		}
		private void OnClosing(object sender, CancelEventArgs e) {
			if (!OnSaveMarked) {
				e.Cancel = true;
			}
		}
		private void OnPreviewTick() {
			const float SeparatorWidthHalf = 2f;
			const float UpdateFPSTick = 0.5f;
			const float OverTimeSec = 0.8f;

			float previewSec = PreviewSeconds;
			float previewFps = PreviewFps;
			float frameDelta = 1f / previewSec / previewFps;
			float maxOverTime = OverTimeSec / previewSec;
			//Simulate Time
			previewTime += frameDelta;
			if(previewTime > 1f + maxOverTime || previewTime < -maxOverTime) {
				previewTime = -maxOverTime;
			}
			float actualTime = Mathf.Clamp01(previewTime);
			float motionTime = EditPanel.OnEditing ? EditPanel.editingMotion.GetMotionValue(actualTime) : 0f;

			//Update Radar
			EditPanel.UpdatePreview(previewTime, maxOverTime);

			//Update Position
			double gridWidth = PreviewPositionGrid.ColumnDefinitions[1].ActualWidth;
			double previewPos = gridWidth * motionTime - PreviewPositionShape.ActualWidth * 0.5f - SeparatorWidthHalf;
			PreviewPositionShape.Margin = new Thickness(previewPos, 0d, 0d, 0d);

			//Update Scale
			PreviewScaleShape.RenderTransform = new ScaleTransform(motionTime, motionTime);

			//Update TextInfos
			previewWatch.Stop();
			ActualFrameTextView.Text = $"({((int)(previewSec * previewFps))} Frame)";
			float deltaMillisec = previewWatch.GetElapsedMilliseconds();
			float deltaSec = deltaMillisec * 0.001f;
			if (UpdateFPSTimer < 0f) {
				if (deltaMillisec > 0.01f) {
					UpdateFPSTimer = UpdateFPSTick;
					ActualFPSTextView.Text = $"{(1f / deltaSec).ToString("0.0")} FPS";
				}
			} else {
				UpdateFPSTimer -= deltaSec;
			}

			previewWatch.Restart();
		}
		private void OnSizeChanged_EditPanel(object sender, SizeChangedEventArgs e) {
			EditPanel.UpdateUI();
		}
		private void OnLostFocus_PreviewFpsEditText(object sender, RoutedEventArgs e) {
			UpdatePreviewFpsEditText();
		}
		private void OnLostFocus_PreviewSecondsEditText(object sender, RoutedEventArgs e) {
			UpdatePreviewSecondsEditText();
		}
		private void OnKeyDown_PreviewFpsEditText(object sender, KeyEventArgs e) {
			if (e.Key == Key.Return) {
				UpdatePreviewFpsEditText();
			}
		}
		private void OnKeyDown_PreviewSecondsEditText(object sender, KeyEventArgs e) {
			if (e.Key == Key.Return) {
				UpdatePreviewSecondsEditText();
			}
		}
		private void OnClick_TMNewFileButton() {
			if(OnEditing && editingFile.isUnsaved)
				return;

			if (OnSaveMarked) {
				ClearEditingData();
				editingFile = new EditableMotionFile();
				SetContentContextVisible(true);
			}
		}
		private void OnClick_TMOpenFileButton() {
			if (OnEditing && editingFile.isUnsaved)
				return;

			if (OnSaveMarked) {
				Dispatcher.BeginInvoke(new Action(() => {
					ClearEditingData();
					editingFile = EditableMotionFile.Load();

					if (editingFile != null) {
						//Success
						SetContentContextVisible(true);


						List<PMItemBase> rootItemList = editingFile.file.rootFolder.childList;
						if (rootItemList.Count > 0) {
							editingFile.SelectItemSingle(rootItemList[0]);
						}
					}
				}));
			}
		}
		private void OnClick_TMSaveFileButton() {
			Dispatcher.BeginInvoke(new Action(()=> {
				editingFile.Save();
			}));
		}
		private void OnClick_MLCreateMotionButton() {
			PMMotion motion = editingFile.CreateMotion();
			editingFile.SelectItemSingle(motion);

			editingFile.MarkUnsaved();
		}
		private void OnClick_MLCreateFolderButton() {
			PMFolder folder = editingFile.CreateFolder();
			editingFile.SelectItemSingle(folder);

			editingFile.MarkUnsaved();
		}
		private void OnClick_MLRemoveButton() {
			editingFile.RemoveSelectedItems();

			editingFile.MarkUnsaved();
		}
		private void OnClick_MLCopyButton() {


			editingFile.MarkUnsaved();
		}

		//UI
		private IEnumerator UpdateItemPreviewRoutine() {
			//UpdateItemPreview
			int iterCounter = 0;
			for(; ;) {
				if(OnEditing) {
					yield return UpdateItemPreview(editingFile.file.rootFolder);
				}
				yield return new GWait(GTimeUnit.Frame, 1);
			}

			IEnumerator UpdateItemPreview(PMFolder folder) {
				for(int childI=0; childI<folder.childList.Count; ++childI) {
					PMItemBase child = folder.childList[childI];
					switch(child.type) {
						case PMItemType.Motion:
							PMMotion motion = child.Cast<PMMotion>();
							motion.view.Cast<PMItemView>().UpdatePreviewGraph(motion);

							if(++iterCounter >= 2) {
								iterCounter = 0;
								yield return new GWait(GTimeUnit.Frame, 1);
							}
							break;
						case PMItemType.Folder:
							yield return UpdateItemPreview(child.Cast<PMFolder>());
							break;
					}
				}
			}
		}
		private void ClearEditingData() {
			EditPanel.DetachMotion();
			MLItemContext.Children.Clear();
		}
		public void ResetPreviewTime() {
			previewTime = 0f;
		}
		public void SetContentContextVisible(bool show) {
			ContentContext.Visibility = show ? Visibility.Visible : Visibility.Hidden;
		}
		public void ApplyPreviewFPS() {
			previewLoopEngine.FPS = PreviewFps;
		}
		private void UpdatePreviewFpsEditText() {
			PreviewFpsEditText.textBox.Text = PreviewFps.ToString();
		}
		private void UpdatePreviewSecondsEditText() {
			PreviewSecondsEditText.textBox.Text = PreviewSeconds.ToString();
		}
	}
}
