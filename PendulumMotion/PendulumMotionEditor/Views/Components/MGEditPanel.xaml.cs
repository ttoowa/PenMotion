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
using GKit;
using PendulumMotion;
using PendulumMotion.Component;
using PendulumMotion.Items;
using PendulumMotion.System;
using PendulumMotionEditor.Views.Items;

namespace PendulumMotionEditor.Views.Components {
	/// <summary>
	/// MGEditPanel.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MGEditPanel : UserControl {

		public bool OnEditing => editingMotion != null;
		public PMMotion editingMotion;

		private const float WidthRatio = 1.777f;
		private static SolidColorBrush GraphLineColor = "B09753".ToBrush();
		private const int GridVerticalCount = 3;
		private const int GridHorizontalCount = 5;
		private const int GraphResolution = 50;
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
			displayZoom = 1f;
			displayOffset = new PVector2();
		}

		public void AttachMotion(PMMotion motion) {
			DetachMotion();
			editingMotion = motion;
			CreatePoints();
			UpdateGrid();
			UpdateGraph();
		}
		public void DetachMotion() {
			editingMotion = null;
			ResetEnv();
			ClearPoints();
		}

		public void UpdateUI() {
			UpdateGrid();
			UpdateGraph();
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
				PMPointView pointView = new PMPointView();
				Canvas.SetLeft(pointView, point.mainPoint.x);
				Canvas.SetTop(pointView, point.mainPoint.y);

				PointContext.Children.Add(pointView);
			}
		}
	}
}
