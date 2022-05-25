using GKitForWPF;
using GKitForWPF.UI.Controls;
using PenMotion.Datas;
using PenMotion.Datas.Items;
using PenMotion.Datas.Items.Elements;
using PenMotionEditor.UI.Elements;
using PenMotionEditor.UI.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace PenMotionEditor.UI.Tabs {
	public partial class MotionTab : UserControl {
		private MotionEditorContext EditorContext;
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
				if (MotionTreeView.SelectedItemSet.Count == 0)
					return false;

				foreach (ITreeItem item in MotionTreeView.SelectedItemSet) {
					if (item is MotionItemView)
						return true;
				}

				return false;
			}
		}
		public MotionFolderItemView SelectedItemParentView {
			get {
				ITreeFolder selectedItemParent = MotionTreeView.SelectedItemParent;
				return selectedItemParent is MotionFolderItemView ? (MotionFolderItemView)selectedItemParent : null;
			}
		}
		public MotionFolderItem SelectedItemParent {
			get {
				MotionFolderItemView selectedItemParentView = SelectedItemParentView;
				return selectedItemParentView != null ? selectedItemParentView.Data : null;
			}
		}

		public MotionTab() {
			InitializeComponent();
		}
		public void Init(MotionEditorContext editorContext) {
			this.EditorContext = editorContext;

			InitMembers();
			RegisterEvents();
		}
		private void InitMembers() {
			itemList = new List<MotionItemBase>();
			DataToViewDict = new Dictionary<MotionItemBase, MotionItemBaseView>();

			MotionTreeView.AutoApplyItemMove = false;
		}
		private void RegisterEvents() {
			ControlBar.CreateItemButtonClick += CreateItemButton_OnClick;
			ControlBar.CreateFolderButtonClick += CreateFolderButton_OnClick;
			ControlBar.CopyItemButtonClick += CopyItemButton_OnClick;
			ControlBar.RemoveItemButtonClick += RemoveItemButton_OnClick;

			MotionTreeView.SelectedItemSet.SelectionAdded += SelectedItemSet_SelectionAdded;
			MotionTreeView.SelectedItemSet.SelectionRemoved += SelectedItemSet_SelectionRemoved;
			MotionTreeView.ItemMoved += MotionListView_ItemMoved;
			MotionTreeView.MessageOccured += MotionListView_MessageOccured;
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
						//Create root
						folderView.SetRootFolder();
						RootFolderView = folderView;

						MotionTreeView.ChildItemCollection.Add(folderView);
						MotionTreeView.ManualRootFolder = folderView;
					}

					//Register events
					MotionFolderItem folderItem = (MotionFolderItem)item;

					folderItem.ChildInserted += Data_ChildInserted;
					folderItem.ChildRemoved += Data_ChildRemoved;

					void Data_ChildInserted(int index, MotionItemBase childItem) {
						MotionItemBaseView childItemView = DataToViewDict[childItem];
						folderView.ChildItemCollection.Insert(index, childItemView);
					}
					void Data_ChildRemoved(MotionItemBase childItem) {
						MotionItemBaseView childItemView = DataToViewDict[childItem];
						folderView.ChildItemCollection.Remove(childItemView);
					}
					break;
			}
			if (parentFolder != null) {
				view.ParentItem = (MotionFolderItemView)DataToViewDict[parentFolder];
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
			if (view.ParentItem != null) {
				view.ParentItem.ChildItemCollection.Remove(view);
				view.ParentItem = null;
			}

			//Remove from collection
			itemList.Remove(item);
			DataToViewDict.Remove(item);

			if (item.IsRoot) {
				MotionTreeView.ChildItemCollection.Remove(view);
				RootFolderView = null;
			}

			//Unregister events
			//어차피 삭제이후 사용되지 않으므로 참조가 어려운 이벤트는 생략한다.
			item.NameChanged -= view.Data_NameChanged;

			EditorContext.MarkUnsaved();
		}

		private void CreateItemButton_OnClick() {
			MotionItem item = EditingFile.CreateMotionDefault(SelectedItemParent);
			MotionTreeView.SelectedItemSet.SetSelectedItem((MotionItemView)DataToViewDict[item]);
		}
		private void CreateFolderButton_OnClick() {
			MotionFolderItem item = EditingFile.CreateFolder(SelectedItemParent);
			MotionTreeView.SelectedItemSet.SetSelectedItem((MotionFolderItemView)DataToViewDict[item]);
		}
		private void RemoveItemButton_OnClick() {
			foreach (MotionItemBase item in MotionTreeView.SelectedItemSet.ToArray().Select(item => ((MotionItemBaseView)item).Data)) {
				MotionTreeView.SelectedItemSet.RemoveSelectedItem(DataToViewDict[item]);
				EditingFile.RemoveItem(item);
			}
		}
		private void CopyItemButton_OnClick() {
			DuplicateSelectedMotion();

			EditorContext.MarkUnsaved();
		}

		private void SelectedItemSet_SelectionRemoved(ISelectable item) {
			SelectionItemChanged();
		}
		private void SelectedItemSet_SelectionAdded(ISelectable item) {
			SelectionItemChanged();
		}
		private void SelectionItemChanged() {
			ControlBar.CopyItemButton.IsEnabled = IsSelectedItemCopyable;
			UpdateFocusItem();
		}

		private void MotionListView_ItemMoved(ITreeItem item, ITreeFolder oldParent, ITreeFolder newParent, int index) {
			MotionItemBase itemData = ((MotionItemBaseView)item).Data;
			MotionFolderItemView newParentFolderView = (MotionFolderItemView)newParent;

			if (oldParent != null) {
				MotionFolderItemView oldParentFolderView = (MotionFolderItemView)oldParent;
				oldParentFolderView.Data.RemoveChild(itemData);
			}
			newParentFolderView.Data.InsertChild(index, itemData);
		}
		private void MotionListView_MessageOccured(string message) {
			ToastMessage.Show(message);
		}

		public void ClearItems() {
			MotionTreeView.SelectedItemSet.UnselectItems();
			itemList.Clear();
			MotionTreeView.ChildItemCollection.Clear();
		}

		public void DuplicateSelectedMotion() {
			if (!IsSelectedItemCopyable)
				return;

			MotionItemBase latestNewItem = null;
			MotionFolderItem parentFolder = SelectedItemParent;
			foreach (MotionItemBaseView refItem in MotionTreeView.SelectedItemSet) {
				if (refItem.Type == MotionItemType.Motion) {
					MotionItem refMotionItem = (MotionItem)refItem.Data;
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

					((MotionItemView)DataToViewDict[newItem]).UpdatePreviewGraph();

					//Set name
					const string CopyPostfix = "_Copy";
					string name = refItem.Data.Name;
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
				MotionTreeView.SelectedItemSet.SetSelectedItem(DataToViewDict[latestNewItem]);
			}
		}

		private void UpdateFocusItem() {
			if (MotionTreeView.SelectedItemSet.Count == 1) {
				MotionItemBaseView itemBaseView = (MotionItemBaseView)MotionTreeView.SelectedItemSet.Last;

				if (itemBaseView.Type == MotionItemType.Motion) {
					EditorContext.GraphEditorTab.AttachMotion((MotionItem)itemBaseView.Data);
					EditorContext.PreviewTab.ResetPreviewTime();

					return;
				}
			}

			EditorContext.GraphEditorTab.DetachMotion();
		}
		public void UpdateItemPreviews() {
			foreach (MotionItemBase item in itemList) {
				if (item.Type == MotionItemType.Motion) {
					((MotionItemView)DataToViewDict[item]).UpdatePreviewGraph();
				}
			}
		}

		private MotionItemBase ToMotionItemBase(ITreeItem item) {
			return ((MotionItemBaseView)item).Data;
		}
		private MotionItem ToMotionItem(ITreeItem item) {
			return ((MotionItemView)item).Data;
		}
		private MotionFolderItem ToFolderItem(ITreeFolder item) {
			return ((MotionFolderItemView)item).Data;
		}
	}
}
