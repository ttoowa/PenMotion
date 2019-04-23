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
using PendulumMotion.Component;
using PendulumMotion.Items;
using GKit;

namespace PendulumMotionEditor.Views.Items {
	/// <summary>
	/// MLMotionItem.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class PMItemView : UserControl {
		private static SolidColorBrush DefaultBG = "E0E0E0".ToBrush();
		private static SolidColorBrush SelectedBG = "EACB9E".ToBrush();
		private static SolidColorBrush GraphLineColor = "B09753".ToBrush();
		private const int GraphResolution = 10;

		private Line[] graphLines;

		public PMItemView() {
			InitializeComponent();
		}
		public PMItemView(PMItemType type) {
			InitializeComponent();
			switch (type) {
				case PMItemType.Motion:
					ContentPanel.Children.Remove(FolderContent);
					CreateGraph();
					break;
				case PMItemType.Folder:
					ContentPanel.Children.Remove(MotionContent);
					break;
				case PMItemType.RootFolder:
					BackPanel.Children.Remove(ContentContext);
					ChildContext.Margin = new Thickness();
					break;
			}
		}
		public void SetSelected(bool selected) {
			ContentPanel.Background = selected ? SelectedBG : DefaultBG;
		}

		//MotionItem
		public void CreateGraph() {
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
		public void UpdateGraph(PMMotion ownerMotion) {
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
					return ownerMotion.GetMotionValue(linearValue);
				}
			}
		}
	}
}
