using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
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
		private static GLoopEngine LoopEngine => Root.loopEngine;

		public bool isUnsaved;
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
			file.rootFolder.view = new PMItemView(file.rootFolder, PMItemType.RootFolder);
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
			isUnsaved = false;

			ToastMessage.Show("저장되었습니다.");

			return true;
		}
		public static EditableMotionFile Load() {
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.DefaultExt = IOInfo.Extension;
			dialog.Filter = IOInfo.Filter;
			bool? result = dialog.ShowDialog();

			if (result != null && result.Value == true) {
				PMFile file = PMFile.Load(dialog.FileName);
				EditableMotionFile editingFile = new EditableMotionFile(file);

				CreateViewRecursive(file.rootFolder);

				void CreateViewRecursive(PMFolder parentFolder) {
					for (int childI = 0; childI < parentFolder.childList.Count; ++childI) {
						PMItemBase item = parentFolder.childList[childI];
						editingFile.InitItem(item, parentFolder);

						switch (item.type) {
							case PMItemType.Folder:
								CreateViewRecursive(item.Cast<PMFolder>());
								break;
						}
					}
				}

				return editingFile; 
			} else {
				return null;
			}
		}		
		public bool ShowSaveMessage() {
			if(isUnsaved) {
				MessageBoxResult result = MessageBox.Show("저장되지 않았습니다. 저장하시겠습니까?", "저장", MessageBoxButton.YesNoCancel);
				switch (result) {
					case MessageBoxResult.Yes:
						return Save();
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

		public PMMotion CreateMotion() {
			PMFolder parentFolder = SelectedParentFolder;
			PMMotion motion = file.CreateMotionDefault(parentFolder);

			InitItem(motion, parentFolder);
			return motion;
		}
		public PMFolder CreateFolder() {
			PMFolder parentFolder = SelectedParentFolder;
			PMFolder folder = file.CreateFolder(parentFolder);

			InitItem(folder, parentFolder);
			return folder;
		}
		public void RemoveItem(PMItemBase item) {
			item.view.Cast<PMItemView>().Parent.Cast<Panel>().Children.Remove(item.view.Cast<PMItemView>());
			file.RemoveItem(item);
		}
		public void RemoveSelectedItems() {
			foreach(PMItemBase item in selectedItemSet) {
				RemoveItem(item);
			}
			UnselectItemAll();
		}
		private void InitItem(PMItemBase item, PMFolder parentFolder) {
			PMItemView view = new PMItemView(item, item.type);
			item.view = view;
			view.SetName(item.name);
			parentFolder.view.Cast<PMItemView>().ChildContext.Children.Add(view);

			RegisterItemEvent(item);
		}

		public void SelectItemSingle(PMItemBase item) {
			UnselectItemAll();
			SelectItemAdd(item);
		}
		public void SelectItemAdd(PMItemBase item) {
			item.view.Cast<PMItemView>().SetSelected(true);
			selectedItemSet.Add(item);
			if(item.type == PMItemType.Motion) {
				MainWindow.EditPanel.AttachMotion(item.Cast<PMMotion>());
				MainWindow.ResetPreviewTime();
			} else {
				MainWindow.EditPanel.DetachMotion();
			}

		}
		public void UnselectItemSingle(PMItemBase item) {
			item.view.Cast<PMItemView>().SetSelected(false);
			selectedItemSet.Remove(item);
			MainWindow.EditPanel.DetachMotion();
		}
		public void UnselectItemAll() {
			foreach(PMItemBase item in selectedItemSet) {
				item.view.Cast<PMItemView>().SetSelected(false);
			}
			selectedItemSet.Clear();
			MainWindow.EditPanel.DetachMotion();
		}

		public void MarkUnsaved() {
			isUnsaved = true;
		}
		private void RegisterItemEvent(PMItemBase item) {
			//심각하게 하드코딩이긴 한데.. 한가지 기능만을 하는 UI를 모듈화 시켜야 할 이유가?

			const float FolderSideEventWeight = 0.2f;
			const float FolderMidEventWeight = 1f - FolderSideEventWeight * 2f;

			PMItemView itemView = item.view.Cast<PMItemView>();
			itemView.ContentPanel.MouseDown += OnMouseDown_ItemContentPanel;


			void OnMouseDown_ItemContentPanel(object sender, System.Windows.Input.MouseButtonEventArgs e) {
				if(e.ClickCount == 1) {
					if (KeyInput.GetKeyHold(WinKey.LeftControl) || KeyInput.GetKeyHold(WinKey.RightControl)) {
						if (selectedItemSet.Contains(item)) {
							UnselectItemSingle(item);
						} else {
							SelectItemAdd(item);
						}
					} else if (KeyInput.GetKeyHold(WinKey.LeftShift) || KeyInput.GetKeyHold(WinKey.RightShift)) { 
						//드래그 선택
					} else {
						LoopEngine.AddGRoutine(OnMouseDrag_ItemContentPanel());
					}
				} else if(e.ClickCount == 2) {
					itemView.SetNameEditTextVisible(true);
				}
				e.Handled = true;
			}
			IEnumerator OnMouseDrag_ItemContentPanel() {
				for(; ;) {
					if(MouseInput.LeftUp) {
						SelectItemSingle(item);
						yield break;
					}
					if(!IsMouseOver(item.view.Cast<PMItemView>().ContentPanel)) {
						if(!IsSelected(item)) {
							SelectItemSingle(item);
						}
						LoopEngine.AddLoopAction(OnMouseDrag_ItemContentPanel_ForMove, GLoopCycle.EveryFrame, GWhen.MouseUpRemove);
						yield break;
					}
					
					yield return new GWait(GTimeUnit.Frame, 1);
				}
			}
			void OnMouseDrag_ItemContentPanel_ForMove() {
				MoveOrder moveOrder = GetCursorTarget();
				Rectangle movePointer = MainWindow.MLItemMovePointer;
				if (moveOrder != null) {
					PMItemView targetView = moveOrder.target.view.Cast<PMItemView>();
					Panel targetContent = targetView.ContentContext;
					float panelRelativeTop = (float)targetView.TranslatePoint(new Point(), MainWindow.MLItemContext).Y;
					Vector2 targetPanelSize = new Vector2((float)targetContent.ActualWidth, (float)targetContent.ActualHeight);

					if (MouseInput.LeftUp) {
						//Hide MovePointer
						movePointer.Visibility = Visibility.Collapsed;
						//Apply
					} else {
						//Show MovePointer
						double pointerHeight = targetPanelSize.y * FolderSideEventWeight;
						movePointer.Width = targetPanelSize.x;
						if(moveOrder.target.type == PMItemType.Motion) {
							movePointer.Height = pointerHeight;
							switch (moveOrder.direction) {
								case Direction.Top:
									Canvas.SetTop(movePointer, panelRelativeTop - pointerHeight * 0.5f);
									break;
								case Direction.Bottom:
									Canvas.SetTop(movePointer, panelRelativeTop + targetPanelSize.y - pointerHeight * 0.5f);
									break;
							}
						} else {
							switch (moveOrder.direction) {
								case Direction.Top:
									movePointer.Height = pointerHeight = targetPanelSize.y * FolderSideEventWeight;
									Canvas.SetTop(movePointer, panelRelativeTop  - pointerHeight * 0.5f);
									break;
								case Direction.Bottom:
									movePointer.Height = pointerHeight = targetPanelSize.y * FolderSideEventWeight;
									Canvas.SetTop(movePointer, panelRelativeTop + targetPanelSize.y - pointerHeight * 0.5f);
									break;
								case Direction.Right:
									movePointer.Height = pointerHeight = targetPanelSize.y * FolderMidEventWeight;
									Canvas.SetTop(movePointer, panelRelativeTop + targetPanelSize.y * FolderSideEventWeight);
									break;
							}
						}
						MainWindow.MLItemMovePointer.Visibility = Visibility.Visible;
					}
					GDebug.Log($"MoveOrder = {moveOrder.target.name} : {moveOrder.direction.ToString()}");
				} else {
					movePointer.Visibility = Visibility.Collapsed;
				}
			}
			MoveOrder GetCursorTarget() {
				return GetCursorTargetRecursion(file.rootFolder);

				MoveOrder GetCursorTargetRecursion(PMItemBase target) {
					if (!target.IsRoot) {
						Panel panel = target.view.Cast<PMItemView>().ContentPanel;
						try {
							float panelTop = panel.GetAbsolutePosition().y;
							if (IsMouseOver(panel)) {
								MoveOrder moveOrder = new MoveOrder(target);
								if (target.type == PMItemType.Motion) {
									float panelMid = panelTop + (float)panel.ActualHeight * 0.5f;

									moveOrder.direction = MouseInput.AbsolutePosition.y < panelMid ? Direction.Top : Direction.Bottom;
									return moveOrder;
								} else {
									float panelMidTop = panelTop + (float)panel.ActualHeight * FolderSideEventWeight;
									float panelMidBot = panelTop + (float)panel.ActualHeight * FolderMidEventWeight;

									if (MouseInput.AbsolutePosition.y < panelMidTop) {
										moveOrder.direction = Direction.Top;
									} else if (MouseInput.AbsolutePosition.y > panelMidBot) {
										moveOrder.direction = Direction.Bottom;
									} else {
										moveOrder.direction = Direction.Right;
									}
									return moveOrder;
								}
							}
						} catch(Exception ex) {
							GDebug.Log(ex.ToString());
						}
					}
					if (target.type != PMItemType.Motion) {
						PMFolder folder = target as PMFolder;
						for(int childI = 0; childI < folder.childList.Count; ++childI) {
							MoveOrder moveOrder = GetCursorTargetRecursion(folder.childList[childI]);
							if(moveOrder != null) {
								return moveOrder;
							}
						}
					}
					return null;
				}
			}
			bool IsMouseOver(Panel itemPanel) {
				float panelTop = itemPanel.GetAbsolutePosition().y;
				GDebug.Log($"{panelTop} , {MouseInput.AbsolutePosition.y}");
				return MouseInput.AbsolutePosition.y > panelTop && MouseInput.AbsolutePosition.y < panelTop + itemPanel.ActualHeight;
			}
		}

		public bool IsSelected(PMItemBase item) {
			return selectedItemSet.Contains(item);
		}
	}
}
