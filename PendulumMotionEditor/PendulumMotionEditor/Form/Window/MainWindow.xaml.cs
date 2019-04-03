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
using PendulumMotionEditor.Form.Element;

namespace PendulumMotionEditor.Form
{
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window
	{
		
		//private MGData editMGData;
		private GLoopCore mainLoopCore;
		private GLoopCore previewLoopCore;
		private float previewTime;
		private float PreviewFPS {
			get {
				return 60;
			}
		}
		private float PreviewSeconds {
			get {
				return 1;
			}
		}
		public MainWindow()
		{
			InitializeComponent();
			Loaded += OnLoad_MainWindow;
		}

		private void OnLoad_MainWindow(object sender, RoutedEventArgs e)
		{
			mainLoopCore = new GLoopCore();
			mainLoopCore.AddLoopAction(OnTick);
			mainLoopCore.StartLoop();

			previewLoopCore = new GLoopCore();
			previewLoopCore.AddLoopAction(OnPreviewTick);
			previewLoopCore.StartLoop();

			//editMGData = new MGData();


		}
		private void OnTick() {
			
		}
		private void OnPreviewTick() {
			const float DelayTime = 0.2f;
			const float SeparatorWidthHalf = 2f;

			//Simulate Time
			previewTime += 1f / 60f;
			if(previewTime > 1f + DelayTime) {
				previewTime = -DelayTime;
			}
			float actualTime = Mathf.Clamp01(previewTime);
			float motionTime = actualTime;// editMGData.GetMotionTime(actualTime);

			//Update Position
			double gridWidth = PreviewPositionGrid.ColumnDefinitions[1].ActualWidth;
			double previewPos = gridWidth * motionTime - PreviewPositionShape.ActualWidth * 0.5f - SeparatorWidthHalf;
			PreviewPositionShape.Margin = new Thickness(previewPos, 0d, 0d, 0d);

			//Update Scale
			PreviewScaleShape.RenderTransform = new ScaleTransform(motionTime, motionTime);
		}

	}
}
