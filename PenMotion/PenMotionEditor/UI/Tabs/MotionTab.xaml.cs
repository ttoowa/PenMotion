using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
using PenMotion;
using PenMotion.Datas;
using PenMotion.Datas.Items;
using PenMotion.System;
using PenMotionEditor.UI.Items;
using PenMotionEditor.UI.Windows;
using GKit;
using GKit.WPF;
using PenMotion.Datas.Items.Elements;

namespace PenMotionEditor.UI.Tabs {
	public partial class MotionTab : UserControl {
		private EditorContext EditorContext;
		private GLoopEngine LoopEngine => EditorContext.LoopEngine;
		private MotionFile EditingFile => EditorContext.EditingFile;
		private GraphEditorTab GraphEditorTab => EditorContext.GraphEditorTab;

		//Event area
		private const float FolderSideEventWeight = 0.2f;
		private const float FolderMidEventWeight = 1f - FolderSideEventWeight * 2f;

		public MotionFolderItemView RootFolderView {
			get; private set;
		}
		private List<MotionItemBase> itemList;
		public Dictionary<MotionItemBase, MotionItemBaseView> DataToViewDict {
			get; private set;
		}

		//Selected
		public bool IsSelectedItemCopyable {
			get {
				if (selectedItemSet.Count == 0)
					return false;

				foreach (MotionItemBase item in selectedItemSet) {
					if (item.Type == MotionItemType.Motion)
						return true;
				}

				return false;
			}
		}
		public HashSet<MotionItemBase> selectedItemSet;
		public MotionFolderItem SelectedItemParent {
			get {
				if (selectedItemSet.Count == 1) {
					foreach (MotionItemBase item in selectedItemSet) {
						if (item.Type == MotionItemType.Folder) {
							return item.Cast<MotionFolderItem>();
						} else {
							return item.Parent;
						}
					}
				}
				return RootFolderView.Data;
			}
		}

		public MotionTab() {
			InitializeComponent();
		}
		public void Init(EditorContext editorContext) {
			this.EditorContext = editorContext;

			InitMembers();
			InitUI();
			RegisterEvents();
		}
		private void InitMembers() {
			itemList = new List<MotionItemBase>();
			DataToViewDict = new Dictionary<MotionItemBase, MotionItemBaseView>();

			selectedItemSet = new HashSet<MotionItemBase>();
		}
		private void InitUI() {
			DstCursorView.Visibility = Visibility.Collapsed;
			DstItemView.Visibility = Visibility.Collapsed;
		}
		private void RegisterEvents() {
			ControlBar.CreateItemButton_OnClick += CreateItemButton_OnClick;
			ControlBar.CreateFolderButton_OnClick += CreateFolderButton_OnClick;
			ControlBar.CopyItemButton_OnClick += CopyItemButton_OnClick;
			ControlBar.RemoveItemButton_OnClick += RemoveItemButton_OnClick;
		}

		//Events
		internal void EditingFile_ItemCreated(MotionItemBase item, MotionFolderItem parentFolder) {
			if (item == null)
				return;

			MotionItemBaseView view = null;
			switch (item.Type) {
				case MotionItemType.Motion:
					MotionItemView motionView = new MotionItemView(EditorContext, (MotionItem)item);
					view = motionView;

					motionView.UpdatePreviewGraph();
					break;
				case MotionItemType.Folder:
					MotionFolderItemView folderView = new MotionFolderItemView(EditorContext, (MotionFolderItem)item);
					view = folderView;

					if (parentFolder == null) {
						//Root
						folderView.SetRootFolder();
						RootFolderView = folderView;
						ItemStackPanel.Children.Add(folderView);
					}

					//Register events
					MotionFolderItem folderItem = (MotionFolderItem)item;

					folderItem.ChildInserted += folderView.Data_ChildInserted;
					folderItem.ChildRemoved += folderView.Data_ChildRemoved;
					break;
			}

			//Register events
			item.NameChanged += view.Data_NameChanged;

			//Add to collection
			itemList.Add(item);
			DataToViewDict.Add(item, view);

			EditorContext.MarkUnsaved();
		}
		internal void EditingFile_ItemRemoved(MotionItemBase item, MotionFolderItem parentFolder) {
			MotionItemBaseView view = DataToViewDict[item];
			view.DetachParent();

			//Remove from collection
			itemList.Remove(item);
			DataToViewDict.Remove(item);

			if (item.IsRoot) {
				ItemStackPanel.Children.Remove(view);
				RootFolderView = null;
			}

			switch (item.Type) {
				case MotionItemType.Motion:
					break;
				case MotionItemType.Folder:
					//Unregister events
					MotionFolderItemView folderView = (MotionFolderItemView)view;
					MotionFolderItem folderItem = (MotionFolderItem)item;

					folderItem.ChildInserted -= folderView.Data_ChildInserted;
					folderItem.ChildRemoved -= folderView.Data_ChildRemoved;
					break;
			}
			//Unregister events
			item.NameChanged -= view.Data_NameChanged;

			EditorContext.MarkUnsaved();
		}

