using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using PendulumMotion;
using PendulumMotion.Components;
using PendulumMotion.Components.Items;
using PendulumMotion.Components.Items.Elements;
using PendulumMotion.System;
using PenMotionEditor.UI.Items;
using PenMotionEditor.UI.Items.Elements;
using PenMotionEditor.UI.Tabs;
using PenMotionEditor.UI.Windows;
using GKit;
using GKit.WPF;

namespace PenMotionEditor.UI.Tabs {
	public partial class GraphEditorTab : UserControl {
		private EditorContext EditorContext;
		private GLoopEngine LoopEngine => EditorContext.LoopEngine;
		private PreviewTab PreviewTab => EditorContext.PreviewTab;
		private CursorStorage CursorStorage => EditorContext.CursorStorage;

		private const float WidthRatio = 1.777f;
		private static SolidColorBrush GraphLineColor = "B09753".ToBrush();

		//Datas
		public bool OnEditing => EditingMotionItemView != null;
		public MotionItemView EditingMotionItemView {
			get; set;
		}
		public MotionItem EditingMotionData => EditingMotionItemView.Data;

		private Vector2 cursorPosMemory;

		//Grids
		private const int GridVerticalCount = 12;
		private const int GridHorizontalCount = 12;
		private const int GraphResolution = 120;
		private const float NearDistance = 0.016f;
		private Line[] gridVerticals;
		private Line[] gridHorizontals;
		private Line[] graphLines;
		private Line[] outsideLines;

		//Display
		private float displayZoom;
		private PVector2 displayOffset;
		private PVector2 ActualOffset {
			get {
				return displayOffset * displayZoom;
			}
		}
		private PVector2 DGraphCenter {
			get {
				PVector2 actualOffset = ActualOffset;
				return new PVector2((float)GridContext.ActualWidth * 0.5f + actualOffset.x, (float)GridContext.ActualHeight * 0.5f + actualOffset.y);
			}
		}
		private PRect DGraphRect {
			get {
				PVector2 graphCenter = DGraphCenter;
				PVector2 graph01Size = DGraph01Size;
				PVector2 actualOffset = ActualOffset;
				return new PRect(
					graphCenter.x + actualOffset.x - graph01Size.x * 0.5f,
					graphCenter.y + actualOffset.y - graph01Size.y * 0.5f,
					graphCenter.x + actualOffset.x + graph01Size.x * 0.5f,
					graphCenter.y + actualOffset.y + graph01Size.y * 0.5f
				);
			}
		}
		private PVector2 DGraph01Size {
			get {
				return new PVector2((float)GridContext.ActualHeight * WidthRatio * displayZoom, (float)GridContext.ActualHeight * displayZoom);
			}
		}

		public GraphEditorTab() {
			InitializeComponent();
		}
		public void Init(EditorContext editorContext) {
			this.EditorContext = editorContext;

			InitMembers();
			InitUI();
			RegisterEvents();
		}
		private void InitMembers() {
		}
		private void InitUI() {
			CreateGrid();
			CreateGraph();

			DetachMotion();
		}
		private void RegisterEvents() {
			LoopEngine.AddLoopAction(OnTick);

			SizeChanged += OnSizeChanged;
			GridContext.MouseDown += OnMouseDown_BackPanel;
		}

		private void OnTick() {
			CheckCursorInteraction();
		}
		private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
			UpdateUI();
		}
		private void OnMouseDown_BackPanel(object sender, MouseButtonEventArgs e) {
			Keyboard.ClearFocus();
			if (KeyInput.GetKeyHold(WinKey.Space)) {
				if (KeyInput.GetKeyHold(WinKey.LeftControl) || KeyInput.GetKeyHold(WinKey.RightControl)) {
					LoopEngine.AddLoopAction(OnMouseDrag_BackPanel_ForZoom, GLoopCycle.EveryFrame, GWhen.MouseUpRemove);
				} else {
					LoopEngine.AddLoopAction(OnMouseDrag_BackPanel_ForPanning, GLoopCycle.EveryFrame, GWhen.MouseUpRemove);
				}
				cursorPosMemory = MouseInput.AbsolutePosition;
			}
		}
		private void OnMouseDrag_BackPanel_ForPanning() {
			const float MaxOffset = 320f;

			Vector2 cursorDelta = MouseInput.AbsolutePosition - cursorPosMemory;
			cursorPosMemory = MouseInput.AbsolutePosition;

			displayOffset += cursorDelta.ToPVector2() / displayZoom * 0.5f;
			displayOffset = BMath.Clamp(displayOffset.ToVector2(), -MaxOffset, MaxOffset).ToPVector2();

			UpdateUI();
		}
		private void OnMouseDrag_BackPanel_ForZoom() {
			const float MinZoom = 0.3f;
			const float MaxZoom = 1.5f;

			Vector2 cursorDelta = MouseInput.AbsolutePosition - cursorPosMemory;
			cursorPosMemory = MouseInput.AbsolutePosition;
			displayZoom += cursorDelta.x * 0.001f;
			displayZoom = Mathf.Clamp(displayZoom, MinZoom, MaxZoom);

			UpdateUI();
		}

