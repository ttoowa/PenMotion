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

namespace PenMotionEditor.UI.Items.Elements
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
			MainHandleView.MouseDown += MainHandle_OnMouseDown;
			for (int subI = 0; subI < SubHandleViews.Length; ++subI) {
				int subHandleIndex = subI;

				SubHandleViews[subI].MouseDown += (object sender, MouseButtonEventArgs e) => {
					if (e.ChangedButton != MouseButton.Left)
						return;

					SubHandle_OnMouseDown(subHandleIndex);
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

		private void MainHandle_OnMouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton != MouseButton.Left)
				return;

			cursorOffset = GetCursorOffset(GraphEditorTab.PointCanvas, this);
			LoopEngine.AddLoopAction(MainHandle_OnDrag, GLoopCycle.EveryFrame, GWhen.MouseUpRemove);
			LoopEngine.AddLoopAction(MainHandle_OnMouseUp, GLoopCycle.None, GWhen.MouseUpRemove);

			GraphEditorTab.isPointDragging = true;

			StartDragging();
		}
		private void MainHandle_OnDrag() {
			Vector2 cursorPos = MouseInput.GetRelativePosition(GraphEditorTab.PointCanvas) + cursorOffset;
			Vector2 pointPos = GraphEditorTab.DisplayToNormal(cursorPos);
			pointPos = BMath.Clamp(pointPos, -MaxHandleRange + 1f, MaxHandleRange);
			FilterMagnet();

			Data.SetMainPoint(pointPos.ToPVector2());

			GraphEditorTab.SetSmartFollowText(pointPos);

			void FilterMagnet() {
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
		private void MainHandle_OnMouseUp() {
			GraphEditorTab.HideSmartFollowText();
			GraphEditorTab.HideSmartLineForX();
			GraphEditorTab.HideSmartLineForY();

			GraphEditorTab.isPointDragging = false;

			EndDragging();
		}

		private void SubHandle_OnMouseDown(int index) {
			cursorOffset = GetCursorOffset(this, SubHandleViews[index]) + new Vector2(MotionPointView.SubHandleWidthHalf, MotionPointView.SubHandleWidthHalf);

			LoopEngine.AddLoopAction(() => { SubHandle_OnDrag(index); }, GLoopCycle.EveryFrame, GWhen.MouseUpRemove);
			LoopEngine.AddLoopAction(SubHandle_OnMouseUp, GLoopCycle.None, GWhen.MouseUpRemove);

			StartDragging();
		}
		private void SubHandle_OnDrag(int index) {
			Vector2 cursorPos = MouseInput.GetRelativePosition(GraphEditorTab.PointCanvas) + cursorOffset;
			Vector2 pointPosAbsolute = GraphEditorTab.DisplayToNormal(cursorPos);
			pointPosAbsolute = BMath.Clamp(pointPosAbsolute, -MaxHandleRange + 1f, MaxHandleRange);
			FilterMagnet();
			Vector2 pointPosRelative = pointPosAbsolute - Data.MainPoint.ToVector2();

			Data.SetSubPoint(index, pointPosRelative.ToPVector2());

			GraphEditorTab.SetSmartFollowText(pointPosRelative, GraphEditorTab.NormalToDisplay(pointPosAbsolute));

			void FilterMagnet() {
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
		private void SubHandle_OnMouseUp() {
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

		private void StartDragging() {

			GraphEditorTab.isPointDragging = true;
		}
		private void EndDragging() {
			GraphEditorTab.isPointDragging = false;

		}

		private Vector2 GetCursorOffset(Visual context, UIElement element) {
			return new Vector2((float)Canvas.GetLeft(element), (float)Canvas.GetTop(element)) - MouseInput.GetRelativePosition(context);
		}
	}
}