		private void CreateItemButton_OnClick() {
			MotionItem item = EditingFile.CreateMotionDefault(SelectedItemParent);
			SelectItemSingle(item);
		}
		private void CreateFolderButton_OnClick() {
			MotionFolderItem item = EditingFile.CreateFolder(SelectedItemParent);
			SelectItemSingle(item);
		}
		private void RemoveItemButton_OnClick() {
			foreach (MotionItemBase item in selectedItemSet) {
				EditingFile.RemoveItem(item);
			}
			UnselectItemAll();
		}
		private void CopyItemButton_OnClick() {
			DuplicateSelectedMotion();

			EditorContext.MarkUnsaved();
		}
		private void OnSelectItemChanged() {
			ControlBar.CopyItemButton.IsEnabled = IsSelectedItemCopyable;
		}

		public void ClearItems() {
			UnselectItemAll();
			itemList.Clear();
			ItemStackPanel.Children.Clear();
		}

		public void DuplicateSelectedMotion() {
			if (!IsSelectedItemCopyable)
				return;

			MotionItemBase latestNewItem = null;
			MotionFolderItem parentFolder = SelectedItemParent;
			foreach (MotionItemBase refItem in selectedItemSet) {
				if (refItem.Type == MotionItemType.Motion) {
					MotionItem refMotionItem = (MotionItem)refItem;
					//Create motion
					MotionItem newItem = EditingFile.CreateMotionEmpty(parentFolder);
					latestNewItem = newItem;

					//Copy points
					foreach (MotionPoint refPoint in refMotionItem.pointList) {
						MotionPoint point = new MotionPoint();
						newItem.AddPoint(point);

						point.SetMainPoint(refPoint.MainPoint);
						for (int i = 0; i < refPoint.SubPoints.Length; ++i) {
							point.SetSubPoint(i, refPoint.SubPoints[i]);
						}
					}

					//Set name
					const string CopyPostfix = "_Copy";
					string name = refItem.Name;
					for (; ; ) {
						if (EditingFile.itemDict.ContainsKey(name)) {
							name += CopyPostfix;
						} else {
							break;
						}
					}
					newItem.SetName(name);

				}
			}
			if (latestNewItem != null) {
				SelectItemSingle(latestNewItem);
			}
		}

		public void UpdateItemPreviews() {
			foreach (MotionItemBase item in itemList) {
				if (item.Type == MotionItemType.Motion) {
					((MotionItemView)DataToViewDict[item]).UpdatePreviewGraph();
				}
			}
		}

		//Select
		public void SelectItemSingle(MotionItemBase item) {
			UnselectItemAll();
			SelectItemAdd(item);
		}
		public void SelectItemAdd(MotionItemBase item) {
			DataToViewDict[item].SetSelected(true);
			selectedItemSet.Add(item);

			if (item.Type == MotionItemType.Motion) {
				EditorContext.GraphEditorTab.AttachMotion((MotionItem)item);
				EditorContext.PreviewTab.ResetPreviewTime();
			} else {
				EditorContext.GraphEditorTab.DetachMotion();
			}

			OnSelectItemChanged();
		}
		public void UnselectItemSingle(MotionItemBase item) {
			DataToViewDict[item].SetSelected(false);
			selectedItemSet.Remove(item);
			GraphEditorTab.DetachMotion();

			if (selectedItemSet.Count > 0) {
				MotionItemBase lastSelectedItemView = selectedItemSet.Last();
				if (lastSelectedItemView.Type == MotionItemType.Motion) {
					GraphEditorTab.AttachMotion((MotionItem)lastSelectedItemView);
				}
			}

			OnSelectItemChanged();
		}
		public void UnselectItemAll() {
			foreach (MotionItemBase item in selectedItemSet) {
				if(DataToViewDict.ContainsKey(item)) {
					DataToViewDict[item].SetSelected(false);
				}
			}
			selectedItemSet.Clear();
			GraphEditorTab.DetachMotion();

			OnSelectItemChanged();
		}

