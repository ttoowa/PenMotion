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
using PenMotion;
using PenMotion.Datas;
using PenMotion.Datas.Items;
using PenMotion.Datas.Items.Elements;
using PenMotion.System;
using PenMotionEditor.UI.Items;
using PenMotionEditor.UI.Items.Elements;
using PenMotionEditor.UI.Tabs;
using PenMotionEditor.UI.Windows;
using GKit;
using GKit.WPF;

namespace PenMotionEditor.UI.Tabs {
/// <summary>
/// MotionItem을 Attach할 때 MotionPointView들을 생성하고, Detach할 때 MotionPointView들을 제거합니다.
/// </summary>
	public partial class GraphEditorTab : UserControl {
		private EditorContext EditorContext;
		private GLoopEngine LoopEngine => EditorContext.LoopEngine;
		private PreviewTab PreviewTab => EditorContext.PreviewTab;
		private MotionTab MotionTab => EditorContext.MotionTab;
		private CursorStorage CursorStorage => EditorContext.CursorStorage;

		private const float WidthRatio = 1.777f;
		private static SolidColorBrush GraphLineColor = "B09753".ToBrush();

		//Datas
		public bool OnEditing => EditingMotionData != null;
		public MotionItem EditingMotionData {
			get; private set;
		}

		private List<MotionPointView> pointViewList;
		private Dictionary<MotionPoint, MotionPointView> dataToViewDict;

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
			pointViewList = new List<MotionPointView>();
			dataToViewDict = new Dictionary<MotionPoint, MotionPointView>();
		}
		private void InitUI() {
			CreateGrid();
			CreateGraph();

			DetachMotion();
		}
		private void RegisterEvents() {
			LoopEngine.AddLoopAction(OnTick);

			SizeChanged += OnSizeChanged;
			GridContext.MouseDown += BackPanel_MouseDown;
		}
		private void ResetEnv() {
			displayZoom = 0.6f;
			displayOffset = new PVector2();
		}

