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

namespace PendulumMotionEditor.Views.Components {
	/// <summary>
	/// MGEditPanel.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MGEditPanel : UserControl {

		public bool OnEditing => editingData != null;
		public PMotionData editingData;

		private const int GridVerticalCount = 2;
		private const int GridHorizontalCount = 3;
		private Line[] gridVertical;
		private Line[] gridHorizontal;
		private float displayZoom;
		private PVector2 displayOffset;

		public MGEditPanel() {
			InitializeComponent();
			InitDynamicComponent();
			Loaded += OnLoaded;

			void InitDynamicComponent() {
				CreateGrid();
			}
		}
		private void OnLoaded(object sender, RoutedEventArgs e) {
			ResetEnv();
		}
		private void ResetEnv() {
			displayZoom = 1f;
			displayOffset = new PVector2();
		}

		private void CreateGrid() {
			SolidColorBrush gridColor = "#4D4D4D".ToBrush();

			gridVertical = new Line[GridVerticalCount];
			gridHorizontal = new Line[GridHorizontalCount];

			for (int i = 0; i < gridVertical.Length; ++i) {
				Line line = gridVertical[i] = new Line();
				line.Stroke = gridColor;
				EditCanvas.Children.Add(line);
			}
			for(int i=0; i<gridHorizontal.Length; ++i) {
				Line line = gridHorizontal[i] = new Line();
				line.Stroke = gridColor;
				EditCanvas.Children.Add(line);
			}
		}

		private void UpdateGrid() {
			for (int i = 0; i < gridVertical.Length; ++i) {
				Line line = gridVertical[i];
			}
			for (int i = 0; i < gridHorizontal.Length; ++i) {
				Line line = gridHorizontal[i];
			}
		}
	}
}