		//ItemMove UI
		public void UpdateDstCursorView(MoveOrder moveOrder) {
			MotionItemBaseView targetView = DataToViewDict[moveOrder.target];
			Panel targetContent = targetView.ContentContext;
			Vector2 targetPanelSize = new Vector2((float)targetContent.ActualWidth, (float)targetContent.ActualHeight);

			float panelRelativeTop = (float)targetView.TranslatePoint(new Point(), ItemStackPanel).Y;

			double pointerHeight = targetPanelSize.y * FolderSideEventWeight;
			DstCursorView.Width = targetPanelSize.x;
			MotionItemBase parent = moveOrder.target.Parent;
			if (moveOrder.target.Type == MotionItemType.Motion) {
				DstCursorView.Height = pointerHeight;
				switch (moveOrder.direction) {
					case Direction.Top:
						Canvas.SetTop(DstCursorView, panelRelativeTop - pointerHeight * 0.5f);
						break;
					case Direction.Bottom:
						Canvas.SetTop(DstCursorView, panelRelativeTop + targetPanelSize.y - pointerHeight * 0.5f);
						break;
				}
			} else {
				switch (moveOrder.direction) {
					case Direction.Top:
						DstCursorView.Height = pointerHeight = targetPanelSize.y * FolderSideEventWeight;
						Canvas.SetTop(DstCursorView, panelRelativeTop - pointerHeight * 0.5f);
						break;
					case Direction.Bottom:
						DstCursorView.Height = pointerHeight = targetPanelSize.y * FolderSideEventWeight;
						Canvas.SetTop(DstCursorView, panelRelativeTop + targetPanelSize.y - pointerHeight * 0.5f);
						break;
					case Direction.Right:
						DstCursorView.Height = pointerHeight = targetPanelSize.y;
						Canvas.SetTop(DstCursorView, panelRelativeTop);
						break;
				}
			}

			float posX = 0f;
			if (!moveOrder.target.IsRoot && !parent.IsRoot) {
				posX = (float)DataToViewDict[parent].ChildStackPanel.TranslatePoint(new Point(), ItemStackPanel).X;
			}
			Canvas.SetLeft(DstCursorView, posX);

			DstCursorView.Visibility = Visibility.Visible;
		}
		public void HideDstCursorView() {
			DstCursorView.Visibility = Visibility.Collapsed;
		}
		public void UpdateDstItemText(string text) {
			DstItemView.SetNameText(text);
		}
		public void UpdateDstItemView() {
			DstItemView.Visibility = MouseInput.Left.Hold ? Visibility.Visible : Visibility.Collapsed;
			DstItemView.Width = ItemStackPanel.ActualWidth;

			float posY = MouseInput.AbsolutePosition.y - (float)ItemStackPanel.GetAbsolutePosition().y - (float)DstItemView.ActualHeight * 0.5f;
			posY = Mathf.Clamp(posY, 0f, (float)RootFolderView.ChildStackPanel.ActualHeight);
			Canvas.SetTop(DstItemView, posY);
		}

		public bool ApplyMoveOrder(MoveOrder moveOrder) {
			MotionFolderItem[] selectedFolders =
				itemList.Where(item => item.Type == MotionItemType.Folder && selectedItemSet.Contains(item)).Select(item => item.Cast<MotionFolderItem>()).ToArray();

			//검사
			//최적화하면서 루프 돌 수 있지만, 코드의 가독성을 위해 성능을 희생한다.
			//O(n^2) 의 시간복잡도
			foreach (MotionFolderItem selectedFolder in selectedFolders) {
				if (selectedFolder == moveOrder.target)
					return false;

				if (IsContainsChildRecursive(selectedFolder, moveOrder.target)) {
					ToastMessage.Show("자신의 하위 폴더로 이동할 수 없습니다.");
					return false;
				}
			}

			//정렬
			MotionItemBase[] selectedItems = CollectSelectedItems();

			//실행
			foreach (MotionItemBase item in selectedItems) {
				item.Parent.RemoveChild(item);

				if (moveOrder.direction == Direction.Right) {
					//폴더 내부로
					MotionFolderItem targetFolder = moveOrder.target.Cast<MotionFolderItem>();
					targetFolder.AddChild(item);
				} else {
					//아이템 위로
					MotionFolderItem targetFolder = moveOrder.target.Parent;
					int targetIndex = targetFolder.childList.IndexOf(moveOrder.target) +
						(moveOrder.direction == Direction.Bottom ? 1 : 0);

					targetFolder.InsertChild(targetIndex, item);
				}
			}
			return true;
		}

