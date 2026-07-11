using GKitForWPF;
using GKitForWPF.Graphics;
using PenMotion.Datas.Items;
using PenMotionEditor.UI.Tabs;
using System.Linq;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;

namespace PenMotionEditor.UI.Elements {
	public class MotionItemView : MotionItemBaseView {
		private const int GraphResolution = 16;
		private static SolidColorBrush GraphLineColor = "A89676".ToBrush();

		public new MotionItem Data {
			get {
				return base.Data.Cast<MotionItem>();
			}
			set {
				base.Data = value;
			}
		}

		private Polyline graphLine;

		public MotionItemView() : base() {

		}
		public MotionItemView(MotionEditorContext editorContext, MotionItem data) : base(editorContext, MotionItemType.Motion) {
			ContentPanel.Children.Remove(FolderContent);
			CreatePreviewGraph();

			Data = data;
		}

		//PreviewGraph
		public void CreatePreviewGraph() {
			graphLine = new Polyline {
				Stroke = GraphLineColor,
				StrokeThickness = 1.5d,
				SnapsToDevicePixels = true
			};
			PreviewGraphContext.Children.Add(graphLine);
		}
		public void UpdatePreviewGraph() {
			if (Data == null || graphLine == null)
				return;
			float previewRectWidth = (float)PreviewGraphContext.Width;
			float previewRectHeight = (float)PreviewGraphContext.Height;
			PointCollection points = new(GraphResolution + 1);
			for (int index = 0; index <= GraphResolution; index++) {
				float linearValue = (float)index / GraphResolution;
				float motionValue = Data.GetMotionValue(linearValue);
				points.Add(new Point(index * previewRectWidth / GraphResolution,
					previewRectHeight - motionValue * previewRectHeight));
			}
			graphLine.Points = points;
		}
	}
}
