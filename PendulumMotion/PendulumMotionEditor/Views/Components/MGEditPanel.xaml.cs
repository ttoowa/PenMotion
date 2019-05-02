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
using PendulumMotion.Component;
using PendulumMotion.Items;
using PendulumMotion.System;
using PendulumMotionEditor.Views.Items;
using PendulumMotionEditor.Views.Windows;
using GKit;

namespace PendulumMotionEditor.Views.Components {
	/// <summary>
	/// MGEditPanel.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MGEditPanel : UserControl {
		private static Root Root => Root.Instance;
		private static MainWindow MainWindow => Root.mainWindow;
		private static GLoopEngine LoopEngine => Root.loopEngine;
		private static CursorStorage CursorStorage => Root.cursorStorage;
		private static EditableMotionFile EditingFile => MainWindow.editingFile;

		public bool OnEditing => editingMotion != null;
		public PMMotion editingMotion;

		private const float WidthRatio = 1.777f;
		private static SolidColorBrush GraphLineColor = "B09753".ToBrush();
		private const int GridVerticalCount = 3;
		private const int GridHorizontalCount = 5;
		private const int GraphResolution = 120;
		private const float NearDistance = 0.016f;
		private Line[] gridVerticals;
		private Line[] gridHorizontals;
		private Line[] graphLines;
		private Line[] outsideLines;
		private List<PMPointView> pointViewList;

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

		public MGEditPanel() {
			InitializeComponent();
			if (this.CheckDesignMode())
				return;
			Loaded += OnLoaded;
		}
		private void OnLoaded(object sender, RoutedEventArgs e) {
			Init();
			RegisterEvent();
			ResetEnv();
			CreateGrid();
			CreateGraph();
			DetachMotion();

			void Init() {
				HideSmartFollowText();
				HideSmartLineForX();
				HideSmartLineForY();
			}
			void RegisterEvent() {
				LoopEngine.AddLoopAction(OnTick);
			}
		}
		private void OnTick() {
			CheckCursorInteraction();
		}
		private void OnDataChanged() {
			EditingFile.MarkUnsaved();
			MainWindow.UpdatePreviewContinuum();
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
			PMPoint cursorOverPoint;
			int cursorOverPointIndex = -1;
			FindCursorOverPoint(out cursorOverPoint, out cursorOverPointIndex);
			
			if (KeyInput.GetKeyHold(WinKey.LeftAlt)) {
				//RemoveTest
				if (cursorOverPoint != null && cursorOverPointIndex > 0 && cursorOverPointIndex < editingMotion.pointList.Count - 1) {
					SetCursor(CursorStorage.cursor_remove);
					cursorChanged = true;

					if (MouseInput.LeftDown) {
						RemovePoint(cursorOverPoint);

						UpdatePointViews();
						UpdateGraph();
						EditingFile.MarkUnsaved();
					}
				}
			} else if(KeyInput.GetKeyHold(WinKey.LeftControl) && cursorOverPoint == null) {
				//AddTest
				Vector2 cursorPos = DisplayToNormal(MouseInput.GetRelativePosition(PointContext));
				if (cursorPos.x > 0f && cursorPos.x < 1f) {
					int rightIndex = editingMotion.GetRightIndex(cursorPos.x);
					if (rightIndex > 0) {
						SetCursor(CursorStorage.cursor_add);
						SetSmartLineForX(cursorPos.x);
						cursorChanged = true;

						if (MouseInput.LeftDown) {
							CreatePoint(cursorPos, rightIndex);

							UpdatePointViews();
							UpdateGraph();
							EditingFile.MarkUnsaved();
						}
					}
				} else {
					HideSmartLineForX();
				}
			}
			if(KeyInput.GetKeyUp(WinKey.LeftControl) && !MouseInput.LeftHold) {
				HideSmartLineForX();
			}
			if (!cursorChanged) {
				SetCursor(CursorStorage.cursor_default);
			}

			
		}
		private void FindCursorOverPoint(out PMPoint cursorOverPoint, out int cursorOverIndex) {
			cursorOverPoint = null;
			cursorOverIndex = -1;
			for (int handleI = 0; handleI < editingMotion.pointList.Count; ++handleI) {
				PMPoint point = editingMotion.pointList[handleI];
				if (point.view.Cast<PMPointView>().MainHandleView.IsMouseOver) {
					cursorOverIndex = handleI;
					cursorOverPoint = point;
				}
			}
		}

