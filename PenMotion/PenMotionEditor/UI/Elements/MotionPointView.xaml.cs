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
using GKit.WPF;
using PenMotion.Datas.Items.Elements;
using PenMotion.System;
using PenMotionEditor.UI.Tabs;

namespace PenMotionEditor.UI.Elements
{
	/// <summary>
	/// MGHandle.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MotionPointView : UserControl, IDisposable
	{
		private EditorContext EditorContext;
		private GraphEditorTab GraphEditorTab => EditorContext.GraphEditorTab;
		private MotionTab MotionTab => EditorContext.MotionTab;
		private PreviewTab PreviewTab => EditorContext.PreviewTab;
		private GLoopEngine LoopEngine => EditorContext.LoopEngine;

		public const float DefaultSubOffset = 0.3f;
		public const float MainHandleWidth = 20f;
		public const float MainHandleWidthHalf = MainHandleWidth * 0.5f;
		public const float SubHandleWidth = 14f;
		public const float SubHandleWidthHalf = SubHandleWidth * 0.5f;

		private const float MaxHandleRange = 2f;

		public MotionPoint Data {
			get; private set;
		}

		public Grid MainHandleView {
			get; private set;
		}
		public Grid[] SubHandleViews {
			get; private set;
		}
		public Line[] SubLineViews {
			get; private set;
		}

		//Event memory
		private Vector2 cursorOffset;

		public MotionPointView() {
			//For designer
			InitializeComponent();
		}
		public MotionPointView(EditorContext editorContext, MotionPoint data)
		{
			InitializeComponent();

			this.EditorContext = editorContext;
			this.Data = data;

			InitViews();
			RegisterEvent();
		}
		private void InitViews() {
			MainHandleView = this.MainHandle;
			SubHandleViews = new Grid[] {
				SubHandle0,
				SubHandle1,
			};
			SubLineViews = new Line[] {
				SubLine0,
				SubLine1,
			};
		}
		private void RegisterEvent() {
			//Button reaction
			SetButtonReaction(MainHandleView);
			for (int subI = 0; subI < SubHandleViews.Length; ++subI) {
				SetButtonReaction(SubHandleViews[subI]);
			}

			//Mouse events
			MainHandleView.MouseDown += MainHandleView_MouseDown;
			MainHandleView.MouseMove += MainHandleView_MouseMove;
			MainHandleView.MouseUp += MainHandleView_MouseUp;

			for (int subI = 0; subI < SubHandleViews.Length; ++subI) {
				int subHandleIndex = subI;

				Grid subHandleView = SubHandleViews[subI]; 
				subHandleView.MouseDown += (object sender, MouseButtonEventArgs e) => {
					SubHandle_MouseDown(sender, e, subHandleIndex);
				};
				subHandleView.MouseMove += (object sender, MouseEventArgs e) => {
					SubHandle_MouseMove(sender, e, subHandleIndex);
				};
				subHandleView.MouseUp += (object sender, MouseButtonEventArgs e) => {
					SubHandle_MouseUp(sender, e, subHandleIndex);
				};
			}

			//Data changed
			Data.MainPointChanged += Data_MainPointChanged;
			Data.SubPointChanged += Data_SubPointChanged;
		}

		public void Dispose() {
			Data.MainPointChanged -= Data_MainPointChanged;
			Data.SubPointChanged -= Data_SubPointChanged;
		}

		//Events
		//PointChanged
		internal void Data_MainPointChanged(PVector2 position) {
			UpdateWithOtherUI();
		}
		internal void Data_SubPointChanged(int index, PVector2 position) {
			UpdateWithOtherUI();
		}

		private void SetButtonReaction(Grid button) {
			button.SetButtonReaction((Shape)button.Children[button.Children.Count - 1], 0.3f);
		}

		private void MainHandleView_MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton != MouseButton.Left)
				return;

			cursorOffset = new Vector2(MainHandleWidthHalf, MainHandleWidthHalf) - (Vector2)e.GetPosition(MainHandle);

