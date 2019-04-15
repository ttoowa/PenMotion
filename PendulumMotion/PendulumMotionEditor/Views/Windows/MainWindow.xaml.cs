using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.Win32;
using GKit;
using PendulumMotion;
using PendulumMotion.Component;
using PendulumMotion.Items;
using PendulumMotion.System;
using PendulumMotionEditor.Views.Items;

namespace PendulumMotionEditor.Views.Windows
{
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window
	{
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
		private float previewTime;

		private GLoopEngine previewLoopCore;

		public bool OnEditing => editingMotion != null;
		public EditableMotionFile editingMotion;

		public MainWindow()
		{
			InitializeComponent();
			Loaded += OnLoad;
		}
		private void OnLoad(object sender, RoutedEventArgs e)
		{
			Root root = new Root(this);

			previewLoopCore = new GLoopEngine();
			previewLoopCore.AddLoopAction(OnPreviewTick);

			Init();
			RegisterEvents();
			previewLoopCore.StartLoop();
			
			void Init() {
				SetContentContextVisible(false);
			}
			void RegisterEvents() {
				//Button reaction
				Grid[] btns = new Grid[] {
					TMNewFileButton,
					TMOpenFileButton,
					TMSaveFileButton,
					MLAddMotionButton,
					MLAddFolderButton,
					MLRemoveButton,
					MLCopyButton,
				};
				for(int i=0; i<btns.Length; ++i) {
					Grid btn = btns[i];
					btn.SetBtnColor((Border)btn.Children[btn.Children.Count - 1]);
				}

				//Topmenu button
				TMNewFileButton.SetOnClick(OnClick_TMNewFileButton);
				TMOpenFileButton.SetOnClick(OnClick_TMOpenFileButton);
				TMSaveFileButton.SetOnClick(OnClick_TMSaveFileButton);

				//Motionlist button
				MLAddMotionButton.SetOnClick(OnClick_MLAddMotionButton);
				MLAddFolderButton.SetOnClick(OnClick_MLAddFolderButton);
				MLRemoveButton.SetOnClick(OnClick_MLRemoveButton);
				MLCopyButton.SetOnClick(OnClick_MLCopyButton);

			}
		}

		//Event
		private void OnPreviewTick() {
			const float DelayTime = 0.2f;
			const float SeparatorWidthHalf = 2f;

			//Simulate Time
			previewTime += 1f / 60f;
			if(previewTime > 1f + DelayTime) {
				previewTime = -DelayTime;
			}
			float actualTime = Mathf.Clamp01(previewTime);
			float motionTime = EditPanel.OnEditing ? EditPanel.editingMotion.GetMotionValue(actualTime) : 0f;

			//Update Position
			double gridWidth = PreviewPositionGrid.ColumnDefinitions[1].ActualWidth;
			double previewPos = gridWidth * motionTime - PreviewPositionShape.ActualWidth * 0.5f - SeparatorWidthHalf;
			PreviewPositionShape.Margin = new Thickness(previewPos, 0d, 0d, 0d);

			//Update Scale
			PreviewScaleShape.RenderTransform = new ScaleTransform(motionTime, motionTime);
		}
		private void OnClick_TMNewFileButton() {
			if(OnEditing && editingMotion.isChanged)
				return;

			editingMotion = new EditableMotionFile();
			SetContentContextVisible(true);
		}
		private void OnClick_TMOpenFileButton() {
			if (OnEditing && editingMotion.isChanged)
				return;
			Dispatcher.BeginInvoke(new Action(() => {
				editingMotion = EditableMotionFile.Load();

				if (editingMotion != null) {
					//Success
					SetContentContextVisible(true);
				}
			}));
		}
		private void OnClick_TMSaveFileButton() {
			Dispatcher.BeginInvoke(new Action(()=> {
				editingMotion.Save();
			}));
		}
		private void OnClick_MLAddMotionButton() {
			PMMotion motion = editingMotion.CreateMotion();
			editingMotion.SelectItem(motion);
		}
		private void OnClick_MLAddFolderButton() {
			PMFolder folder = editingMotion.CreateFolder();
			editingMotion.SelectItem(folder);
			//MLFolderItem folder = new MLFolderItem();
			//MLItemContext.Children.Add(folder);
		}
		private void OnClick_MLRemoveButton() {

		}
		private void OnClick_MLCopyButton() {

		}

		//UI
		public void SetContentContextVisible(bool show) {
			ContentContext.Visibility = show ? Visibility.Visible : Visibility.Hidden;
		}
	}
}