		public void AttachMotion(PMMotion motion) {
			DetachMotion();
			editingMotion = motion;
			CreatePointViews();
			UpdateUI();
			PreviewContext.Visibility = Visibility.Visible;
			MainWindow.SetPreviewContinuumVisible(true);
			MainWindow.UpdatePreviewContinuum();
		}
		public void DetachMotion() {
			editingMotion = null;
			ResetEnv();
			ClearPointViews();
			SetGraphVisible(false);
			HideSmartFollowText();
			HideSmartLineForX();
			HideSmartLineForY();
			PreviewContext.Visibility = Visibility.Collapsed;
			MainWindow.SetPreviewContinuumVisible(false);
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

			gridVerticals = new Line[GridVerticalCount];
			gridHorizontals = new Line[GridHorizontalCount];

			for (int i = 0; i < gridVerticals.Length; ++i) {
				Line line = gridVerticals[i] = new Line();
				line.Stroke = gridColor;
				line.StrokeThickness = 1d;
				GridContext.Children.Add(line);
			}
			for (int i = 0; i < gridHorizontals.Length; ++i) {
				Line line = gridHorizontals[i] = new Line();
				line.Stroke = gridColor;
				line.StrokeThickness = 1d;
				GridContext.Children.Add(line);
			}
		}
		private void UpdateGrid() {
			PRect graphRect = DGraphRect;
			PVector2 graph01Size = DGraph01Size;

			for (int i = 0; i < gridVerticals.Length; ++i) {
				Line line = gridVerticals[i];
				float x = graphRect.xMin + (i * graph01Size.x / (GridVerticalCount - 1));
				line.X1 =
				line.X2 = x;
				line.Y1 = 0d;
				line.Y2 = ActualHeight;
			}
			for (int i = 0; i < gridHorizontals.Length; ++i) {
				Line line = gridHorizontals[i];
				float y = graphRect.yMax - (i * graph01Size.y / (GridHorizontalCount - 1));
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
		private void UpdateGraph() {
			SetGraphVisible(true);

			PVector2 dGraph01Size = DGraph01Size;
			PRect dGraphRect = DGraphRect;

			for (int graphLineI = 0; graphLineI < graphLines.Length; ++graphLineI) {
				float motionValue = GetMotionValue(graphLineI);
				float nextMotionValue = GetMotionValue(graphLineI + 1);

				Line line = graphLines[graphLineI];
				line.X1 = dGraphRect.xMin + graphLineI * dGraph01Size.x / GraphResolution;
				line.X2 = dGraphRect.xMin + (graphLineI + 1) * dGraph01Size.x / GraphResolution;
				line.Y1 = dGraphRect.yMax - motionValue * dGraph01Size.y;
				line.Y2 = dGraphRect.yMax - nextMotionValue * dGraph01Size.y;

				float GetMotionValue(int index) {
					float linearValue = (float)index / graphLines.Length;
					return editingMotion.GetMotionValue(linearValue);
				}
			}

			float outsideLeftY = NormalToDisplayY(editingMotion.GetMotionValue(0f));
			float outsideRightY = NormalToDisplayY(editingMotion.GetMotionValue(1f));
			outsideLines[0].SetLinePosition(new Vector2(Mathf.Min(dGraphRect.xMin, 0f), outsideLeftY), new Vector2(dGraphRect.xMin, outsideLeftY));
			outsideLines[1].SetLinePosition(new Vector2(dGraphRect.xMax, outsideRightY), new Vector2(Mathf.Max(dGraphRect.xMax, (float)GraphContext.ActualWidth), outsideRightY));
		}

		//Point
		private void CreatePoint(Vector2 cursorPos, int rightIndex) {
			PMPoint prevPoint = editingMotion.pointList[rightIndex - 1];
			PMPoint nextPoint = editingMotion.pointList[rightIndex];
			float prevNextDeltaX = nextPoint.mainPoint.x - prevPoint.mainPoint.x;
			float prevDeltaX = cursorPos.x - prevPoint.mainPoint.x;
			float nextDeltaX = nextPoint.mainPoint.x - cursorPos.x;
			PMPoint point = new PMPoint();

			float mainPointX = cursorPos.x;
			float subPoint0X = cursorPos.x - prevDeltaX * 0.5f;
			float subPoint1X = cursorPos.x + nextDeltaX * 0.5f;
			point.mainPoint = new PVector2(mainPointX, editingMotion.GetMotionValue(mainPointX));
			PVector2 subPoint0 = new PVector2(subPoint0X, editingMotion.GetMotionValue(subPoint0X)) - point.mainPoint;
			PVector2 subPoint1 = new PVector2(subPoint1X, editingMotion.GetMotionValue(subPoint1X)) - point.mainPoint;
			//망한 방법이다. 나중에 고치자.
			//Vector2 averageVector = GetAverageVector(subPoint0.ToVector2(), subPoint1.ToVector2());
			//point.subPoints[0] = (averageVector * subPoint0.magnitude).ToPVector2();
			//point.subPoints[1] = (-averageVector * subPoint1.magnitude).ToPVector2();
			point.subPoints[0] = subPoint0;
			point.subPoints[1] = subPoint1;

			prevPoint.subPoints[1] *= prevDeltaX * 0.5f / prevNextDeltaX;
			nextPoint.subPoints[0] *= nextDeltaX * 0.5f / prevNextDeltaX;

			editingMotion.InsertPoint(point, rightIndex);

			CreatePointView(point);
		}
		private void RemovePoint(PMPoint point) {
			RemovePointView(point);
			editingMotion.RemovePoint(point);
		}
		private void CreatePointViews() {
			pointViewList = new List<PMPointView>();

			for (int pointI = 0; pointI < editingMotion.pointList.Count; ++pointI) {
				PMPoint point = editingMotion.pointList[pointI];

				CreatePointView(point);
			}
		}
		private void CreatePointView(PMPoint point) {
			if (point.view == null) {
				PMPointView pointView = new PMPointView();
				point.view = pointView;

				RegisterPointEvent(pointView);
			}
			PointContext.Children.Add(point.view.Cast<PMPointView>());

			void RegisterPointEvent(PMPointView pointView) {
				Vector2 cursorOffset = new Vector2();

				pointView.MainHandleView.MouseDown += OnMouseDown_PointMainHandle;
				for (int subI = 0; subI < pointView.SubHandleViews.Length; ++subI) {
					int subHandleIndex = subI;
					Grid subHandleView = pointView.SubHandleViews[subI];
					subHandleView.MouseDown += OnMouseDown_PointSubHandle;

					void OnMouseDown_PointSubHandle(object sender, MouseButtonEventArgs e) {
						cursorOffset = GetCursorOffset(pointView, subHandleView) + new Vector2(PMPointView.SubHandleWidthHalf, PMPointView.SubHandleWidthHalf);
						LoopEngine.AddLoopAction(OnDrag_PointSubHandle, GLoopCycle.EveryFrame, GWhen.MouseUpRemove);
						LoopEngine.AddLoopAction(OnMouseUp_PointSubHandle, GLoopCycle.None, GWhen.MouseUpRemove);
					}
					void OnDrag_PointSubHandle() {
						Vector2 cursorPos = MouseInput.GetRelativePosition(PointContext) + cursorOffset;
						Vector2 pointPosAbsolute = DisplayToNormal(cursorPos);
						FilterMagnet();
						Vector2 pointPosRelative = pointPosAbsolute - point.mainPoint.ToVector2();

						point.subPoints[subHandleIndex] = pointPosRelative.ToPVector2();

						UpdateGraph();
						UpdatePointView(point);
						editingMotion.view.Cast<PMItemView>().UpdatePreviewGraph(editingMotion);

						SetSmartFollowText(pointPosRelative, NormalToDisplay(pointPosAbsolute));
						OnDataChanged();

						void FilterMagnet() {
							float? result = null;
							result = FindMagnetForY(pointPosAbsolute.y, false, point);
							if (result.HasValue) {
								pointPosAbsolute.y = result.Value;
							}
							result = FindMagnetForX(pointPosAbsolute.x, false, point);
							if (result.HasValue) {
								pointPosAbsolute.x = result.Value;
							}
						}
					}
					void OnMouseUp_PointSubHandle() {
						HideSmartFollowText();
						HideSmartLineForX();
						HideSmartLineForY();
					}
				}

				void OnMouseDown_PointMainHandle(object sender, MouseButtonEventArgs e) {
					cursorOffset = GetCursorOffset(PointContext, pointView);
					LoopEngine.AddLoopAction(OnDrag_PointMainHandle, GLoopCycle.EveryFrame, GWhen.MouseUpRemove);
					LoopEngine.AddLoopAction(OnMouseUp_PointMainHandle, GLoopCycle.None, GWhen.MouseUpRemove);
				}
				void OnDrag_PointMainHandle() {
					Vector2 cursorPos = MouseInput.GetRelativePosition(PointContext) + cursorOffset;
					Vector2 pointPos = DisplayToNormal(cursorPos);
					FilterMagnet();

					point.mainPoint = pointPos.ToPVector2();

					UpdateGraph();
					UpdatePointView(point);
					editingMotion.view.Cast<PMItemView>().UpdatePreviewGraph(editingMotion);

					SetSmartFollowText(pointPos);
					OnDataChanged();

					void FilterMagnet() {
						float? result = null;
						result = FindMagnetForY(pointPos.y, true, point);
						if (result.HasValue) {
							pointPos.y = result.Value;
						}
						result = FindMagnetForX(pointPos.x, true, point);
						if (result.HasValue) {
							pointPos.x = result.Value;
						}
					}
				}
				void OnMouseUp_PointMainHandle() {
					HideSmartFollowText();
					HideSmartLineForX();
					HideSmartLineForY();
				}
			}
		}
		private void ClearPointViews() {
			PointContext.Children.Clear();
		}
		private void RemovePointView(PMPoint point) {
			PointContext.Children.Remove(point.view as PMPointView);
		}
		private void UpdatePointViews() {
			for (int pointI = 0; pointI < editingMotion.pointList.Count; ++pointI) {
				UpdatePointView(editingMotion.pointList[pointI]);
			}
		}
		private void UpdatePointView(PMPoint point) {
			PMPointView pointView = point.view.Cast<PMPointView>();
			Vector2 dPointPos = NormalToDisplay(point.mainPoint.ToVector2());
			pointView.SetCanvasPosition(dPointPos);

			for (int subI = 0; subI < point.subPoints.Length; ++subI) {
				Grid subHandleView = pointView.SubHandleViews[subI];
				Line subLineView = pointView.SubLineViews[subI];

				Vector2 dSubPoint = NormalToDisplay((point.subPoints[subI] + point.mainPoint).ToVector2()) - dPointPos;
				subHandleView.SetCanvasPosition(dSubPoint - new Vector2(PMPointView.SubHandleWidthHalf, PMPointView.SubHandleWidthHalf));
				subLineView.SetLinePosition(new Vector2(), dSubPoint);

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

		private float? FindMagnetForX(float x, bool findPoints, PMPoint exclusivePoint) {
			List<float> magnetList = new List<float>() {
				0f,
				1f,
			};
			if (findPoints) {
				foreach (PMPoint point in editingMotion.pointList) {
					if (point != exclusivePoint) {
						magnetList.Add(point.mainPoint.x);
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
		private float? FindMagnetForY(float y, bool findPoints, PMPoint exclusivePoint) {
			List<float> magnetList = new List<float>() {
				0f,
				1f,
			};
			if (findPoints) {
				foreach (PMPoint point in editingMotion.pointList) {
					if (point != exclusivePoint) {
						magnetList.Add(point.mainPoint.y);
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
		private Vector2 GetCursorOffset(Visual context, UIElement element) {
			return new Vector2((float)Canvas.GetLeft(element), (float)Canvas.GetTop(element)) - MouseInput.GetRelativePosition(context);
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
	}
}
