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

		private const float FolderSideEventWeight = 0.2f;
		private const float FolderMidEventWeight = 1f - FolderSideEventWeight * 2f;

		public bool OnDuplicatableState {
			get {
				if (selectedItemSet.Count == 1) {
					foreach (PMItemBase selectedItem in selectedItemSet) {
						return selectedItem.type == PMItemType.Motion;
					}
				}
				return false;
			}
		}
		public bool isUnsaved;
		public PMFile file;

		public HashSet<PMItemBase> selectedItemSet;
		public PMFolder SelectedParentFolder {
			get {
				if (selectedItemSet.Count == 1) {
					foreach (PMItemBase item in selectedItemSet) {
						if (item.IsFolder) {
							return item.Cast<PMFolder>();
						} else {
							return item.parent;
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
			PMItemView rootFolderView = new PMItemView(file.rootFolder, PMItemType.Folder);
			file.rootFolder.view = rootFolderView;
			rootFolderView.SetRootFolder();
			MainWindow.MLItemContext.Children.Add(file.rootFolder.view.Cast<PMItemView>());
		}
		public void Dispose() {
			MainWindow.MLItemContext.Children.Remove(file.rootFolder.view.Cast<PMItemView>());
		}

		private void OnSelectItemChanged() {
			MainWindow.SetCopyButtonEnable(OnDuplicatableState);
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
				MainWindow.ClearEditingData();

				PMFile file = PMFile.Load(dialog.FileName);
				EditableMotionFile editingFile = new EditableMotionFile(file);

				CreateViewRecursive(file.rootFolder);

				void CreateViewRecursive(PMFolder parentFolder) {
					for (int childI = 0; childI < parentFolder.childList.Count; ++childI) {
						PMItemBase item = parentFolder.childList[childI];
						editingFile.CreateView(item);

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
			if (isUnsaved) {
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
		public void MarkUnsaved() {
			isUnsaved = true;
		}

		public PMMotion CreateMotion() {
			PMFolder parentFolder = SelectedParentFolder;
			PMMotion motion = file.CreateMotionDefault(parentFolder);

			CreateView(motion);
			return motion;
		}
		public PMFolder CreateFolder() {
			PMFolder parentFolder = SelectedParentFolder;
			PMFolder folder = file.CreateFolder(parentFolder);

			CreateView(folder);
			return folder;
		}
		public PMItemView CreateView(PMItemBase item, bool setParent = true) {
			PMItemView view = new PMItemView(item, item.type);
			item.view = view;
			view.SetName(item.name);
			if(setParent) {
				item.parent.view.Cast<PMItemView>().ChildContext.Children.Add(view);
			}

			RegisterItemEvent(item);
			return view;
		}
		public void RemoveItem(PMItemBase item) {
			item.view.Cast<PMItemView>().Parent.Cast<Panel>().Children.Remove(item.view.Cast<PMItemView>());
			file.RemoveItem(item);
		}
		public void RemoveSelectedItems() {
			foreach (PMItemBase item in selectedItemSet) {
				RemoveItem(item);
			}
			UnselectItemAll();
		}

		public void SelectItemSingle(PMItemBase item) {
			UnselectItemAll();
			SelectItemAdd(item);
		}
		public void SelectItemAdd(PMItemBase item) {
			item.view.Cast<PMItemView>().SetSelected(true);
			selectedItemSet.Add(item);
			if (item.type == PMItemType.Motion) {
				MainWindow.EditPanel.AttachMotion(item.Cast<PMMotion>());
				MainWindow.ResetPreviewTime();
			} else {
				MainWindow.EditPanel.DetachMotion();
			}

			OnSelectItemChanged();
		}
		public void UnselectItemSingle(PMItemBase item) {
			item.view.Cast<PMItemView>().SetSelected(false);
			selectedItemSet.Remove(item);
			MainWindow.EditPanel.DetachMotion();

			OnSelectItemChanged();
		}
		public void UnselectItemAll() {
			foreach (PMItemBase item in selectedItemSet) {
				item.view.Cast<PMItemView>().SetSelected(false);
			}
			selectedItemSet.Clear();
			MainWindow.EditPanel.DetachMotion();

			OnSelectItemChanged();
		}
		
		public void DuplicateSelectedMotion() {
			if(selectedItemSet.Count == 1) {
				foreach(PMItemBase refItem in selectedItemSet) {
					if (refItem.type == PMItemType.Motion) {
						PMMotion refMotion = refItem as PMMotion;

						int index = refMotion.parent.childList.IndexOf(refItem) + 1;
						PMMotion motion = PMMotion.CreateDefault(file);
						motion.parent = refMotion.parent;
						motion.pointList.Clear();
						foreach(PMPoint refPoint in refMotion.pointList) {
							PMPoint point = new PMPoint();
							motion.AddPoint(point);

							point.mainPoint = refPoint.mainPoint;
							point.subPoints = refPoint.subPoints.ToArray();
						}
						string name = $"{refMotion.name}";
						for(; ;) {
							if (file.itemDict.ContainsKey(name)) {
								name += "_Copy";
							} else {
								break;
							}
						}
						motion.name = name;

						file.itemDict.Add(motion.name, motion);
						PMItemView view = CreateView(motion, false);

						refMotion.parent.childList.Insert(index, motion);
						refMotion.parent.view.Cast<PMItemView>().ChildContext.Children.Insert
						(index, view);

						SelectItemSingle(motion);
					}
				}
			}
		}

		private void RegisterItemEvent(PMItemBase item) {
			PMItemView itemView = item.view.Cast<PMItemView>();
			itemView.ContentPanel.MouseDown += OnMouseDown_ItemContentPanel;

			void OnMouseDown_ItemContentPanel(object sender, System.Windows.Input.MouseButtonEventArgs e) {
				if (e.ClickCount == 1) {
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
				} else if (e.ClickCount == 2) {
					if (selectedItemSet.Count < 2) {
						itemView.SetNameEditTextVisible(true);
					}
				}
				e.Handled = true;
			}
			IEnumerator OnMouseDrag_ItemContentPanel() {
				for (; ; ) {
					if (!MouseInput.LeftHold) {
						SelectItemSingle(item);
						yield break;
					}
					if (!IsMouseOverY(item.view.Cast<PMItemView>().ContentPanel)) {
						if (!IsSelected(item)) {
							SelectItemSingle(item);
						}
						LoopEngine.AddLoopAction(OnMouseDrag_ItemContentPanel_ForMove, GLoopCycle.EveryFrame, GWhen.MouseUpRemove);
						yield break;
					}

					yield return new GWait(GTimeUnit.Frame, 1);
				}
			}
			void OnMouseDrag_ItemContentPanel_ForMove() {
				if (selectedItemSet.Count == 0)
					return;

				MoveOrder moveOrder = GetMoveOrder();
				Rectangle movePointer = MainWindow.MoveOrderPointer;
				if (moveOrder != null) {
					if (!MouseInput.LeftHold) {
						HideMoveOrderPointer();
						ApplyMoveOrder(moveOrder);
					} else {
						SetMoveOrderPointer(moveOrder);
					}
				} else {
					movePointer.Visibility = Visibility.Collapsed;
				}

				string selectedName = selectedItemSet.Count == 1 ? selectedItemSet.Select(i=>i.name).ToArray()[0] : $"{selectedItemSet.Count} Items";
				SetMoveOrderCursorText(selectedName);
				UpdateMoveOrderCursor();
			}
		}

		private void SetMoveOrderPointer(MoveOrder moveOrder) {
			Rectangle moveOrderPointer = MainWindow.MoveOrderPointer;
			PMItemView targetView = moveOrder.target.view.Cast<PMItemView>();
			Panel targetContent = targetView.ContentContext;
			Vector2 targetPanelSize = new Vector2((float)targetContent.ActualWidth, (float)targetContent.ActualHeight);

			float panelRelativeTop = (float)moveOrder.target.view.Cast<PMItemView>().TranslatePoint(new Point(), MainWindow.MLItemContext).Y;

			double pointerHeight = targetPanelSize.y * FolderSideEventWeight;
			moveOrderPointer.Width = targetPanelSize.x;
			PMFolder parent = moveOrder.target.parent;
			if (moveOrder.target.type == PMItemType.Motion) {
				moveOrderPointer.Height = pointerHeight;
				switch (moveOrder.direction) {
					case Direction.Top:
						Canvas.SetTop(moveOrderPointer, panelRelativeTop - pointerHeight * 0.5f);
						break;
					case Direction.Bottom:
						Canvas.SetTop(moveOrderPointer, panelRelativeTop + targetPanelSize.y - pointerHeight * 0.5f);
						break;
				}
			} else {
				switch (moveOrder.direction) {
					case Direction.Top:
						moveOrderPointer.Height = pointerHeight = targetPanelSize.y * FolderSideEventWeight;
						Canvas.SetTop(moveOrderPointer, panelRelativeTop - pointerHeight * 0.5f);
						break;
					case Direction.Bottom:
						moveOrderPointer.Height = pointerHeight = targetPanelSize.y * FolderSideEventWeight;
						Canvas.SetTop(moveOrderPointer, panelRelativeTop + targetPanelSize.y - pointerHeight * 0.5f);
						break;
					case Direction.Right:
						moveOrderPointer.Height = pointerHeight = targetPanelSize.y;
						Canvas.SetTop(moveOrderPointer, panelRelativeTop);
						break;
				}
			}

			float posX = 0f;
			if(!moveOrder.target.IsRoot && !parent.IsRoot) {
				posX = (float)parent.view.Cast<PMItemView>().ChildContext.TranslatePoint(new Point(), MainWindow.MLItemContext).X;
			}
			Canvas.SetLeft(moveOrderPointer, posX);

			moveOrderPointer.Visibility = Visibility.Visible;
		}
		private void HideMoveOrderPointer() {
			MainWindow.MoveOrderPointer.Visibility = Visibility.Collapsed;
		}
		private void SetMoveOrderCursorText(string text) {
			MainWindow.MoveOrderCursor.SetNameText(text);
		}
		private void UpdateMoveOrderCursor() {
			MainWindow.MoveOrderCursor.Visibility = MouseInput.LeftHold ? Visibility.Visible : Visibility.Collapsed;
			MainWindow.MoveOrderCursor.Width = MainWindow.MLItemContext.ActualWidth;

			float posY = MouseInput.AbsolutePosition.y - (float)MainWindow.MLItemContext.GetAbsolutePosition().y - (float)MainWindow.MoveOrderCursor.ActualHeight * 0.5f;
			posY = Mathf.Clamp(posY, 0f, (float)file.rootFolder.view.Cast<PMItemView>().ChildContext.ActualHeight);
			Canvas.SetTop(MainWindow.MoveOrderCursor, posY);
		}
		private bool ApplyMoveOrder(MoveOrder moveOrder) {
			List<PMFolder> selectedFolderList = file.itemDict.Where(pair => pair.Value.IsFolder && selectedItemSet.Contains(pair.Value)).Select(pair => pair.Value as PMFolder).ToList();

			//검사
			//최적화하면서 루프 돌 수 있지만, 코드의 가독성을 위해 성능을 희생한다.
			//O(n^2) 의 시간복잡도
			for (int i = 0; i < selectedFolderList.Count; ++i) {
				PMFolder selectedFolder = selectedFolderList[i];
				if (selectedFolder == moveOrder.target) {
					return false;
				}
				if (ContainsChildRecursive(selectedFolder, moveOrder.target)) {
					ToastMessage.Show("자신의 하위 폴더로 이동할 수 없습니다.");
					return false;
				}
			}
			//정렬
			List<PMItemBase> orderedSelectedItemList = new List<PMItemBase>();
			CollectSelectedItemsRecursive(file.rootFolder, orderedSelectedItemList);

			//실행
			foreach (PMItemBase selectedItem in orderedSelectedItemList) {
				selectedItem.parent.childList.Remove(selectedItem);
				PMItemView selectedItemView = selectedItem.view.Cast<PMItemView>();
				selectedItemView.DetachParent();
				if (moveOrder.direction == Direction.Right) {
					PMFolder targetFolder = moveOrder.target.Cast<PMFolder>();
					targetFolder.AddChild(selectedItem);
					selectedItemView.SetParent(targetFolder.view.Cast<PMItemView>().ChildContext);
				} else {
					PMFolder targetFolder = moveOrder.target.parent;
					int targetIndex = targetFolder.childList.IndexOf(moveOrder.target) +
					(moveOrder.direction == Direction.Bottom ? 1 : 0);

					targetFolder.InsertChild(targetIndex, selectedItem);
					targetFolder.view.Cast<PMItemView>().ChildContext.Children.Insert(targetIndex, selectedItem.view.Cast<PMItemView>());
				}
			}
			return true;
		}


		private MoveOrder GetMoveOrder() {
			MoveOrder moveOrder = GetMoveOrderRecursion(file.rootFolder);

			Panel rootPanel = file.rootFolder.view.Cast<PMItemView>().ChildContext;
			float bottomY = rootPanel.GetAbsolutePosition(new Vector2(0f, (float)rootPanel.ActualHeight)).y;
			if (moveOrder == null && MouseInput.AbsolutePosition.y > bottomY) {
				moveOrder = new MoveOrder(file.rootFolder, Direction.Right);
			}
			return moveOrder;
		}
		private MoveOrder GetMoveOrderRecursion(PMItemBase target) {
			//Check current
			if (!target.IsRoot) {
				Panel panel = target.view.Cast<PMItemView>().ContentPanel;
				try {
					if (IsMouseOverY(panel)) {
						float panelTop = panel.GetAbsolutePosition().y;
						MoveOrder moveOrder = new MoveOrder(target);
						if (target.IsFolder) {
							float panelMidTop = panelTop + (float)panel.ActualHeight * FolderSideEventWeight;
							float panelMidBot = panelTop + (float)panel.ActualHeight * FolderMidEventWeight;

							if (MouseInput.AbsolutePosition.y < panelMidTop) {
								moveOrder.direction = Direction.Top;
							} else if (MouseInput.AbsolutePosition.y > panelMidBot) {
								moveOrder.direction = target.Cast<PMFolder>().HasChild ? Direction.Right : Direction.Bottom;
							} else {
								moveOrder.direction = Direction.Right;
							}
							return moveOrder;
						} else {
							float panelMid = panelTop + (float)panel.ActualHeight * 0.5f;

							//선택중인 모션아이템이면 피해서 오더를 설정한다.
							if (selectedItemSet.Contains(moveOrder.target)) {
								int index = moveOrder.target.parent.childList.IndexOf(moveOrder.target);
								for (; ; ) {
									if (selectedItemSet.Contains(moveOrder.target)) {
										moveOrder.direction = Direction.Bottom;
										if (index > 0) {
											--index;
											moveOrder.target = moveOrder.target.parent.childList[index];
										} else {
											//아래로 순회하면서 탐색
											foreach (PMItemBase childItem in moveOrder.target.parent.childList) {
												if (!selectedItemSet.Contains(childItem)) {
													moveOrder.target = childItem;
													moveOrder.direction = Direction.Top;
													return moveOrder;
												}
											}
											//없으면 상위 폴더에 넣는 오더로
											moveOrder.target = moveOrder.target.parent;
											moveOrder.direction = Direction.Right;
											return moveOrder;
										}
									} else {
										return moveOrder;
									}
								}
							}
							moveOrder.direction = MouseInput.AbsolutePosition.y < panelMid ? Direction.Top : Direction.Bottom;
							return moveOrder;
						}
					}
				} catch (Exception ex) {
					GDebug.Log(ex.ToString());
				}
			}
			//Recursive
			if (target.IsFolder) {
				PMFolder folder = target as PMFolder;
				for (int childI = 0; childI < folder.childList.Count; ++childI) {
					MoveOrder moveOrder = GetMoveOrderRecursion(folder.childList[childI]);
					if (moveOrder != null) {
						return moveOrder;
					}
				}
			}
			return null;
		}
		private void CollectSelectedItemsRecursive(PMItemBase item, List<PMItemBase> resultList) {
			if (selectedItemSet.Contains(item)) {
				resultList.Add(item);
			}

			if (item.IsFolder) {
				foreach (PMItemBase child in item.Cast<PMFolder>().childList) {
					CollectSelectedItemsRecursive(child, resultList);
				}
			}
		}

		public bool IsSelected(PMItemBase item) {
			return selectedItemSet.Contains(item);
		}
		private bool IsMouseOverY(Panel itemPanel) {
			float panelTop = itemPanel.GetAbsolutePosition().y;
			return MouseInput.AbsolutePosition.y > panelTop && MouseInput.AbsolutePosition.y < panelTop + itemPanel.ActualHeight;
		}
		private bool ContainsChildRecursive(PMFolder parent, PMItemBase target) {
			foreach (PMItemBase child in parent.childList) {
				if (child == target) {
					return true;
				} else if (child.IsFolder) {
					if (ContainsChildRecursive(child as PMFolder, target)) {
						return true;
					}
				}
			}
			return false;
		}
	}
}