		//Events
		private void OnTick() {
			CheckCursorInteraction();
		}
		private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
			UpdateUiAll();
		}

		private void BackPanel_MouseDown(object sender, MouseButtonEventArgs e) {
			Keyboard.ClearFocus();
			if (KeyInput.GetKeyHold(WinKey.Space)) {
				if (KeyInput.GetKeyHold(WinKey.LeftControl) || KeyInput.GetKeyHold(WinKey.RightControl)) {
					LoopEngine.AddLoopAction(BackPanel_MouseDrag_ForZoom, GLoopCycle.EveryFrame, GWhen.MouseUpRemove);
				} else {
					LoopEngine.AddLoopAction(BackPanel_MouseDrag_ForPanning, GLoopCycle.EveryFrame, GWhen.MouseUpRemove);
				}
				cursorPosMemory = MouseInput.AbsolutePosition;
			}
		}
		private void BackPanel_MouseDrag_ForPanning() {
			const float MaxOffset = 320f;

			Vector2 cursorDelta = MouseInput.AbsolutePosition - cursorPosMemory;
			cursorPosMemory = MouseInput.AbsolutePosition;

			displayOffset += cursorDelta.ToPVector2() / displayZoom * 0.5f;
			displayOffset = BMath.Clamp(displayOffset.ToVector2(), -MaxOffset, MaxOffset).ToPVector2();

			UpdateUiAll();
		}
		private void BackPanel_MouseDrag_ForZoom() {
			const float MinZoom = 0.3f;
			const float MaxZoom = 1.5f;

			Vector2 cursorDelta = MouseInput.AbsolutePosition - cursorPosMemory;
			cursorPosMemory = MouseInput.AbsolutePosition;
			displayZoom += cursorDelta.x * 0.001f;
			displayZoom = Mathf.Clamp(displayZoom, MinZoom, MaxZoom);

			UpdateUiAll();
		}

		private void MotionItem_PointInserted(int index, MotionPoint point) {
			MotionPointView view = CreatePointViewFromData(point);

			//Add to collection
			pointViewList.Insert(index, view);
			dataToViewDict.Add(point, view);

			//Update UI
			UpdateGraphLine();

			EditorContext.MarkUnsaved();
		}
		private void MotionItem_PointRemoved(MotionPoint point) {

			MotionPointView view = dataToViewDict[point];
			RemovePointView(view);

			//Remove from collection
			pointViewList.Remove(view);
			dataToViewDict.Remove(point);

			view.Dispose();

			//Update UI
			UpdateGraphLine();

			EditorContext.MarkUnsaved();

		}

		//MotionItem
		public void AttachMotion(MotionItem motionItem) {
			DetachMotion();
			EditingMotionData = motionItem;

			//Register events
			motionItem.PointInserted += MotionItem_PointInserted;
			motionItem.PointRemoved += MotionItem_PointRemoved;

			//Load data
			for (int i = 0; i < motionItem.pointList.Count; ++i) {
				MotionItem_PointInserted(i, motionItem.pointList[i]);
			}

			UpdateUiAll();

			//Update preview
			PreviewContext.Visibility = Visibility.Visible;
			PreviewTab.SetPositionContinuumVisible(true);
			PreviewTab.UpdatePositionContinuum();
		}
		public void DetachMotion() {

			ResetEnv();
			ClearPointViews();

			//Unregister events
			if(OnEditing) {
				EditingMotionData.PointInserted -= MotionItem_PointInserted;
				EditingMotionData.PointRemoved -= MotionItem_PointRemoved;
			}

			//Update UI
			SetGraphVisible(false);
			HideSmartFollowText();
			HideSmartLineForX();
			HideSmartLineForY();
			PreviewContext.Visibility = Visibility.Collapsed;

			EditingMotionData = null;

			PreviewTab.SetPositionContinuumVisible(false);
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
		public void UpdateGraphLine() {
			SetGraphVisible(true);

			PVector2 dGraph01Size = DGraph01Size;
			PRect dGraphRect = DGraphRect;

			//Update Inside lines
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

			//Update Outside lines
			float outsideLeftY = NormalToDisplayY(EditingMotionData.GetMotionValue(0f));
			float outsideRightY = NormalToDisplayY(EditingMotionData.GetMotionValue(1f));
			outsideLines[0].SetLinePosition(new Vector2(Mathf.Min(dGraphRect.xMin, 0f), outsideLeftY), new Vector2(dGraphRect.xMin, outsideLeftY));
			outsideLines[1].SetLinePosition(new Vector2(dGraphRect.xMax, outsideRightY), new Vector2(Mathf.Max(dGraphRect.xMax, (float)GraphContext.ActualWidth), outsideRightY));
		}

		//PointViews
		private MotionPointView CreatePointViewFromData(MotionPoint motionPoint) {
			MotionPointView view = new MotionPointView(EditorContext, motionPoint);
			PointCanvas.Children.Add(view);

			//MainPoint
			view.Data_MainPointChanged(motionPoint.MainPoint);

			//SubPoints
			for(int i=0; i<motionPoint.SubPoints.Length; ++i) {
				view.Data_SubPointChanged(i, motionPoint.SubPoints[i]);
			}

			return view;
		}
		private void RemovePointView(MotionPointView motionPointView) {
			PointCanvas.Children.Remove(motionPointView);
		}
		private void ClearPointViews() {
			foreach(MotionPointView pointView in pointViewList) {
				pointView.Dispose();
			}

			PointCanvas.Children.Clear();
			dataToViewDict.Clear();
		}

		//Points // 이거 MotionItem 으로 옮기자
		private void CreatePointWithInterpolation(int index, Vector2 position) {
			//Collect prev, next points
			MotionPointView prevPointView = pointViewList[index - 1];
			MotionPointView nextPointView = pointViewList[index];
			float prevNextDeltaX = nextPointView.Data.MainPoint.x - prevPointView.Data.MainPoint.x;
			float prevDeltaX = position.x - prevPointView.Data.MainPoint.x;
			float nextDeltaX = nextPointView.Data.MainPoint.x - position.x;

			MotionPoint point = new MotionPoint();

			//Set data
			float mainPointX = position.x;
			float subPoint0X = position.x - prevDeltaX * 0.5f;
			float subPoint1X = position.x + nextDeltaX * 0.5f;
			PVector2 subPoint0 = new PVector2(subPoint0X, EditingMotionData.GetMotionValue(subPoint0X)) - point.MainPoint;
			PVector2 subPoint1 = new PVector2(subPoint1X, EditingMotionData.GetMotionValue(subPoint1X)) - point.MainPoint;
			point.SetSubPoint(0, subPoint0);
			point.SetSubPoint(1, subPoint1);

			//망한 방법이다. 나중에 고치자.
			//Vector2 averageVector = GetAverageVector(subPoint0.ToVector2(), subPoint1.ToVector2());
			//point.subPoints[0] = (averageVector * subPoint0.magnitude).ToPVector2();
			//point.subPoints[1] = (-averageVector * subPoint1.magnitude).ToPVector2();

			//Apply
			point.SetMainPoint(new PVector2(mainPointX, EditingMotionData.GetMotionValue(mainPointX)));
			prevPointView.Data.SubPoints[1] *= prevDeltaX * 0.5f / prevNextDeltaX;
			nextPointView.Data.SubPoints[0] *= nextDeltaX * 0.5f / prevNextDeltaX;

			EditingMotionData.InsertPoint(index, point);
		}

		//UI
		public void UpdateUiAll() {
			if (!OnEditing)
				return;

			UpdateGrid();
			UpdateGraphLine();
			UpdateAllPoints();
		}
		public void UpdatePlaybackRadar(float time, float maxOverTime) {
			PlaybackRadar.Height = PreviewContext.ActualHeight;

			PRect dGraphRect = DGraphRect;
			float x = dGraphRect.xMin + dGraphRect.Width * time;
			float overTime = time > 0f ? Mathf.Max(0f, time - 1f) : -time;
			PlaybackRadar.Opacity = 1f - overTime / maxOverTime;
			Canvas.SetLeft(PlaybackRadar, x - PlaybackRadar.Width);
		}
		private void UpdateAllPoints() {
			foreach (MotionPointView view in pointViewList) {
				view.Update();
			}
		}

		private void CheckCursorInteraction() {
			//코드 꼴이 말이 아니다. 으아아악
			//언젠가 리팩토링 할 것.

			const float InteractionThreshold = 0.02f;

			if (!OnEditing)
				return;

			bool cursorChanged = false;
			MotionPointView cursorOverPoint;
			int cursorOverPointIndex = -1;
			FindCursorOverPoint(out cursorOverPoint, out cursorOverPointIndex);

			if (KeyInput.GetKeyHold(WinKey.LeftAlt)) {
				//Remove mode
				if (cursorOverPoint != null && cursorOverPointIndex > 0 && cursorOverPointIndex < pointViewList.Count - 1) {
					SetCursor(CursorStorage.cursor_remove);
					cursorChanged = true;

					if (MouseInput.Left.Down) {
						EditingMotionData.RemovePoint(cursorOverPoint.Data);
					}
				}
			} else if (KeyInput.GetKeyHold(WinKey.LeftControl) && cursorOverPoint == null) {
				Vector2 cursorPos = DisplayToNormal(MouseInput.GetRelativePosition(PointCanvas));
				if (!KeyInput.GetKeyHold(WinKey.Space) && cursorPos.x > 0f && cursorPos.x < 1f) {
					//Add mode

					int index = EditingMotionData.GetRightPointIndex(cursorPos.x);
					if (index > 0) {
						SetCursor(CursorStorage.cursor_add);
						SetSmartLineForX(cursorPos.x);
						cursorChanged = true;

						if (MouseInput.Left.Down) {
							CreatePointWithInterpolation(index, cursorPos);
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
			for (int handleI = 0; handleI < pointViewList.Count; ++handleI) {
				MotionPointView pointView = pointViewList[handleI];

				if (pointView.MainHandleView.IsMouseOver) {
					cursorOverIndex = handleI;
					cursorOverPoint = pointView;
					break;
				}
			}
		}

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
				foreach (MotionPointView pointView in pointViewList) {
					if (pointView != exclusivePoint) {
						magnetList.Add(pointView.Data.MainPoint.x);
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
				foreach (MotionPointView point in pointViewList) {
					if (point != exclusivePoint) {
						magnetList.Add(point.Data.MainPoint.y);
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
			return OnEditing ? EditingMotionData.Cast<MotionItem>().GetMotionValue(linearTime) : 0f;
		}
	}
}