		private void ResetEnv() {
			displayZoom = 0.6f;
			displayOffset = new PVector2();
		}

		private void CheckCursorInteraction() {
			const float InteractionThreshold = 0.02f;

			if (!OnEditing)
				return;

			bool cursorChanged = false;
			MotionPointView cursorOverPoint;
			int cursorOverPointIndex = -1;
			FindCursorOverPoint(out cursorOverPoint, out cursorOverPointIndex);

			if (KeyInput.GetKeyHold(WinKey.LeftAlt)) {
				//Remove mode
				if (cursorOverPoint != null && cursorOverPointIndex > 0 && cursorOverPointIndex < EditingMotionData.pointList.Count - 1) {
					SetCursor(CursorStorage.cursor_remove);
					cursorChanged = true;

					if (MouseInput.Left.Down) {
						RemovePointViewWithUpdate(cursorOverPoint);
					}
				}
			} else if (KeyInput.GetKeyHold(WinKey.LeftControl) && cursorOverPoint == null) {
				Vector2 cursorPos = DisplayToNormal(MouseInput.GetRelativePosition(PointCanvas));
				if (!KeyInput.GetKeyHold(WinKey.Space) && cursorPos.x > 0f && cursorPos.x < 1f) {
					//Add mode

					int index = EditingMotionItemView.Data.GetRightPointIndex(cursorPos.x);
					if (index > 0) {
						SetCursor(CursorStorage.cursor_add);
						SetSmartLineForX(cursorPos.x);
						cursorChanged = true;

						if (MouseInput.Left.Down) {
							CreatePointWithUpdate(index, cursorPos);
						}
					}
				} else {
					HideSmartLineForX();
				}
			}
			if (KeyInput.GetKeyUp(WinKey.LeftControl) && !MouseInput.Left.Hold) {
				HideSmartLineForX();
			}
			if (!cursorChanged) {
				SetCursor(CursorStorage.cursor_default);
			}
		}
		private void FindCursorOverPoint(out MotionPointView cursorOverPoint, out int cursorOverIndex) {
			cursorOverPoint = null;
			cursorOverIndex = -1;
			for (int handleI = 0; handleI < EditingMotionItemView.pointList.Count; ++handleI) {
				MotionPointView point = EditingMotionItemView.pointList[handleI];
				if (point.MainHandleView.IsMouseOver) {
					cursorOverIndex = handleI;
					cursorOverPoint = point;
				}
			}
		}

		public void AttachMotion(MotionItemView motionItemView) {
			DetachMotion();

			EditingMotionItemView = motionItemView;

			CreatePointsFromData(motionItemView.Data);
			UpdateUI();
			PreviewContext.Visibility = Visibility.Visible;

			PreviewTab.SetPositionContinuumVisible(true);
			PreviewTab.UpdatePositionContinuum();
		}
		public void DetachMotion() {
			EditingMotionItemView = null;

			ResetEnv();
			ClearPointViews();

			SetGraphVisible(false);
			HideSmartFollowText();
			HideSmartLineForX();
			HideSmartLineForY();
			PreviewContext.Visibility = Visibility.Collapsed;

			PreviewTab.SetPositionContinuumVisible(false);
		}

