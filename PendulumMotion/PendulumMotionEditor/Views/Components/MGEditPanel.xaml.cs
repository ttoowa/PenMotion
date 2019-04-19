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

		public bool OnEditing => editingMotion != null;
		public PMMotion editingMotion;

		private const float WidthRatio = 1.777f;
		private static SolidColorBrush GraphLineColor = "B09753".ToBrush();
		private const int GridVerticalCount = 3;
		private const int GridHorizontalCount = 5;
		private const int GraphResolution = 80;
		private Line[] gridVerticals;
		private Line[] gridHorizontals;
		private Line[] graphLines;
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
			Loaded += OnLoaded;
		}
		private void OnLoaded(object sender, RoutedEventArgs e) {
			ResetEnv();
			CreateGrid();
			CreateGraph();
		}
		private void ResetEnv() {
			displayZoom = 0.6f;
			displayOffset = new PVector2();
		}

		public void AttachMotion(PMMotion motion) {
			DetachMotion();
			editingMotion = motion;
			CreatePoints();
			UpdateUI();
		}
		public void DetachMotion() {
			editingMotion = null;
			ResetEnv();
			ClearPoints();
		}

		public void UpdateUI() {
			UpdateGrid();
			UpdateGraph();
			UpdatePoints();
		}

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
				float x = graphRect.xMin + (i * graph01Size.x  / (GridVerticalCount-1));
				line.X1 =
				line.X2 = x;
				line.Y1 = 0d;
				line.Y2 = ActualHeight;
			}
			for (int i = 0; i < gridHorizontals.Length; ++i) {
				Line line = gridHorizontals[i];
				float y = graphRect.yMax - (i * graph01Size.y / (GridHorizontalCount-1));
				line.Y1 =
				line.Y2 = y;
				line.X1 = 0d;
				line.X2 = ActualWidth;
			}
		}

		private void CreateGraph() {
			graphLines = new Line[GraphResolution];
			for(int i=0; i<graphLines.Length; ++i) {
				Line line = graphLines[i] = new Line();
				line.Stroke = GraphLineColor;
				line.StrokeThickness = 2d;

				GraphContext.Children.Add(line);
			}
		}
		private void UpdateGraph() {
			PVector2 graph01Size = DGraph01Size;
			PRect graphRect = DGraphRect;

			for (int i = 0; i < graphLines.Length; ++i) {
				float motionValue = GetMotionValue(i);
				float nextMotionValue = GetMotionValue(i + 1);


				Line line = graphLines[i];
				line.X1 = graphRect.xMin + i * graph01Size.x / GraphResolution;
				line.X2 = graphRect.xMin + (i + 1) * graph01Size.x / GraphResolution;
				line.Y1 = graphRect.yMax - motionValue * graph01Size.y;
				line.Y2 = graphRect.yMax - nextMotionValue * graph01Size.y;


				float GetMotionValue(int index) {
					float linearValue = (float)index / (graphLines.Length - 1);
					return editingMotion.GetMotionValue(linearValue);
				}
			}
		}

		private void ClearPoints() {
			PointContext.Children.Clear();
		}
		private void CreatePoints() {
			pointViewList = new List<PMPointView>();
			
			for (int pointI = 0; pointI < editingMotion.pointList.Count; ++pointI) {
				PMPoint point = editingMotion.pointList[pointI];
				if(point.view == null) {
					PMPointView pointView = new PMPointView();
					point.view = pointView;

					RegisterPointEvent(point, pointView);
				}
				PointContext.Children.Add(point.view.Cast<PMPointView>());

			}

			void RegisterPointEvent(PMPoint point, PMPointView pointView) {
				Vector2 cursorOffset = new Vector2();

				pointView.MainHandleView.MouseDown += OnMouseDown_PointMainHandle;
				for(int subI = 0; subI < pointView.SubHandleViews.Length; ++subI) {
					int subHandleIndex = subI;
					Grid subHandleView = pointView.SubHandleViews[subI];
					subHandleView.MouseDown += OnMouseDown_PointSubHandle;

					void OnMouseDown_PointSubHandle(object sender, MouseButtonEventArgs e) {
						cursorOffset = GetCursorOffset(pointView, subHandleView) + new Vector2(PMPointView.SubHandleWidthHalf, PMPointView.SubHandleWidthHalf);
						LoopEngine.AddLoopAction(OnDrag_PointSubHandle, GLoopCycle.EveryFrame, GWhen.MouseUpRemove);
					}
					void OnDrag_PointSubHandle() {
						//PointView
						Vector2 cursorPos = MouseInput.GetRelativePosition(PointContext) + cursorOffset;
						Vector2 pointViewPos = pointView.GetCanvasPosition();
						point.subPoints[subHandleIndex] = DisplayToNormal(cursorPos).ToPVector2() - point.mainPoint;

						UpdateGraph();
						UpdatePoint(point);
					}
				}

				void OnMouseDown_PointMainHandle(object sender, MouseButtonEventArgs e) {
					cursorOffset = GetCursorOffset(PointContext, pointView);
					LoopEngine.AddLoopAction(OnDrag_PointMainHandle, GLoopCycle.EveryFrame, GWhen.MouseUpRemove);
				}
				void OnDrag_PointMainHandle() {
					Vector2 cursorPos = MouseInput.GetRelativePosition(PointContext) + cursorOffset;

					point.mainPoint = DisplayToNormal(cursorPos).ToPVector2();

					UpdateGraph();
					UpdatePoint(point);
				}
			}
		}
		private void UpdatePoints() {
			for (int pointI = 0; pointI < editingMotion.pointList.Count; ++pointI) {
				UpdatePoint(editingMotion.pointList[pointI]);
			}
		}
		private void UpdatePoint(PMPoint point) {
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
	}
}
