using GKitForWPF;
using PenMotion.Datas.Items;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PenMotionEditor.UI.Tabs {
	public partial class PreviewTab : UserControl {
		private MotionEditorContext EditorContext;
		private GraphEditorTab GraphEditorTab => EditorContext.GraphEditorTab;
		private MotionItem EditingMotionData => GraphEditorTab.EditingMotionData;

		private const float SeparatorWidthHalf = 2f;

		private int PreviewFps => Mathf.Clamp(FpsEditText.Text.Parse2Int(60), 1, 500);
		private float PreviewSeconds => Mathf.Clamp(SecondsEditText.Text.Parse2Float(1f), 0.02f, 1000f);
		private float ActualPreviewTime => Mathf.Clamp01(previewTime);
		private float previewTime;

		private GLoopEngine previewLoopEngine;
		private Stopwatch previewWatch;
		private float UpdateFPSTimer;
		private Ellipse[] previewContinuum;

		public PreviewTab() {
			InitializeComponent();
		}
		public void Init(MotionEditorContext editorContext) {
			this.EditorContext = editorContext;

			InitMembers();
			InitUI();
			RegisterEvents();
		}
		private void InitMembers() {
			previewLoopEngine = new GLoopEngine(registInput: false);
			previewLoopEngine.MaxOverlapFrame = 1;
			previewWatch = new Stopwatch();

			previewLoopEngine.StartLoop();
		}
		private void InitUI() {
			FpsEditText.SetOnlyIntInput();
			SecondsEditText.SetOnlyFloatInput();
			FpsEditText.Text = 60.ToString();
			SecondsEditText.Text = 1.ToString();
			CreatePositionContinuum();
		}
		private void RegisterEvents() {
			PositionCanvas.SizeChanged += PositionCanvas_OnSizeChanged;
			FpsEditText.LostFocus += FpsEditText_OnLostFocus;
			FpsEditText.KeyDown += FpsEditText_OnKeyDown;
			SecondsEditText.LostFocus += SecondsEditText_OnLostFocus;
			SecondsEditText.KeyDown += SecondsEditText_OnKeyDown;

			previewLoopEngine.AddLoopAction(OnPreviewTick);
		}

		//Events
		private void OnPreviewTick() {
			const float OverTimeSec = 0.8f;

			if (!GraphEditorTab.OnEditing) {
				PositionCanvas.Visibility = Visibility.Hidden;
				ScaleShape.Visibility = Visibility.Hidden;
				return;
			}
			PositionCanvas.Visibility = Visibility.Visible;
			ScaleShape.Visibility = Visibility.Visible;


			float previewSec = PreviewSeconds;
			int previewFps = PreviewFps;
			float maxOverTime = OverTimeSec / previewSec;

			SetFPS(previewFps);

			SimulateTime(ref previewTime, previewSec, maxOverTime);
			float motionTime = EditingMotionData.GetMotionValue(ActualPreviewTime);

			GraphEditorTab.UpdatePlaybackRadar(previewTime, maxOverTime);
			UpdatePositionShape(motionTime);
			UpdateScaleShape(motionTime);
			UpdateInfoTexts(previewSec, previewFps);
		}
		private void PositionCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e) {
			if (!GraphEditorTab.OnEditing)
				return;

			float motionTime = EditingMotionData.GetMotionValue(ActualPreviewTime);

			UpdatePositionContinuum();
			UpdatePositionShape(motionTime);
		}
		private void FpsEditText_OnLostFocus(object sender, RoutedEventArgs e) {
			UpdateFpsEditText();
		}
		private void FpsEditText_OnKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Return) {
				UpdateFpsEditText();
			}
		}
		private void SecondsEditText_OnLostFocus(object sender, RoutedEventArgs e) {
			UpdateSecondsEditText();
		}
		private void SecondsEditText_OnKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Return) {
				UpdateSecondsEditText();
			}
		}

		//Time
		private void SimulateTime(ref float previewTime, float previewSec, float maxOverTime) {
			previewTime += previewLoopEngine.DeltaSeconds / previewSec;
			if (previewTime > 1f + maxOverTime || previewTime < -maxOverTime) {
				previewTime = -maxOverTime;
			}
		}
		public void ResetPreviewTime() {
			previewTime = 0f;
		}
		public void SetFPS(int fps) {
			previewLoopEngine.FPS = fps;
		}

		//Update shapes
		private void UpdatePositionShape(float motionTime) {

			double gridWidth = PositionCanvas.ActualWidth;
			double previewPos = gridWidth * motionTime - PositionShape.ActualWidth * 0.5f - SeparatorWidthHalf;
			Canvas.SetLeft(PositionShape, previewPos);
			Canvas.SetTop(PositionShape, (PositionCanvas.ActualHeight - PositionShape.Height) * 0.5f);
		}
		private void UpdateScaleShape(float motionTime) {
			ScaleShape.RenderTransform = new ScaleTransform(motionTime, motionTime);
		}

		//Update infos
		private void UpdateFpsEditText() {
			FpsEditText.Text = PreviewFps.ToString();
		}
		private void UpdateSecondsEditText() {
			SecondsEditText.Text = PreviewSeconds.ToString();
		}
		private void UpdateInfoTexts(float previewSec, float previewFps) {
			const float UpdateFpsTick = 0.5f;

			previewWatch.Stop();
			ActualFrameTextView.Text = $"({((int)(previewSec * previewFps))} Frame)";
			float deltaMillisec = previewWatch.GetElapsedMilliseconds();
			float deltaSec = deltaMillisec * 0.001f;
			if (UpdateFPSTimer < 0f) {
				if (deltaMillisec > 0.01f) {
					UpdateFPSTimer = UpdateFpsTick;
					ActualFPSTextView.Text = $"{(1f / deltaSec).ToString("0.0")} FPS";
				}
			} else {
				UpdateFPSTimer -= deltaSec;
			}
			previewWatch.Restart();
		}

		private void CreatePositionContinuum() {
			const int ContinuumResolution = 20;
			const float ContinuumElementAlpha = 0.15f;

			previewContinuum = new Ellipse[ContinuumResolution];
			for (int i = 0; i < previewContinuum.Length; ++i) {
				Ellipse continuumElement = previewContinuum[i] = new Ellipse();

				continuumElement.SetParent(PositionCanvas);
				continuumElement.Width = PositionShape.Width;
				continuumElement.Height = PositionShape.Height;
				continuumElement.Fill = PositionShape.Fill;
				continuumElement.Stroke = PositionShape.Stroke;
				continuumElement.StrokeThickness = PositionShape.StrokeThickness;
				continuumElement.HorizontalAlignment = PositionShape.HorizontalAlignment;
				continuumElement.Opacity = ContinuumElementAlpha;
				Grid.SetColumn(continuumElement, Grid.GetColumn(PositionShape));
				Grid.SetColumnSpan(continuumElement, Grid.GetColumnSpan(PositionShape));
			}
			UpdatePositionContinuum();
		}
		public void UpdatePositionContinuum() {
			if (previewContinuum == null || !GraphEditorTab.OnEditing) {
				return;
			}

			double gridWidth = PositionCanvas.ActualWidth;

			for (int i = 0; i < previewContinuum.Length; ++i) {
				float linearTime = (float)i / (previewContinuum.Length - 1);
				float motionTime = EditingMotionData.GetMotionValue(linearTime);

				Ellipse continuumElement = previewContinuum[i];

				Canvas.SetLeft(continuumElement, motionTime * gridWidth - PositionShape.ActualWidth * 0.5f - SeparatorWidthHalf);
				Canvas.SetTop(continuumElement, (PositionCanvas.ActualHeight - continuumElement.Height) * 0.5f);
			}
		}
		public void SetPositionContinuumVisible(bool visible) {
			if (previewContinuum == null)
				return;

			for (int i = 0; i < previewContinuum.Length; ++i) {
				previewContinuum[i].Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
			}
		}


	}
}