		public MoveOrder GetMoveOrder() {
			MoveOrder moveOrder = GetMoveOrderRecursion(RootFolderView.Data);

			Panel rootPanel = RootFolderView.ChildStackPanel;
			float bottomY = rootPanel.GetAbsolutePosition(new Vector2(0f, (float)rootPanel.ActualHeight)).y;
			if (moveOrder == null && MouseInput.AbsolutePosition.y > bottomY) {
				moveOrder = new MoveOrder(RootFolderView.Data, Direction.Right);
			}
			return moveOrder;
		}
		private MoveOrder GetMoveOrderRecursion(MotionItemBase searchTarget) {
			//Check current
			if (!searchTarget.IsRoot) {
				Panel panel = DataToViewDict[searchTarget].ContentPanel;
				try {
					if (IsMouseOverY(panel)) {
						float panelTop = panel.GetAbsolutePosition().y;
						MoveOrder moveOrder = new MoveOrder(searchTarget);
						if (searchTarget.Type == MotionItemType.Folder) {
							//폴더이면 3분할 해서 오더 결정

							float panelMidTop = panelTop + (float)panel.ActualHeight * FolderSideEventWeight;
							float panelMidBot = panelTop + (float)panel.ActualHeight * FolderMidEventWeight;

							if (MouseInput.AbsolutePosition.y < panelMidTop) {
								moveOrder.direction = Direction.Top;
							} else if (MouseInput.AbsolutePosition.y > panelMidBot) {
								moveOrder.direction = searchTarget.Cast<MotionFolderItem>().HasChild ? Direction.Right : Direction.Bottom;
							} else {
								moveOrder.direction = Direction.Right;
							}
							return moveOrder;
						} else {
							float panelMid = panelTop + (float)panel.ActualHeight * 0.5f;

							//선택중인 모션아이템이면 피해서 오더를 설정한다.
							if (selectedItemSet.Contains(moveOrder.target)) {
								int index = moveOrder.target.Parent.childList.IndexOf(moveOrder.target);
								for (; ; ) {
									if (selectedItemSet.Contains(moveOrder.target)) {
										moveOrder.direction = Direction.Bottom;
										if (index > 0) {
											--index;
											moveOrder.target = moveOrder.target.Parent.childList[index];
										} else {
											//아래로 순회하면서 탐색
											foreach (MotionItemBase childItem in moveOrder.target.Parent.childList) {
												if (!selectedItemSet.Contains(childItem)) {
													moveOrder.target = childItem;
													moveOrder.direction = Direction.Top;
													return moveOrder;
												}
											}

											//없으면 상위 폴더에 넣는 오더로
											moveOrder.target = moveOrder.target.Parent;
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
			if (searchTarget.Type == MotionItemType.Folder) {
				foreach (MotionItemBase itemView in searchTarget.Cast<MotionFolderItem>().childList) {
					MoveOrder moveOrder = GetMoveOrderRecursion(itemView);
					if (moveOrder != null) {
						return moveOrder;
					}
				}
			}
			return null;
		}
		private MotionItemBase[] CollectSelectedItems() {
			List<MotionItemBase> resultList = new List<MotionItemBase>();

			CollectSelectedItemsRecursion(RootFolderView.Data);

			return resultList.ToArray();

			void CollectSelectedItemsRecursion(MotionItemBase targetItem) {
				//Collect
				if (selectedItemSet.Contains(targetItem)) {
					resultList.Add(targetItem);
				}

				//Recursion
				if (targetItem.Type == MotionItemType.Folder) {
					foreach (MotionItemBase child in targetItem.Cast<MotionFolderItem>().childList) {
						CollectSelectedItemsRecursion(child);
					}
				}
			}
		}

		public bool IsSelected(MotionItemBase item) {
			return selectedItemSet.Contains(item);
		}
		private bool IsMouseOverY(Panel itemPanel) {
			float panelTop = itemPanel.GetAbsolutePosition().y;
			return MouseInput.AbsolutePosition.y > panelTop && MouseInput.AbsolutePosition.y < panelTop + itemPanel.ActualHeight;
		}

		private bool IsContainsChildRecursive(MotionFolderItem parentFolder, MotionItemBase target) {
			foreach (MotionItemBase item in parentFolder.childList) {
				if (item == target) {
					return true;
				} else if (item.Type == MotionItemType.Folder) {
					//Recursion
					if (IsContainsChildRecursive(item.Cast<MotionFolderItem>(), target)) {
						return true;
					}
				}
			}
			return false;
		}
	}
}
