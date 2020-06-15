using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;
using PenMotion.Datas.Items;
using PenMotion.Datas.Items.Elements;
using PenMotionEditor.UI.Tabs;
using PenMotionEditor.UI.Elements;
using GKitForWPF;
using GKitForWPF.UI.Controls;
using System.Windows;
using System.Windows.Controls;
using GKitForWPF.Graphics;

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

		private Line[] graphLines;

		public MotionItemView() : base() {

		}
		public MotionItemView(MotionEditorContext editorContext, MotionItem data) : base(editorContext, MotionItemType.Motion) {
			ContentPanel.Children.Remove(FolderContent);
			CreatePreviewGraph();

			Data = data;
		}

		//PreviewGraph
		public void CreatePreviewGraph() {
			graphLines = new Line[GraphResolution];

			for (int i = 0; i < graphLines.Length; ++i) {
				Line line = graphLines[i] = new Line();
				SetLineStyle(line);

				PreviewGraphContext.Children.Add(line);
			}

			void SetLineStyle(Line line) {
				line.Stroke = GraphLineColor;
				line.StrokeThickness = 1.5d;
			}
		}
		public void UpdatePreviewGraph() {
			float previewRectWidth = (float)PreviewGraphContext.Width;
			float previewRectHeight = (float)PreviewGraphContext.Height;

			for (int graphLineI = 0; graphLineI < graphLines.Length; ++graphLineI) {
				float motionValue = GetMotionValue(graphLineI);
				float nextMotionValue = GetMotionValue(graphLineI + 1);

				Line line = graphLines[graphLineI];
				line.X1 = graphLineI * previewRectWidth / GraphResolution;
				line.X2 = (graphLineI + 1) * previewRectWidth / GraphResolution;
				line.Y1 = previewRectHeight - motionValue * previewRectHeight;
				line.Y2 = previewRectHeight - nextMotionValue * previewRectHeight;

				float GetMotionValue(int index) {
					float linearValue = (float)index / graphLines.Length;
					return Data.GetMotionValue(linearValue);
				}
			}
		}
	}
}
