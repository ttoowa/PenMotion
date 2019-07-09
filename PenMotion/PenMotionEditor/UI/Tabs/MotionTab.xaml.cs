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
using PendulumMotion;
using PendulumMotion.Components;
using PendulumMotion.Components.Items;
using PendulumMotion.System;
using PenMotionEditor.UI.Items;
using PenMotionEditor.UI.Windows;
using GKit;
using GKit.WPF;
using PendulumMotion.Components.Items.Elements;

namespace PenMotionEditor.UI.Tabs {
	public partial class MotionTab : UserControl {
		private EditorContext EditorContext;
		private GLoopEngine LoopEngine => EditorContext.LoopEngine;
		private MotionFile EditingFile => EditorContext.EditingFile;
		private GraphEditorTab GraphEditorTab => EditorContext.GraphEditorTab;

		//Event area
		private const float FolderSideEventWeight = 0.2f;
		private const float FolderMidEventWeight = 1f - FolderSideEventWeight * 2f;

		public List<MotionItemBaseView> motionItemList;

		public MotionFolderItemView RootFolderView {
			get; private set;
		}

		//Selected
		public bool IsSelectedItemCopyable {
			get {
				if (selectedItemSet.Count == 0)
					return false;

				foreach (MotionItemBaseView item in selectedItemSet) {
					if (item.Type == MotionItemType.Motion)
						return true;
				}

				return false;
			}
		}
		public HashSet<MotionItemBaseView> selectedItemSet;
		public MotionFolderItemView SelectedItemParent {
			get {
				if (selectedItemSet.Count == 1) {
					foreach (MotionItemBaseView item in selectedItemSet) {
						if (item.Type == MotionItemType.Folder) {
							return item.Cast<MotionFolderItemView>();
						} else {
							return item.ParentItemView;
						}
					}
				}
				return RootFolderView;
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
			motionItemList = new List<MotionItemBaseView>();

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


		private void CreateItemButton_OnClick() {
			MotionItemBaseView motion = CreateMotion(SelectedItemParent);
			SelectItemSingle(motion);

			EditorContext.MarkUnsaved();
		}
		private void CreateFolderButton_OnClick() {
			MotionItemBaseView folder = CreateFolder(SelectedItemParent);
			SelectItemSingle(folder);

			EditorContext.MarkUnsaved();
		}
		private void RemoveItemButton_OnClick() {
			foreach (MotionItemBaseView item in selectedItemSet) {
				RemoveItem(item);
			}
			UnselectItemAll();

			EditorContext.MarkUnsaved();
		}
		private void CopyItemButton_OnClick() {
			DuplicateSelectedMotion();

			EditorContext.MarkUnsaved();
		}
		private void OnSelectItemChanged() {
			ControlBar.CopyItemButton.IsEnabled = IsSelectedItemCopyable;
		}

		public MotionItemView CreateMotion(MotionFolderItemView parent) {
			MotionItemView motion = new MotionItemView(EditorContext);
			parent.AddChild(motion);
			return motion;
		}
		public MotionFolderItemView CreateFolder(MotionFolderItemView parent) {
			MotionFolderItemView folder = new MotionFolderItemView(EditorContext);
			parent.AddChild(folder);
			return folder;
		}
		public void RemoveItem(MotionItemBaseView item) {
			if (item.ParentItemView != null) {
				item.ParentItemView.RemoveChild(item);
			}
		}
		public void ClearItems() {
			ItemStackPanel.Children.Clear();
		}

		public void DuplicateSelectedMotion() {
			if (!IsSelectedItemCopyable)
				return;

			MotionItemBaseView latestNewItem = null;
			MotionFolderItemView parent = SelectedItemParent;
			foreach (MotionItemBaseView refItem in selectedItemSet) {
				if (refItem.Type == MotionItemType.Motion) {
					MotionItemView refMotionItem = refItem.Cast<MotionItemView>();
					//Create motion
					MotionItemView newItemView = CreateMotion(parent);
					latestNewItem = newItemView;
					newItemView.Data.pointList.Clear();

					//Copy points
					foreach (MotionPoint refPoint in refMotionItem.Data.pointList) {
						MotionPoint point = new MotionPoint();
						newItemView.Data.AddPoint(point);

						point.mainPoint = refPoint.mainPoint;
						point.subPoints = refPoint.subPoints.ToArray();
					}

					//Set name
					const string copyPostfix = "_Copy";
					string name = refItem.Data.name;
					for (; ; ) {
						if (EditingFile.itemDict.ContainsKey(name)) {
							name += copyPostfix;
						} else {
							break;
						}
					}
					newItemView.SetName(name);

				}
			}
			if (latestNewItem != null) {
				SelectItemSingle(latestNewItem);
			}
		}

		public void SelectItemSingle(MotionItemBaseView itemView) {
			UnselectItemAll();
			SelectItemAdd(itemView);
		}
		public void SelectItemAdd(MotionItemBaseView itemView) {
			itemView.SetSelected(true);
			selectedItemSet.Add(itemView);

			if (itemView.Type == MotionItemType.Motion) {
				EditorContext.GraphEditorTab.AttachMotion((MotionItemView)itemView);
				EditorContext.PreviewTab.ResetPreviewTime();
			} else {
				EditorContext.GraphEditorTab.DetachMotion();
			}

			OnSelectItemChanged();
		}
		public void UnselectItemSingle(MotionItemBaseView item) {
			item.SetSelected(false);
			selectedItemSet.Remove(item);
			GraphEditorTab.DetachMotion();

			if (selectedItemSet.Count > 0) {
				MotionItemBaseView lastSelectedItemView = selectedItemSet.Last();
				if(lastSelectedItemView.Type == MotionItemType.Motion) {
					GraphEditorTab.AttachMotion((MotionItemView)lastSelectedItemView);
				}
			}

			OnSelectItemChanged();
		}
		public void UnselectItemAll() {
			foreach (MotionItemBaseView item in selectedItemSet) {
				item.SetSelected(false);
			}
			selectedItemSet.Clear();
			GraphEditorTab.DetachMotion();

			OnSelectItemChanged();
		}

		//private IEnumerator UpdateItemPreviewRoutine() {
		//	//UpdateItemPreview
		//	int iterCounter = 0;
		//	for (; ; ) {
		//		if (OnEditing) {
		//			yield return UpdateItemPreview(editingFile.file.rootFolder);
		//		}
		//		yield return new GWait(GTimeUnit.Frame, 1);
		//	}

		//	IEnumerator UpdateItemPreview(PMFolder folder) {
		//		for (int childI = 0; childI < folder.childList.Count; ++childI) {
		//			PMItemBase child = folder.childList[childI];
		//			switch (child.type) {
		//				case PMItemType.Motion:
		//					PMMotion motion = child.Cast<PMMotion>();
		//					motion.view.Cast<PMItemView>().UpdatePreviewGraph(motion);

		//					if (++iterCounter >= 2) {
		//						iterCounter = 0;
		//						yield return new GWait(GTimeUnit.Frame, 1);
		//					}
		//					break;
		//				case PMItemType.Folder:
		//					yield return UpdateItemPreview(child.Cast<PMFolder>());
		//					break;
		//			}
		//		}
		//	}
		//}

		public void UpdateDstCursorView(MoveOrder moveOrder) {
			Panel targetContent = moveOrder.target.ContentContext;
			Vector2 targetPanelSize = new Vector2((float)targetContent.ActualWidth, (float)targetContent.ActualHeight);

			float panelRelativeTop = (float)moveOrder.target.TranslatePoint(new Point(), ItemStackPanel).Y;

			double pointerHeight = targetPanelSize.y * FolderSideEventWeight;
			DstCursorView.Width = targetPanelSize.x;
			MotionItemBaseView parent = moveOrder.target.ParentItemView;
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
				posX = (float)parent.ChildStackPanel.TranslatePoint(new Point(), ItemStackPanel).X;
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
			MotionFolderItemView[] selectedFolders =
				motionItemList.Where(item => item.Type == MotionItemType.Folder && selectedItemSet.Contains(item)).Select(item => item.Cast<MotionFolderItemView>()).ToArray();

			//검사
			//최적화하면서 루프 돌 수 있지만, 코드의 가독성을 위해 성능을 희생한다.
			//O(n^2) 의 시간복잡도
			foreach (MotionFolderItemView itemView in selectedFolders) {
				if (itemView == moveOrder.target)
					return false;

				if (ContainsChildRecursive(itemView, moveOrder.target)) {
					ToastMessage.Show("자신의 하위 폴더로 이동할 수 없습니다.");
					return false;
				}
			}

			//정렬
			MotionItemBaseView[] selectedItems = CollectSelectedItems();

			//실행
			foreach (MotionItemBaseView item in selectedItems) {
				item.ParentItemView.RemoveChild(item);

				if (moveOrder.direction == Direction.Right) {
					//폴더 내부로
					MotionFolderItemView targetFolder = moveOrder.target.Cast<MotionFolderItemView>();
					targetFolder.AddChild(item);
				} else {
					//아이템 위로
					MotionFolderItemView targetFolder = moveOrder.target.ParentItemView;
					int targetIndex = targetFolder.childList.IndexOf(moveOrder.target) +
						(moveOrder.direction == Direction.Bottom ? 1 : 0);

					targetFolder.InsertChild(targetIndex, item);
				}
			}
			return true;
		}

		public MoveOrder GetMoveOrder() {
			MoveOrder moveOrder = GetMoveOrderRecursion(RootFolderView);

			Panel rootPanel = RootFolderView.ChildStackPanel;
			float bottomY = rootPanel.GetAbsolutePosition(new Vector2(0f, (float)rootPanel.ActualHeight)).y;
			if (moveOrder == null && MouseInput.AbsolutePosition.y > bottomY) {
				moveOrder = new MoveOrder(RootFolderView, Direction.Right);
			}
			return moveOrder;
		}
		private MoveOrder GetMoveOrderRecursion(MotionItemBaseView searchTarget) {
			//Check current
			if (!searchTarget.IsRoot) {
				Panel panel = searchTarget.ContentPanel;
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
								int index = moveOrder.target.ParentItemView.childList.IndexOf(moveOrder.target);
								for (; ; ) {
									if (selectedItemSet.Contains(moveOrder.target)) {
										moveOrder.direction = Direction.Bottom;
										if (index > 0) {
											--index;
											moveOrder.target = moveOrder.target.ParentItemView.childList[index];
										} else {
											//아래로 순회하면서 탐색
											foreach (MotionItemBaseView childItem in moveOrder.target.ParentItemView.childList) {
												if (!selectedItemSet.Contains(childItem)) {
													moveOrder.target = childItem;
													moveOrder.direction = Direction.Top;
													return moveOrder;
												}
											}

											//없으면 상위 폴더에 넣는 오더로
											moveOrder.target = moveOrder.target.ParentItemView;
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
				foreach (MotionItemBaseView itemView in searchTarget.Cast<MotionFolderItemView>().childList) {
					MoveOrder moveOrder = GetMoveOrderRecursion(itemView);
					if (moveOrder != null) {
						return moveOrder;
					}
				}
			}
			return null;
		}
		private MotionItemBaseView[] CollectSelectedItems() {
			List<MotionItemBaseView> resultList = new List<MotionItemBaseView>();

			CollectSelectedItemsRecursion(RootFolderView);

			return resultList.ToArray();

			void CollectSelectedItemsRecursion(MotionItemBaseView targetItemView) {
				//Collect
				if (selectedItemSet.Contains(targetItemView)) {
					resultList.Add(targetItemView);
				}

				//Recursion
				if (targetItemView.Type == MotionItemType.Folder) {
					foreach (MotionItemBaseView child in targetItemView.Cast<MotionFolderItemView>().childList) {
						CollectSelectedItemsRecursion(child);
					}
				}
			}
		}

		public bool IsSelected(MotionItemBaseView item) {
			return selectedItemSet.Contains(item);
		}
		private bool IsMouseOverY(Panel itemPanel) {
			float panelTop = itemPanel.GetAbsolutePosition().y;
			return MouseInput.AbsolutePosition.y > panelTop && MouseInput.AbsolutePosition.y < panelTop + itemPanel.ActualHeight;
		}
		private bool ContainsChildRecursive(MotionFolderItemView parent, MotionItemBaseView target) {
			foreach (MotionItemBaseView itemView in parent.childList) {
				if (itemView == target) {
					return true;
				} else if (itemView.Type == MotionItemType.Folder) {
					//Recursion
					if (ContainsChildRecursive(itemView.Cast<MotionFolderItemView>(), target)) {
						return true;
					}
				}
			}
			return false;
		}
	}
}