		public void UpdateUI() {
			if (!OnEditing)
				return;

			UpdateGrid();
			UpdateGraph();
			UpdatePointViews();
		}
		public void UpdatePlaybackRadar(float time, float maxOverTime) {
			PlaybackRadar.Height = PreviewContext.ActualHeight;

			PRect dGraphRect = DGraphRect;
			float x = dGraphRect.xMin + dGraphRect.Width * time;
			float overTime = time > 0f ? Mathf.Max(0f, time - 1f) : -time;
			PlaybackRadar.Opacity = 1f - overTime / maxOverTime;
			Canvas.SetLeft(PlaybackRadar, x - PlaybackRadar.Width);
		}

		//Grid
		private void CreateGrid() {
			SolidColorBrush gridColor = "#4D4D4D".ToBrush();
			SolidColorBrush grid01Color = "#525252".ToBrush();
			const float Line01Width = 2.5f;
			const float LineWidth = 1f;

			gridVerticals = new Line[GridVerticalCount];
			gridHorizontals = new Line[GridHorizontalCount];

			for (int i = 0; i < gridVerticals.Length; ++i) {
				bool is01Line = Mathf.Abs((i + 1) - gridVerticals.Length / 2) == 1;

				Line line = gridVerticals[i] = new Line();
				line.Stroke = is01Line ? grid01Color : gridColor;
				line.StrokeThickness = is01Line ? Line01Width : LineWidth;
				GridContext.Children.Add(line);
			}
			for (int i = 0; i < gridHorizontals.Length; ++i) {
				bool is01Line = Mathf.Abs((i + 1) - gridHorizontals.Length / 2) == 1;

				Line line = gridHorizontals[i] = new Line();
				line.Stroke = is01Line ? grid01Color : gridColor;
				line.StrokeThickness = is01Line ? Line01Width : LineWidth;
				GridContext.Children.Add(line);
			}
		}
		private void UpdateGrid() {
			PRect graphRect = DGraphRect;
			PVector2 graph01Size = DGraph01Size;

			for (int i = 0; i < gridVerticals.Length; ++i) {
				Line line = gridVerticals[i];
				float x = graphRect.xMin + ((i - (GridHorizontalCount / 3)) * graph01Size.x / (GridVerticalCount / 4 - 1));
				line.X1 =
				line.X2 = x;
				line.Y1 = 0d;
				line.Y2 = ActualHeight;
			}
			for (int i = 0; i < gridHorizontals.Length; ++i) {
				Line line = gridHorizontals[i];
				float y = graphRect.yMax - ((i - (GridHorizontalCount / 3)) * graph01Size.y / (GridHorizontalCount / 4 - 1));
				line.Y1 =
				line.Y2 = y;
				line.X1 = 0d;
				line.X2 = ActualWidth;
			}
		}

		//Graph
		private void CreateGraph() {
			graphLines = new Line[GraphResolution];
			outsideLines = new Line[2];

			for (int i = 0; i < graphLines.Length; ++i) {
				Line line = graphLines[i] = new Line();
				SetLineStyle(line);

				GraphContext.Children.Add(line);
			}
			for (int i = 0; i < outsideLines.Length; ++i) {
				Line line = outsideLines[i] = new Line();
				SetLineStyle(line);

				GraphContext.Children.Add(line);
			}

			void SetLineStyle(Line line) {
				line.Stroke = GraphLineColor;
				line.StrokeThickness = 2d;
			}
		}
		private void SetGraphVisible(bool visible) {
			GraphContext.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
		}
		public void UpdateGraph() {
			SetGraphVisible(true);

			PVector2 dGraph01Size = DGraph01Size;
			PRect dGraphRect = DGraphRect;

			//Inside lines
			for (int graphLineI = 0; graphLineI < graphLines.Length; ++graphLineI) {
				float motionValue = GetLineMotionValue(graphLineI);
				float nextMotionValue = GetLineMotionValue(graphLineI + 1);

				Line line = graphLines[graphLineI];
				line.X1 = dGraphRect.xMin + graphLineI * dGraph01Size.x / GraphResolution;
				line.X2 = dGraphRect.xMin + (graphLineI + 1) * dGraph01Size.x / GraphResolution;
				line.Y1 = dGraphRect.yMax - motionValue * dGraph01Size.y;
				line.Y2 = dGraphRect.yMax - nextMotionValue * dGraph01Size.y;

				float GetLineMotionValue(int index) {
					float linearValue = (float)index / graphLines.Length;
					return EditingMotionData.GetMotionValue(linearValue);
				}
			}

			//Outside lines
			float outsideLeftY = NormalToDisplayY(EditingMotionData.GetMotionValue(0f));
			float outsideRightY = NormalToDisplayY(EditingMotionData.GetMotionValue(1f));
			outsideLines[0].SetLinePosition(new Vector2(Mathf.Min(dGraphRect.xMin, 0f), outsideLeftY), new Vector2(dGraphRect.xMin, outsideLeftY));
			outsideLines[1].SetLinePosition(new Vector2(dGraphRect.xMax, outsideRightY), new Vector2(Mathf.Max(dGraphRect.xMax, (float)GraphContext.ActualWidth), outsideRightY));
		}

