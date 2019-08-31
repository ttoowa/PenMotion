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
using GKit.WPF.UI.Controls;

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
				if (MotionListView.SelectedItemSet.Count == 0)
					return false;

				foreach (IListItem item in MotionListView.SelectedItemSet) {
					if (item is MotionItemView)
						return true;
				}

				return false;
			}
		}
		public MotionFolderItemView SelectedItemParentView {
			get {
				IListFolder selectedItemParent = MotionListView.SelectedItemParent;
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
		public void Init(EditorContext editorContext) {
			this.EditorContext = editorContext;

			InitMembers();
			RegisterEvents();
		}
		private void InitMembers() {
			itemList = new List<MotionItemBase>();
			DataToViewDict = new Dictionary<MotionItemBase, MotionItemBaseView>();
		}
		private void InitUI() {
			MotionListView.AutoApplyItemMove = false;
		}
		private void RegisterEvents() {
			ControlBar.CreateItemButton_OnClick += CreateItemButton_OnClick;
			ControlBar.CreateFolderButton_OnClick += CreateFolderButton_OnClick;
			ControlBar.CopyItemButton_OnClick += CopyItemButton_OnClick;
			ControlBar.RemoveItemButton_OnClick += RemoveItemButton_OnClick;

			MotionListView.SelectedItemSet.SelectionAdded += SelectedItemSet_SelectionAdded;
			MotionListView.SelectedItemSet.SelectionRemoved += SelectedItemSet_SelectionRemoved;
			MotionListView.ItemMoved += MotionListView_ItemMoved;
			MotionListView.MessageOccured += MotionListView_MessageOccured;
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

					if(parentFolder == null) {
						//Create root
						folderView.SetRootFolder();
						RootFolderView = folderView;

						MotionListView.ChildItemCollection.Add(folderView);
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
				MotionListView.ChildItemCollection.Remove(view);
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
			MotionListView.SelectedItemSet.SetSelectedItem((MotionItemView)DataToViewDict[item]);
		}
		private void CreateFolderButton_OnClick() {
			MotionFolderItem item = EditingFile.CreateFolder(SelectedItemParent);
			MotionListView.SelectedItemSet.SetSelectedItem((MotionFolderItemView)DataToViewDict[item]);
		}
		private void RemoveItemButton_OnClick() {
			foreach (MotionItemBase item in MotionListView.SelectedItemSet.ToArray().Select(item => ((MotionItemBaseView)item).Data)) {
				MotionListView.SelectedItemSet.RemoveSelectedItem(DataToViewDict[item]);
				EditingFile.RemoveItem(item);
			}
		}
		private void CopyItemButton_OnClick() {
			DuplicateSelectedMotion();

			EditorContext.MarkUnsaved();
		}

		private void SelectedItemSet_SelectionRemoved(IListItem item) {
			SelectionItemChanged();
		}
		private void SelectedItemSet_SelectionAdded(IListItem item) {
			SelectionItemChanged();
		}
		private void SelectionItemChanged() {
			ControlBar.CopyItemButton.IsEnabled = IsSelectedItemCopyable;
			UpdateFocusItem();
		}

		private void MotionListView_ItemMoved(IListItem item, IListFolder oldParent, IListFolder newParent, int index) {
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
			MotionListView.SelectedItemSet.UnselectItems();
			itemList.Clear();
			MotionListView.ChildItemCollection.Clear();
		}

		public void DuplicateSelectedMotion() {
			if (!IsSelectedItemCopyable)
				return;

			MotionItemBase latestNewItem = null;
			MotionFolderItem parentFolder = SelectedItemParent;
			foreach (MotionItemBaseView refItem in MotionListView.SelectedItemSet) {
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
				MotionListView.SelectedItemSet.SetSelectedItem(DataToViewDict[latestNewItem]);
			}
		}

		private void UpdateFocusItem() {
			if (MotionListView.SelectedItemSet.Count == 1) {
				MotionItemBaseView itemBaseView = (MotionItemBaseView)MotionListView.SelectedItemSet.Last;
				
				if(itemBaseView.Type == MotionItemType.Motion) {
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

		private MotionItemBase ToMotionItemBase(IListItem item) {
			return ((MotionItemBaseView)item).Data;
		}
		private MotionItem ToMotionItem(IListItem item) {
			return ((MotionItemView)item).Data;
		}
		private MotionFolderItem ToFolderItem(IListFolder item) {
			return ((MotionFolderItemView)item).Data;
		}
	}
}