			StartDragging(MainHandleView);
		}
		private void MainHandleView_MouseMove(object sender, MouseEventArgs e) {
			if (GraphEditorTab.DraggingElement != MainHandleView)
				return;

			Vector2 cursorPos = (Vector2)e.GetPosition(GraphEditorTab.PointCanvas) + cursorOffset;
			Vector2 pointPos = GraphEditorTab.DisplayToNormal(cursorPos);
			pointPos = BMath.Clamp(pointPos, -MaxHandleRange + 1f, MaxHandleRange);

			ApplyMagnet();

			Data.SetMainPoint(pointPos.ToPVector2());

			GraphEditorTab.SetSmartFollowText(pointPos);

			void ApplyMagnet() {
				float? result = null;
				result = GraphEditorTab.FindMagnetForY(pointPos.y, true, this);
				if (result.HasValue) {
					pointPos.y = result.Value;
				}
				result = GraphEditorTab.FindMagnetForX(pointPos.x, true, this);
				if (result.HasValue) {
					pointPos.x = result.Value;
				}
			}
		}
		private void MainHandleView_MouseUp(object sender, MouseButtonEventArgs e) {
			if (GraphEditorTab.DraggingElement != MainHandleView)
				return;

			GraphEditorTab.HideSmartFollowText();
			GraphEditorTab.HideSmartLineForX();
			GraphEditorTab.HideSmartLineForY();

			EndDragging();
		}

		private void SubHandle_MouseDown(object sender, MouseButtonEventArgs e, int index) {
			if (e.ChangedButton != MouseButton.Left)
				return;

			//cursorOffset = this.GetCanvasPosition() - (Vector2)e.GetPosition(SubHandleViews[index]) + new Vector2(MotionPointView.SubHandleWidthHalf, MotionPointView.SubHandleWidthHalf);
			cursorOffset = -(Vector2)e.GetPosition(SubHandleViews[index]) + new Vector2(MotionPointView.SubHandleWidthHalf, MotionPointView.SubHandleWidthHalf);

			StartDragging(SubHandleViews[index]);
		}
		private void SubHandle_MouseMove(object sender, MouseEventArgs e, int index) {
			if (GraphEditorTab.DraggingElement != SubHandleViews[index])
				return;

			Vector2 cursorPos = (Vector2)e.GetPosition(GraphEditorTab.PointCanvas) + cursorOffset;
			Vector2 pointPosAbsolute = GraphEditorTab.DisplayToNormal(cursorPos);
			pointPosAbsolute = BMath.Clamp(pointPosAbsolute, -MaxHandleRange + 1f, MaxHandleRange);

			ApplyMagnet();

			Vector2 pointPosRelative = pointPosAbsolute - Data.MainPoint.ToVector2();

			Data.SetSubPoint(index, pointPosRelative.ToPVector2());

			GraphEditorTab.SetSmartFollowText(pointPosRelative, GraphEditorTab.NormalToDisplay(pointPosAbsolute));

			void ApplyMagnet() {
				float? result = null;
				result = GraphEditorTab.FindMagnetForY(pointPosAbsolute.y, true, null);
				if (result.HasValue) {
					pointPosAbsolute.y = result.Value;
				}
				result = GraphEditorTab.FindMagnetForX(pointPosAbsolute.x, true, null);
				if (result.HasValue) {
					pointPosAbsolute.x = result.Value;
				}
			}
		}
		private void SubHandle_MouseUp(object sender, MouseButtonEventArgs e, int index) {
			if (GraphEditorTab.DraggingElement != SubHandleViews[index])
				return;

			GraphEditorTab.HideSmartFollowText();
			GraphEditorTab.HideSmartLineForX();
			GraphEditorTab.HideSmartLineForY();

			EndDragging();
		}

		public void Update() {
			Vector2 dPointPos = GraphEditorTab.NormalToDisplay(Data.MainPoint.ToVector2());
			this.SetCanvasPosition(dPointPos);

			for (int subI = 0; subI < Data.SubPoints.Length; ++subI) {
				Grid subHandleView = SubHandleViews[subI];
				Line subLineView = SubLineViews[subI];

				Vector2 dSubPoint = GraphEditorTab.NormalToDisplay((Data.SubPoints[subI] + Data.MainPoint).ToVector2()) - dPointPos;
				subHandleView.SetCanvasPosition(dSubPoint - new Vector2(SubHandleWidthHalf, SubHandleWidthHalf));
				subLineView.SetLinePosition(new Vector2(), dSubPoint);
			}
		}
		private void UpdateWithOtherUI() {
			Update();
			GraphEditorTab.UpdateGraphLine();
			PreviewTab.UpdatePositionContinuum();
			MotionTab.DataToViewDict[GraphEditorTab.EditingMotionData].Cast<MotionItemView>().UpdatePreviewGraph();
		}

		private void StartDragging(FrameworkElement element) {
			GraphEditorTab.SetDraggingElement(element);
		}
		private void EndDragging() {
			GraphEditorTab.SetDraggingElement(null);
		}
	}
}
