using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
using GKit.WPF;
using PenMotion;
using PenMotion.Datas;
using PenMotion.Datas.Items;
using PenMotion.System;
using PenMotionEditor.UI.Elements;
using PenMotionEditor.UI.FX;

namespace PenMotionEditor.UI.Windows
{
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			if (this.IsDesignMode())
				return;

			Init();
			RegisterEvents();
		}

		private void Init() {
			Cursor = EditorContext.CursorStorage.cursor_default;
			SetContentContextVisible(false);
		}
		private void RegisterEvents() {
			Closing += OnClosing;

			EditorContext.LoopEngine.AddLoopAction(OnTick);

			//FileManagerBar
			FileManagerBar.CreateFileButton_OnClick += NewFileButton_OnClick;
			FileManagerBar.OpenFileButton_OnClick += OpenFileButton_OnClick;
			FileManagerBar.SaveFileButton_OnClick += SaveFileButton_OnClick;
		}

		private void OnClosing(object sender, CancelEventArgs e) {
			if (!EditorContext.IsSaved) {
				if(!EditorContext.ShowSaveQuestion()) {
					e.Cancel = true;
				}
			}
		}
		private void OnTick() {
			if (MouseInput.Left.Down) {
				Vector2 fxPos = MouseInput.AbsolutePosition - FxCanvas.GetAbsolutePosition();

				ClickFx fx = new ClickFx();
				fx.SetParent(FxCanvas);
				fx.SetCanvasPosition(fxPos);
			}
		}
		private void NewFileButton_OnClick() {
			if (EditorContext.CreateFile()) {
				SetContentContextVisible(true);
			}
		}
		private void OpenFileButton_OnClick() {
			if(EditorContext.OpenFile()) {
				SetContentContextVisible(true);
			}
		}
		private void SaveFileButton_OnClick() {
			EditorContext.SaveFile();
		}

		public void CreateNewFile() {
			EditorContext.CreateFile();
		}
		public bool OpenFile() {
			EditorContext.OpenFile();
			return true;
		}
		public void SaveFile() {
			EditorContext.SaveFile();
		}

		//UI
		public void SetContentContextVisible(bool show) {
			EditorContext.Visibility = show ? Visibility.Visible : Visibility.Hidden;
		}
		
	}
}
