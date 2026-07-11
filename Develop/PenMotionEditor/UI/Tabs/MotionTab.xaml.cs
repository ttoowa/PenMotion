using GKitForWPF;
using GKitForWPF.UI.Controls;
using PenMotion.Datas;
using PenMotion.Datas.Items;
using PenMotion.Datas.Items.Elements;
using PenMotionEditor.UI.Elements;
using PenMotionEditor.UI.Controls;
using PenMotionEditor.UI.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace PenMotionEditor.UI.Tabs; 

public partial class MotionTab : UserControl {
    private MotionEditorContext EditorContext;
    private GLoopEngine LoopEngine => EditorContext.LoopEngine;
    private MotionFile EditingFile => EditorContext.EditingFile;
    private GraphEditorTab GraphEditorTab => EditorContext.GraphEditorTab;

    //Event area
    private const float FolderSideEventWeight = 0.2f;
    private const float FolderMidEventWeight = 1f - FolderSideEventWeight * 2f;

    private List<MotionItemBase> itemList;
    public Dictionary<MotionItemBase, MotionItemBaseView> DataToViewDict => MotionTreeView.RealizedViews;

    //Selected
    public bool IsSelectedItemCopyable {
        get {
            if (MotionTreeView.SelectedModels.Count == 0) {
                return false;
            }

            foreach (MotionItemBase item in MotionTreeView.SelectedModels) {
                if (item is MotionItem) {
                    return true;
                }
            }

            return false;
        }
    }

    public MotionFolderItem SelectedItemParent {
        get {
            return MotionTreeView.SelectedModel?.Parent ?? EditingFile?.rootFolder;
        }
    }

    public MotionTab() {
        InitializeComponent();
    }

    public void Init(MotionEditorContext editorContext) {
        EditorContext = editorContext;

        InitMembers();
        RegisterEvents();
    }

    private void InitMembers() {
        itemList = new List<MotionItemBase>();
        MotionTreeView.Init(EditorContext);
    }

    private void RegisterEvents() {
        ControlBar.CreateItemButtonClick += CreateItemButton_OnClick;
        ControlBar.CreateFolderButtonClick += CreateFolderButton_OnClick;
        ControlBar.CopyItemButtonClick += CopyItemButton_OnClick;
        ControlBar.RemoveItemButtonClick += RemoveItemButton_OnClick;

        MotionTreeView.SelectionChangedByModel += SelectionItemChanged;
        MotionTreeView.MoveRequested += MotionListView_ItemMoved;
    }

    //Events
    internal void EditingFile_ItemCreated(MotionItemBase item, MotionFolderItem parentFolder) {
        if (item == null) {
            return;
        }

        itemList.Add(item);
        if (parentFolder == null && item is MotionFolderItem rootFolder)
            MotionTreeView.SetRoot(rootFolder);
        else
            MotionTreeView.ScheduleRebuild();

        EditorContext.MarkUnsaved();
    }

    internal void EditingFile_ItemRemoved(MotionItemBase item, MotionFolderItem parentFolder) {
        itemList.Remove(item);
        MotionTreeView.ScheduleRebuild();

        EditorContext.MarkUnsaved();
    }

    private void CreateItemButton_OnClick() {
        MotionItem item = EditingFile.CreateMotionDefault(SelectedItemParent);
        MotionTreeView.SelectSingleDeferred(item);
    }

    private void CreateFolderButton_OnClick() {
        MotionFolderItem item = EditingFile.CreateFolder(SelectedItemParent);
        MotionTreeView.SelectSingleDeferred(item);
    }

    private void RemoveItemButton_OnClick() {
        foreach (MotionItemBase item in MotionTreeView.SelectedModels.ToArray()) {
            EditingFile.RemoveItem(item);
        }
    }

    private void CopyItemButton_OnClick() {
        DuplicateSelectedMotion();

        EditorContext.MarkUnsaved();
    }

    private void SelectionItemChanged() {
        ControlBar.CopyItemButton.IsEnabled = IsSelectedItemCopyable;
        UpdateFocusItem();
    }

    private void MotionListView_ItemMoved(MotionItemBase item, MotionItemBase target, MotionTreeDropMode mode) {
        MotionFolderItem oldParent = item.Parent;
        MotionFolderItem newParent = mode == MotionTreeDropMode.Inside
            ? target as MotionFolderItem
            : target.Parent;
        if (oldParent == null || newParent == null)
            return;
        int index = mode == MotionTreeDropMode.Inside
            ? newParent.childList.Count
            : newParent.childList.IndexOf(target) + (mode == MotionTreeDropMode.After ? 1 : 0);
        if (ReferenceEquals(oldParent, newParent)) {
            int oldIndex = oldParent.childList.IndexOf(item);
            if (oldIndex >= 0 && oldIndex < index)
                index--;
        }
        oldParent.RemoveChild(item);
        newParent.InsertChild(Math.Clamp(index, 0, newParent.childList.Count), item);
        MotionTreeView.ScheduleRebuild();
    }

    private void MotionListView_MessageOccured(string message) {
        ToastMessage.Show(message);
    }

    public void ClearItems() {
        itemList.Clear();
        MotionTreeView.ClearTree();
    }

    public void DuplicateSelectedMotion() {
        if (!IsSelectedItemCopyable) {
            return;
        }

        MotionItemBase latestNewItem = null;
        MotionFolderItem parentFolder = SelectedItemParent;
        foreach (MotionItemBase refItem in MotionTreeView.SelectedModels) {
            if (refItem.Type == MotionItemType.Motion) {
                MotionItem refMotionItem = (MotionItem)refItem;
                //Create motion
                MotionItem newItem = EditingFile.CreateMotionEmpty(parentFolder);
                latestNewItem = newItem;

                //Copy points
                foreach (MotionPoint refPoint in refMotionItem.pointList) {
                    MotionPoint point = new();
                    newItem.AddPoint(point);

                    point.SetMainPoint(refPoint.MainPoint);
                    for (int i = 0; i < refPoint.SubPoints.Length; ++i) {
                        point.SetSubPoint(i, refPoint.SubPoints[i]);
                    }
                }

                //Set name
                const string CopyPostfix = " (Clone)";
                string name = refItem.Name + CopyPostfix;

                newItem.SetName(name);
            }
        }

        if (latestNewItem != null) {
            MotionTreeView.SelectSingleDeferred(latestNewItem);
        }
    }

    private void UpdateFocusItem() {
        if (MotionTreeView.SelectedModels.Count == 1) {
            MotionItemBase item = MotionTreeView.SelectedModel;

            if (item.Type == MotionItemType.Motion) {
                EditorContext.GraphEditorTab.AttachMotion((MotionItem)item);
                EditorContext.PreviewTab.ResetPreviewTime();

                return;
            }
        }

        EditorContext.GraphEditorTab.DetachMotion();
    }

    public void UpdateItemPreviews() {
        foreach (MotionItemBase item in itemList)
            if (item.Type == MotionItemType.Motion)
                MotionTreeView.RefreshRealized(item);
    }

    public void RefreshMotionPreview(MotionItem item) => MotionTreeView.RefreshRealized(item);

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
