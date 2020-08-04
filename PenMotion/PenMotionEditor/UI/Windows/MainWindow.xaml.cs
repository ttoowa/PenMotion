using GKitForWPF;
using PenMotionEditor.UI.FX;
using System.ComponentModel;
using System.Windows;

namespace PenMotionEditor.UI.Windows {
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
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
			FileManagerBar.CreateFileButtonClick += NewFileButton_OnClick;
			FileManagerBar.OpenFileButtonClick += OpenFileButton_OnClick;
			FileManagerBar.SaveFileButtonClick += SaveFileButton_OnClick;
		}

		private void OnClosing(object sender, CancelEventArgs e) {
			if (!EditorContext.IsSaved) {
				if (!EditorContext.ShowSaveQuestion()) {
					e.Cancel = true;
				}
			}
		}
		private void OnTick() {
			if (MouseInput.Left.IsDown) {
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
			if (EditorContext.OpenFile()) {
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
