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
using PendulumMotion.Components.Items.Elements;
using PenMotionEditor.UI.Tabs;

namespace PenMotionEditor.UI.Items.Elements
{
	/// <summary>
	/// MGHandle.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MotionPointView : UserControl
	{
		private EditorContext EditorContext;
		private GraphEditorTab GraphEditorTab => EditorContext.GraphEditorTab;
		private MotionItemView EditingMotionItemView => GraphEditorTab.EditingMotionItemView;
		private GLoopEngine LoopEngine => EditorContext.LoopEngine;

		public const float DefaultSubOffset = 0.3f;
		public const float MainHandleWidth = 20f;
		public const float MainHandleWidthHalf = MainHandleWidth * 0.5f;
		public const float SubHandleWidth = 14f;
		public const float SubHandleWidthHalf = SubHandleWidth * 0.5f;

		private const float MaxHandleRange = 2f;

		public MotionPoint Data {
			get; set;
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

		public event Action DataChanged;

		//Event memory
		private Vector2 cursorOffset;

		public MotionPointView(EditorContext editorContext)
		{
			InitializeComponent();

			MainHandleView = this.MainHandle;
			SubHandleViews = new Grid[] {
				SubHandle0,
				SubHandle1,
			};
			SubLineViews = new Line[] {
				SubLine0,
				SubLine1,
			};

			RegisterEvent();
		}
		private void RegisterEvent() {
			SetButtonReaction(MainHandleView);
			for (int subI = 0; subI < SubHandleViews.Length; ++subI) {
				SetButtonReaction(SubHandleViews[subI]);
			}
		}
		private void SetButtonReaction(Grid button) {
			button.SetButtonReaction((Shape)button.Children[button.Children.Count - 1], 0.3f);
		}

		private void RegisterPointEvent(MotionPointView pointView) {

			Vector2 cursorOffset = new Vector2();

			pointView.MainHandleView.MouseDown += MainHandle_OnMouseDown;
			for (int subI = 0; subI < pointView.SubHandleViews.Length; ++subI) {
				int subHandleIndex = subI;

				pointView.SubHandleViews[subI].MouseDown += (object sender, MouseButtonEventArgs e) => {
					if (e.ChangedButton != MouseButton.Left)
						return;

					SubHandle_OnMouseDown(subHandleIndex);
				};
			}
		}

		private void MainHandle_OnMouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton != MouseButton.Left)
				return;

			cursorOffset = GetCursorOffset(GraphEditorTab.PointCanvas, this);
			LoopEngine.AddLoopAction(MainHandle_OnDrag, GLoopCycle.EveryFrame, GWhen.MouseUpRemove);
			LoopEngine.AddLoopAction(MainHandle_OnMouseUp, GLoopCycle.None, GWhen.MouseUpRemove);
		}
		private void MainHandle_OnDrag() {
			Vector2 cursorPos = MouseInput.GetRelativePosition(GraphEditorTab.PointCanvas) + cursorOffset;
			Vector2 pointPos = GraphEditorTab.DisplayToNormal(cursorPos);
			pointPos = BMath.Clamp(pointPos, -MaxHandleRange + 1f, MaxHandleRange);
			FilterMagnet();

			Data.mainPoint = pointPos.ToPVector2();

			//TODO : 이벤트 드라이븐으로 바꾸기
			GraphEditorTab.UpdateGraph();
			Update();
			EditingMotionItemView.UpdatePreviewGraph();

			GraphEditorTab.SetSmartFollowText(pointPos);
			DataChanged?.Invoke();

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
		}

		private void SubHandle_OnMouseDown(int index) {
			cursorOffset = GetCursorOffset(this, SubHandleViews[index]) + new Vector2(MotionPointView.SubHandleWidthHalf, MotionPointView.SubHandleWidthHalf);

			LoopEngine.AddLoopAction(() => { SubHandle_OnDrag(index); }, GLoopCycle.EveryFrame, GWhen.MouseUpRemove);
			LoopEngine.AddLoopAction(SubHandle_OnMouseUp, GLoopCycle.None, GWhen.MouseUpRemove);
		}
		private void SubHandle_OnDrag(int index) {
			Vector2 cursorPos = MouseInput.GetRelativePosition(GraphEditorTab.PointCanvas) + cursorOffset;
			Vector2 pointPosAbsolute = GraphEditorTab.DisplayToNormal(cursorPos);
			pointPosAbsolute = BMath.Clamp(pointPosAbsolute, -MaxHandleRange + 1f, MaxHandleRange);
			FilterMagnet();
			Vector2 pointPosRelative = pointPosAbsolute - Data.mainPoint.ToVector2();

			Data.subPoints[index] = pointPosRelative.ToPVector2();

			GraphEditorTab.UpdateGraph();
			Update();
			EditingMotionItemView.UpdatePreviewGraph();

			GraphEditorTab.SetSmartFollowText(pointPosRelative, GraphEditorTab.NormalToDisplay(pointPosAbsolute));

			DataChanged?.Invoke();

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
		}

		public void Update() {
			Vector2 dPointPos = GraphEditorTab.NormalToDisplay(Data.mainPoint.ToVector2());
			this.SetCanvasPosition(dPointPos);

			for (int subI = 0; subI < Data.subPoints.Length; ++subI) {
				Grid subHandleView = SubHandleViews[subI];
				Line subLineView = SubLineViews[subI];

				Vector2 dSubPoint = GraphEditorTab.NormalToDisplay((Data.subPoints[subI] + Data.mainPoint).ToVector2()) - dPointPos;
				subHandleView.SetCanvasPosition(dSubPoint - new Vector2(SubHandleWidthHalf, SubHandleWidthHalf));
				subLineView.SetLinePosition(new Vector2(), dSubPoint);

			}
		}

		private Vector2 GetCursorOffset(Visual context, UIElement element) {
			return new Vector2((float)Canvas.GetLeft(element), (float)Canvas.GetTop(element)) - MouseInput.GetRelativePosition(context);
		}
	}
}