		//Point
		//TODO : View 함수들을 View클래스 안으로 집어넣자..
		private void CreatePointsFromData(MotionItem motionItem) {

		}
		private void CreatePointWithUpdate(int index, Vector2 cursorPos) {
			CreatePointView(index, cursorPos);

			UpdatePointViews();
			UpdateGraph();

			EditorContext.MarkUnsaved();
		}
		private void CreatePointView(int index, Vector2 cursorPos) {
			MotionPointView prevPointView = EditingMotionItemView.pointList[index - 1];
			MotionPointView nextPointView = EditingMotionItemView.pointList[index];
			float prevNextDeltaX = nextPointView.Data.mainPoint.x - prevPointView.Data.mainPoint.x;
			float prevDeltaX = cursorPos.x - prevPointView.Data.mainPoint.x;
			float nextDeltaX = nextPointView.Data.mainPoint.x - cursorPos.x;

			MotionPointView pointView = new MotionPointView(EditorContext);

			float mainPointX = cursorPos.x;
			float subPoint0X = cursorPos.x - prevDeltaX * 0.5f;
			float subPoint1X = cursorPos.x + nextDeltaX * 0.5f;
			pointView.Data.mainPoint = new PVector2(mainPointX, EditingMotionData.GetMotionValue(mainPointX));
			PVector2 subPoint0 = new PVector2(subPoint0X, EditingMotionData.GetMotionValue(subPoint0X)) - pointView.Data.mainPoint;
			PVector2 subPoint1 = new PVector2(subPoint1X, EditingMotionData.GetMotionValue(subPoint1X)) - pointView.Data.mainPoint;
			//망한 방법이다. 나중에 고치자.
			//Vector2 averageVector = GetAverageVector(subPoint0.ToVector2(), subPoint1.ToVector2());
			//point.subPoints[0] = (averageVector * subPoint0.magnitude).ToPVector2();
			//point.subPoints[1] = (-averageVector * subPoint1.magnitude).ToPVector2();
			pointView.Data.subPoints[0] = subPoint0;
			pointView.Data.subPoints[1] = subPoint1;

			prevPointView.Data.subPoints[1] *= prevDeltaX * 0.5f / prevNextDeltaX;
			nextPointView.Data.subPoints[0] *= nextDeltaX * 0.5f / prevNextDeltaX;

			EditingMotionData.InsertPoint(index, pointView.Data);
		}
		private void RemovePointViewWithUpdate(MotionPointView point) {
			RemovePointView(point);

			UpdatePointViews();
			UpdateGraph();

			EditorContext.MarkUnsaved();
		}
		private void RemovePointView(MotionPointView pointView) {
			EditingMotionItemView.RemovePoint(pointView);
		}
		private void ClearPointViews() {
			PointCanvas.Children.Clear();
		}
		private void UpdatePointViews() {
			for (int pointI = 0; pointI < EditingMotionItemView.pointList.Count; ++pointI) {
				EditingMotionItemView.pointList[pointI].Update();
			}
		}

		//SmartUI
		public void SetSmartFollowText(Vector2 dataPosition) {
			SetSmartFollowText(dataPosition, NormalToDisplay(dataPosition));
		}
		public void SetSmartFollowText(Vector2 dataPosition, Vector2 displayPosition) {
			SmartFollowTextView.Text = $"({dataPosition.x.ToString("0.00")}, {dataPosition.y.ToString("0.00")})";
			if (SmartFollowTextView.ActualWidth == 0f)
				return;

			SmartFollowTextView.SetCanvasPosition(new Vector2(
				displayPosition.x - (float)SmartFollowTextView.ActualWidth * 0.5f,
				displayPosition.y - (float)SmartFollowTextView.ActualHeight * 2.3f));
			SmartFollowTextView.Visibility = Visibility.Visible;
		}
		public void HideSmartFollowText() {
			SmartFollowTextView.Visibility = Visibility.Hidden;
		}

