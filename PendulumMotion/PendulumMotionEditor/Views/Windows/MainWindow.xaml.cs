﻿using System;
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
using Microsoft.Win32;
using GKit;
using PendulumMotion;
using PendulumMotion.Component;
using PendulumMotion.System;
using PendulumMotionEditor.Views.Elements;

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

		public bool OnEditing => editingFile != null;
		public PMotionFile editingFile;

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


			RegisterEvents();

			previewLoopCore.StartLoop();
			
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

		private void OnPreviewTick() {
			const float DelayTime = 0.2f;
			const float SeparatorWidthHalf = 2f;

			//Simulate Time
			previewTime += 1f / 60f;
			if(previewTime > 1f + DelayTime) {
				previewTime = -DelayTime;
			}
			float actualTime = Mathf.Clamp01(previewTime);
			float motionTime = EditPanel.OnEditing ? EditPanel.editingData.GetMotionValue(actualTime) : 0f;

			//Update Position
			double gridWidth = PreviewPositionGrid.ColumnDefinitions[1].ActualWidth;
			double previewPos = gridWidth * motionTime - PreviewPositionShape.ActualWidth * 0.5f - SeparatorWidthHalf;
			PreviewPositionShape.Margin = new Thickness(previewPos, 0d, 0d, 0d);

			//Update Scale
			PreviewScaleShape.RenderTransform = new ScaleTransform(motionTime, motionTime);
		}

		private bool CheckSaveEditingFile() {
			return true;
		}

		private void OnClick_TMNewFileButton() {
			if(OnEditing && !CheckSaveEditingFile())
				return;

			editingFile = new PMotionFile();
			ContentContext.Visibility = Visibility.Visible;
		}
		private void OnClick_TMOpenFileButton() {
			if (OnEditing && !CheckSaveEditingFile())
				return;

			OpenFileDialog dialog = new OpenFileDialog();
			dialog.DefaultExt = ".pmotion";
			bool? result = dialog.ShowDialog();

			if(result != null && result.Value == true) {
				editingFile = PMotionFile.Load(dialog.FileName);
			}
		}
		private void OnClick_TMSaveFileButton() {

		}
		private void OnClick_MLAddMotionButton() {

		}
		private void OnClick_MLAddFolderButton() {

		}
		private void OnClick_MLRemoveButton() {

		}
		private void OnClick_MLCopyButton() {

		}

	}
}
