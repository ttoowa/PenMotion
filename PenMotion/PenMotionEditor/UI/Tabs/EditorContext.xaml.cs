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
using Microsoft.Win32;
using GKit;
using GKit.WPF;
using PendulumMotion.Components;
using PendulumMotion.System;

namespace PenMotionEditor.UI.Tabs {
	public partial class EditorContext : UserControl {
		public static EditorContext Instance {
			get; private set;
		}

		//Modules
		public GLoopEngine LoopEngine {
			get; private set;
		}
		public CursorStorage CursorStorage {
			get; private set;
		}

		//EditingFile
		public bool IsSaved {
			get; private set;
		}
		public bool OnEditing => EditingFile != null;
		public MotionFile EditingFile {
			get; private set;
		}

		public EditorContext() {
			Instance = this;

			InitializeComponent();

			Init();
			RegisterEvents();
		}
		private void Init() {
			InitMembers();
			InitTabs();
			RegisterEvents();
		}
		private void InitTabs() {
			MotionTab.Init(this);
			GraphEditorTab.Init(this);
			PreviewTab.Init(this);
		}
		private void InitMembers() {
			LoopEngine = new GLoopEngine();
			CursorStorage = new CursorStorage();

			LoopEngine.StartLoop();
		}
		private void RegisterEvents() {

			LoopEngine.AddLoopAction(OnTick);
		}

		public bool CreateFile(bool checkSaved = true) {
			if (checkSaved && !ShowSaveQuestion())
				return false;

			CloseFile(false);
			EditingFile = new MotionFile();
			return true;
		}
		public bool OpenFile(bool checkSaved = true) {
			if (checkSaved && !ShowSaveQuestion())
				return false;

			string filePath = GetOpenFilename();
			if(string.IsNullOrEmpty(filePath))
				return false;

			CloseFile(false);
			EditingFile = MotionFile.Load(filePath);
			//TODO : View도 Sync 해줘야 한다..
			return true;
		}
		public bool SaveFile() {
			string filePath = GetSaveFilename();
			if (string.IsNullOrEmpty(filePath))
				return false;

			EditingFile.Save(filePath);

			MarkSaved();
			return true;
		}
		public bool CloseFile(bool checkSaved = true) {
			if (checkSaved && !ShowSaveQuestion())
				return false;

			MotionTab.ClearItems();
			GraphEditorTab.DetachMotion();
			return true;
		}

		public void MarkSaved() {
			IsSaved = true;
		}
		public void MarkUnsaved() {
			IsSaved = false;
		}

		public bool ShowSaveQuestion() {
			if (!IsSaved) {
				MessageBoxResult result = MessageBox.Show(
					$"작업 중인 파일이 저장되지 않았습니다.{Environment.NewLine}" +
					$"저장하시겠습니까?", "저장", MessageBoxButton.YesNoCancel);
				switch (result) {
					case MessageBoxResult.Yes:
						return SaveFile();
					case MessageBoxResult.No:
						return true;
					default:
					case MessageBoxResult.Cancel:
						return false;
				}
			} else {
				return true;
			}
		}

		//Events
		private void OnTick() {
			//ApplyPreviewFPS();
		}
		
		private string GetSaveFilename() {
			if (EditingFile.IsFilePathAvailable) {
				return EditingFile.filePath;
			} else {
				SaveFileDialog dialog = new SaveFileDialog();
				dialog.Filter = IOInfo.Filter;
				dialog.DefaultExt = IOInfo.Extension;

				bool? result = dialog.ShowDialog();
				if (result != null && result.Value) {
					EditingFile.filePath = dialog.FileName;
					return dialog.FileName;
				} else {
					return null;
				}
			}
		}
		private string GetOpenFilename() {
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.DefaultExt = IOInfo.Extension;
			dialog.Filter = IOInfo.Filter;
			bool? result = dialog.ShowDialog();

			if (result != null && result.Value == true) {
				return dialog.FileName;
			} else {
				return null;
			}
		}
	}
}
