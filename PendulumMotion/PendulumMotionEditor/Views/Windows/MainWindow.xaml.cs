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
using PendulumMotion;
using PendulumMotion.Component;
using PendulumMotion.Items;
using PendulumMotion.System;
using PendulumMotionEditor.Views.Items;
using PendulumMotionEditor.Views.FX;

namespace PendulumMotionEditor.Views.Windows
{
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window
	{
		private static Root Root => Root.Instance;
		private static GLoopEngine LoopEngine => Root.loopEngine;
		private static CursorStorage CursorStorage => Root.cursorStorage;

		public MainWindow()
		{
			InitializeComponent();
			if (this.IsDesignMode())
				return;

			Loaded += OnLoad;
		}

		//Event
		private void OnLoad(object sender, RoutedEventArgs e)
		{
			Init();
			RegisterEvents();
		}
		private void Init() {

			Cursor = CursorStorage.cursor_default;
			SetContentContextVisible(false);
		}
		private void RegisterEvents() {
			Closing += OnClosing;

			Root.OnLoopTick += OnTick;

			//FileManagerBar
			FileManagerBar.OnClick_CreateFileButton += OnClick_NewFileButton;
			FileManagerBar.OnClick_OpenFileButton += OnClick_OpenFileButton;
			FileManagerBar.OnClick_SaveFileButton += OnClick_SaveFileButton;
		}

		private void OnClosing(object sender, CancelEventArgs e) {
			if (!EditorContext.IsSaveMarked) {
				e.Cancel = true;
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
		private void OnClick_NewFileButton() {
			if(EditorContext.IsSaveMarked) {
				EditorContext.CreateNewFile();
				SetContentContextVisible(true);
			}
		}
		private void OnClick_OpenFileButton() {
			if (EditorContext.IsSaveMarked) {
				Dispatcher.BeginInvoke(new Action(() => {
					if(EditorContext.OpenFile()) {
						SetContentContextVisible(true);
					}
				}));
			}
		}
		private void OnClick_SaveFileButton() {
			Dispatcher.BeginInvoke(new Action(()=> {
				EditorContext.SaveFile();
			}));
		}

		//UI
		public void SetContentContextVisible(bool show) {
			EditorContext.Visibility = show ? Visibility.Visible : Visibility.Hidden;
		}
		
	}
}