		public void SetSmartLineForX(float x) {
			SmartLineForXView.X1 =
			SmartLineForXView.X2 = NormalToDisplayX(x);
			SmartLineForXView.Y1 = 0d;
			SmartLineForXView.Y2 = SmartContext.ActualHeight;
			SmartLineForXView.Visibility = Visibility.Visible;
		}
		public void SetSmartLineForY(float y) {
			SmartLineForYView.X1 = 0d;
			SmartLineForYView.X2 = SmartContext.ActualWidth;
			SmartLineForYView.Y1 =
			SmartLineForYView.Y2 = NormalToDisplayY(y);
			SmartLineForYView.Visibility = Visibility.Visible;
		}
		public void HideSmartLineForX() {
			SmartLineForXView.Visibility = Visibility.Hidden;
		}
		public void HideSmartLineForY() {
			SmartLineForYView.Visibility = Visibility.Hidden;
		}

		public float? FindMagnetForX(float x, bool findMainPoints, MotionPointView exclusivePoint = null) {
			List<float> magnetList = new List<float>() {
				0f,
				1f,
			};
			if (findMainPoints) {
				foreach (MotionPointView pointView in EditingMotionItemView.pointList) {
					if (pointView != exclusivePoint) {
						magnetList.Add(pointView.Data.mainPoint.x);
					}
				}
			}
			foreach (float magnet in magnetList) {
				if (Mathf.Abs(magnet - x) < NearDistance) {
					//Found magnet
					SetSmartLineForX(magnet);
					return magnet;
				}
			}
			HideSmartLineForX();
			return null;
		}
		public float? FindMagnetForY(float y, bool findMainPoints, MotionPointView exclusivePoint = null) {
			List<float> magnetList = new List<float>() {
				0f,
				1f,
			};
			if (findMainPoints) {
				foreach (MotionPointView point in EditingMotionItemView.pointList) {
					if (point != exclusivePoint) {
						magnetList.Add(point.Data.mainPoint.y);
					}
				}
			}
			foreach (float magnet in magnetList) {
				if (Mathf.Abs(magnet - y) < NearDistance) {
					//Found magnet
					SetSmartLineForY(magnet);
					return magnet;
				}
			}
			GDebug.Log(magnetList.Count);
			HideSmartLineForY();
			return null;
		}

		//Matrix
		public float NormalToDisplayX(float x) {
			return DGraphRect.xMin + x * DGraphRect.Width;
		}
		public float NormalToDisplayY(float y) {
			return DGraphRect.yMax - y * DGraphRect.Height;
		}
		public Vector2 NormalToDisplay(Vector2 normalPoint) {
			return new Vector2(NormalToDisplayX(normalPoint.x), NormalToDisplayY(normalPoint.y));
		}
		public float DisplayToNormalX(float x) {
			return (x - DGraphRect.xMin) / DGraphRect.Width;
		}
		public float DisplayToNormalY(float y) {
			return (DGraphRect.yMax - y) / DGraphRect.Height;
		}
		public Vector2 DisplayToNormal(Vector2 displayPoint) {
			return new Vector2(DisplayToNormalX(displayPoint.x), DisplayToNormalY(displayPoint.y));
		}
		
		private Vector2 GetAverageVector(Vector2 left, Vector2 right) {
			float averageAngle = ((left.x / left.y) + (right.x / right.y)) * 0.5f * Mathf.Rad2Deg;
			float verticalRadian = (averageAngle + 90f) * Mathf.Deg2Rad;

			return new Vector2(Mathf.Cos(verticalRadian), -Mathf.Sin(verticalRadian));
		}

		private void SetCursor(Cursor cursor) {
			if (this.Cursor != cursor) {
				this.Cursor = cursor;
			}
		}

		private float GetMotionTime(float linearTime) {
			return OnEditing ? EditingMotionItemView.Data.Cast<MotionItem>().GetMotionValue(linearTime) : 0f;
		}
	}
}
