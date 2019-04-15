using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using PendulumMotion;
using PendulumMotion.Component;
using PendulumMotion.Items;
using PendulumMotion.System;
using PendulumMotionEditor.Views.Components;
using PendulumMotionEditor.Views.Items;
using PendulumMotionEditor.Views.Windows;
using GKit;
using GKit.Security;

namespace PendulumMotionEditor {
	public class EditableMotionFile : IDisposable {
		private static Root Root => Root.Instance;
		private static MainWindow MainWindow => Root.mainWindow;

		public bool isChanged;
		public PMFile file;
		public HashSet<PMItemBase> selectedItemSet;
		public PMFolder SelectedParentFolder {
			get {
				if (selectedItemSet.Count == 1) {
					foreach (PMItemBase item in selectedItemSet) {
						if (item.type == PMItemType.Folder) {
							return item.Cast<PMFolder>();
						}
					}
				}
				return file.rootFolder;
			}
		}

		public EditableMotionFile() {
			file = new PMFile();
			Init();
		}
		private EditableMotionFile(PMFile file) {
			this.file = file;
			Init();
		}
		private void Init() {
			selectedItemSet = new HashSet<PMItemBase>();
			file.rootFolder.view = new PMItemView(PMItemType.RootFolder);
			MainWindow.MLItemContext.Children.Add(file.rootFolder.view.Cast<PMItemView>());
		}
		public void Dispose() {
			MainWindow.MLItemContext.Children.Remove(file.rootFolder.view.Cast<PMItemView>());
		}

		public bool Save() {
			string filePath = null;
			if (file.IsFilePathAvailable) {
				filePath = file.filePath;
			} else {
				SaveFileDialog dialog = new SaveFileDialog();
				dialog.Filter = IOInfo.Filter;
				dialog.DefaultExt = IOInfo.Extension;

				bool? result = dialog.ShowDialog();
				if (result != null && result.Value) {
					file.filePath = filePath = dialog.FileName;
				} else {
					return false;
				}
			}

			file.Save(filePath);
			isChanged = false;
			return true;
		}
		public static EditableMotionFile Load() {
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.DefaultExt = IOInfo.Extension;
			dialog.Filter = IOInfo.Filter;
			bool? result = dialog.ShowDialog();

			if (result != null && result.Value == true) {
				PMFile file = PMFile.Load(dialog.FileName);

				return new EditableMotionFile(file); 
			} else {
				return null;
			}
		}		

		public PMMotion CreateMotion() {
			PMFolder parentFolder = SelectedParentFolder;

			PMMotion motion = file.CreateMotion(parentFolder);
			motion.view = new PMItemView(PMItemType.Motion);
			parentFolder.view.Cast<PMItemView>().ChildContext.Children.Add(motion.view.Cast<PMItemView>());

			RegisterItemEvent(motion);
			return motion;
		}
		public PMFolder CreateFolder() {
			PMFolder parentFolder = SelectedParentFolder;

			PMFolder folder = file.CreateFolder(parentFolder);
			folder.view = new PMItemView(PMItemType.Folder);
			parentFolder.view.Cast<PMItemView>().ChildContext.Children.Add(folder.view.Cast<PMItemView>());

			RegisterItemEvent(folder);
			return folder;
		}
		public void RemoveItem(PMItemBase item) {
			item.parent.view.Cast<PMItemView>().ChildContext.Children.Remove(item.view.Cast<PMItemView>());
			file.RemoveItem(item);
		}

		private void RegisterItemEvent(PMItemBase item) {
			item.view.Cast<PMItemView>().ContentPanel.MouseDown += OnMouseDown_ItemBackPanel;

			void OnMouseDown_ItemBackPanel(object sender, System.Windows.Input.MouseButtonEventArgs e) {
				if(KeyInput.GetKey(WinKey.LeftControl) || KeyInput.GetKey(WinKey.RightControl)) {
					if(selectedItemSet.Contains(item)) {
						RemoveSelectedItem(item);
					} else {
						AddSelectedItem(item);
					}
				} else {
					SelectItem(item);
				}
			}
		}

		public void SelectItem(PMItemBase item) {
			ClearSelectedItem();
			AddSelectedItem(item);
		}
		public void AddSelectedItem(PMItemBase item) {
			item.view.Cast<PMItemView>().SetSelected(true);
			selectedItemSet.Add(item);
			if(item.type == PMItemType.Motion) {
				MainWindow.EditPanel.AttachMotion(item.Cast<PMMotion>());
			} else {
				MainWindow.EditPanel.DetachMotion();
			}

		}
		public void RemoveSelectedItem(PMItemBase item) {
			item.view.Cast<PMItemView>().SetSelected(false);
			selectedItemSet.Remove(item);
			MainWindow.EditPanel.DetachMotion();
		}
		public void ClearSelectedItem() {
			foreach(PMItemBase item in selectedItemSet) {
				item.view.Cast<PMItemView>().SetSelected(false);
			}
			selectedItemSet.Clear();
			MainWindow.EditPanel.DetachMotion();
		}
	}
}
