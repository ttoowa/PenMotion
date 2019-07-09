using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;
using GKit;
using PendulumMotion.Components.Items;
using PendulumMotion.Components.Items.Elements;
using PenMotionEditor.UI.Tabs;
using PenMotionEditor.UI.Items.Elements;

namespace PenMotionEditor.UI.Items {
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
		public List<MotionPointView> pointList;

		public MotionItemView(EditorContext editorContext, bool createDefault = true) : base(editorContext, MotionItemType.Motion) {
			ContentPanel.Children.Remove(FolderContent);

			pointList = new List<MotionPointView>();
			CreatePreviewGraph();

			if(createDefault) {
				Data = EditingFile.CreateMotionDefault();
			} else {
				Data = EditingFile.CreateMotionEmpty();
			}

			CreatePointViewsFromData();
		}

		public void ClearPointViews() {
			if (pointList == null)
				return;

			pointList.Clear();
		}
		public void CreatePointViewsFromData() {
			pointList = new List<MotionPointView>();

			foreach(MotionPoint point in Data.pointList) {
				MotionPointView pointView = new MotionPointView(EditorContext);
				pointView.Data = point;
				//TODO : View 동기화 해줄 것

				pointList.Add(pointView);
			}
		}
		public void InsertPoint(int index, Vector2 position) {
			MotionPointView pointView = new MotionPointView(EditorContext);
			pointList.Insert(index, pointView);

			Data.InsertPoint(index, pointView.Data);
		}
		public void RemovePoint(MotionPointView pointView) {
			pointList.Remove(pointView);

			Data.RemovePoint(pointView.Data);
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
			float previewRectWidth = (float)PreviewGraphContext.ActualWidth;
			float previewRectHeight = (float)PreviewGraphContext.ActualHeight;
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
