using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading;
using GKitForWPF.UI.Controls;
using PenMotion.Datas.Items;
using PenMotionEditor.UI.Elements;
using PenMotionEditor.UI.Tabs;

namespace PenMotionEditor.UI.Controls;

public enum MotionTreeDropMode { Before, Inside, After }
public sealed record MotionTreeNode(MotionItemBase Model, int Depth);

/// <summary>Flattened, recycling tree for large motion libraries.</summary>
public sealed class VirtualizedMotionTree : ListBox {
    private const double DepthIndent = 7d;
    private const int RecycledViewLimit = 48;
    private MotionEditorContext editorContext;
    private MotionFolderItem root;
    private readonly HashSet<MotionFolderItem> collapsedFolders = new();
    private readonly Stack<MotionItemView> recycledMotionViews = new();
    private readonly Stack<MotionFolderItemView> recycledFolderViews = new();
    private readonly Dictionary<MotionFolderItemView, MouseButtonEventHandler> foldHandlers = new();
    private readonly DropTargetIndicatorPopup dropIndicator = new();
    private readonly DispatcherTimer rebuildTimer;
    private MotionItemBase pendingSelection;
    private int rebuildPostPending;

    public Dictionary<MotionItemBase, MotionItemBaseView> RealizedViews { get; } = new();
    public IReadOnlyCollection<MotionItemBase> SelectedModels =>
        SelectedItems.Cast<MotionTreeNode>().Select(x => x.Model).ToArray();
    public MotionItemBase SelectedModel => SelectedItems.Count == 1
        ? ((MotionTreeNode)SelectedItems[0]).Model
        : null;

    public event Action SelectionChangedByModel;
    public event Action<MotionItemBase, MotionItemBase, MotionTreeDropMode> MoveRequested;

    private Point dragStart;
    private MotionItemBase draggedModel;
    private ListBoxItem dragSourceContainer;
    private DragRowGhostPopup dragGhost;
    private DragWheelScrollController dragWheelScroll;

    public VirtualizedMotionTree() {
        SelectionMode = SelectionMode.Extended;
        Background = Brushes.Transparent;
        BorderThickness = new Thickness(0);
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VirtualizingPanel.SetIsVirtualizing(this, true);
        VirtualizingPanel.SetVirtualizationMode(this, VirtualizationMode.Recycling);
        VirtualizingPanel.SetCacheLength(this, new VirtualizationCacheLength(0, 0));
        ScrollViewer.SetCanContentScroll(this, true);
        ScrollViewer.SetHorizontalScrollBarVisibility(this, ScrollBarVisibility.Disabled);
        ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));

        Style style = new(typeof(ListBoxItem));
        style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        style.Setters.Add(new Setter(PaddingProperty, new Thickness(0)));
        style.Setters.Add(new Setter(MarginProperty, new Thickness(0)));
        style.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0)));
        style.Setters.Add(new Setter(BackgroundProperty, Brushes.Transparent));
        ItemContainerStyle = style;

        rebuildTimer = new DispatcherTimer(DispatcherPriority.Background) {
            Interval = TimeSpan.FromMilliseconds(40)
        };
        rebuildTimer.Tick += (_, _) => {
            rebuildTimer.Stop();
            RebuildNow();
        };

        SelectionChanged += (_, _) => {
            UpdateRealizedSelection();
            SelectionChangedByModel?.Invoke();
        };
        PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        PreviewMouseMove += OnPreviewMouseMove;
        PreviewDragOver += OnPreviewDragOver;
        Drop += OnDrop;
        AllowDrop = true;
    }

    public void Init(MotionEditorContext context) => editorContext = context;

    public void SetRoot(MotionFolderItem rootFolder) {
        root = rootFolder;
        ScheduleRebuild();
    }

    public void ScheduleRebuild() {
        if (!Dispatcher.CheckAccess()) {
            if (Interlocked.Exchange(ref rebuildPostPending, 1) == 0) {
                Dispatcher.BeginInvoke(() => {
                    Interlocked.Exchange(ref rebuildPostPending, 0);
                    ScheduleRebuild();
                }, DispatcherPriority.Background);
            }
            return;
        }
        rebuildTimer.Stop();
        rebuildTimer.Start();
    }

    public void ClearTree() {
        rebuildTimer.Stop();
        root = null;
        collapsedFolders.Clear();
        RealizedViews.Clear();
        ItemsSource = null;
        UnselectAll();
    }

    public void SelectSingleDeferred(MotionItemBase model) {
        pendingSelection = model;
        ScheduleRebuild();
    }

    public void RefreshRealized(MotionItemBase model) {
        if (!Dispatcher.CheckAccess()) {
            Dispatcher.BeginInvoke(() => RefreshRealized(model), DispatcherPriority.Background);
            return;
        }
        if (!RealizedViews.TryGetValue(model, out MotionItemBaseView view))
            return;
        view.Data_NameChanged(null, model.Name);
        if (view is MotionItemView motionView)
            motionView.UpdatePreviewGraph();
    }

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item) {
        base.PrepareContainerForItemOverride(element, item);
        if (element is not ListBoxItem container || item is not MotionTreeNode node || editorContext == null)
            return;

        MotionItemBaseView view;
        if (node.Model is MotionItem motion) {
            MotionItemView motionView = recycledMotionViews.Count > 0
                ? recycledMotionViews.Pop()
                : new MotionItemView(editorContext, motion);
            motionView.Data = motion;
            motionView.UpdatePreviewGraph();
            view = motionView;
        } else {
            MotionFolderItem folder = (MotionFolderItem)node.Model;
            MotionFolderItemView folderView = recycledFolderViews.Count > 0
                ? recycledFolderViews.Pop()
                : new MotionFolderItemView(editorContext, folder);
            folderView.Data = folder;
            folderView.ChildStackPanel.Visibility = Visibility.Collapsed;
            folderView.FoldButton.LayoutTransform = new RotateTransform(
                collapsedFolders.Contains(folder) ? 0d : 90d);
            MouseButtonEventHandler foldHandler = (_, e) => {
                if (collapsedFolders.Remove(folder)) { }
                else collapsedFolders.Add(folder);
                e.Handled = true;
                ScheduleRebuild();
            };
            foldHandlers[folderView] = foldHandler;
            folderView.FoldButton.PreviewMouseLeftButtonDown += foldHandler;
            view = folderView;
        }

        view.ParentItem = null;
        view.SetDisplayName(node.Model.Name);
        view.SetSelected(SelectedItems.Contains(node));
        view.Margin = new Thickness(node.Depth * DepthIndent, 0, 0, 0);
        node.Model.NameChanged += view.Data_NameChanged;
        container.Content = view;
        RealizedViews[node.Model] = view;
    }

    protected override void ClearContainerForItemOverride(DependencyObject element, object item) {
        if (element is ListBoxItem container && container.Content is MotionItemBaseView view) {
            container.Content = null;
            if (item is MotionTreeNode node) {
                node.Model.NameChanged -= view.Data_NameChanged;
                RealizedViews.Remove(node.Model);
            }
            if (view is MotionFolderItemView folderView) {
                if (foldHandlers.Remove(folderView, out MouseButtonEventHandler handler))
                    folderView.FoldButton.PreviewMouseLeftButtonDown -= handler;
                if (recycledFolderViews.Count < RecycledViewLimit)
                    recycledFolderViews.Push(folderView);
            } else if (view is MotionItemView motionView && recycledMotionViews.Count < RecycledViewLimit) {
                recycledMotionViews.Push(motionView);
            }
        }
        base.ClearContainerForItemOverride(element, item);
    }

    private void RebuildNow() {
        MotionItemBase selected = pendingSelection ?? SelectedModel;
        pendingSelection = null;
        List<MotionTreeNode> nodes = new();
        if (root != null)
            AppendChildren(root, 0, nodes);
        ItemsSource = nodes;
        if (selected != null) {
            MotionTreeNode node = nodes.FirstOrDefault(x => ReferenceEquals(x.Model, selected));
            if (node != null) {
                SelectedItem = node;
                ScrollIntoView(node);
            }
        }
    }

    private void AppendChildren(MotionFolderItem folder, int depth, List<MotionTreeNode> nodes) {
        foreach (MotionItemBase child in folder.childList) {
            nodes.Add(new MotionTreeNode(child, depth));
            if (child is MotionFolderItem childFolder && !collapsedFolders.Contains(childFolder))
                AppendChildren(childFolder, depth + 1, nodes);
        }
    }

    private void UpdateRealizedSelection() {
        foreach ((MotionItemBase model, MotionItemBaseView view) in RealizedViews)
            view.SetSelected(SelectedModels.Contains(model));
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
        dragStart = e.GetPosition(this);
        dragSourceContainer = FindContainer(e.OriginalSource as DependencyObject);
        draggedModel = GetModel(dragSourceContainer);
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e) {
        if (e.LeftButton != MouseButtonState.Pressed || draggedModel == null)
            return;
        Point point = e.GetPosition(this);
        if (Math.Abs(point.X - dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(point.Y - dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;
        dragGhost = DragRowGhostPopup.TryCreate(this, dragSourceContainer);
        dragWheelScroll = DragWheelScrollController.TryCreate(this);
        try {
            DragDrop.DoDragDrop(this, new DataObject(typeof(MotionItemBase), draggedModel), DragDropEffects.Move);
        } finally {
            CleanupDrag();
        }
    }

    private void OnPreviewDragOver(object sender, DragEventArgs e) {
        ListBoxItem container = FindContainer(e.OriginalSource as DependencyObject);
        MotionItemBase target = GetModel(container);
        if (target == null || ReferenceEquals(target, draggedModel) ||
            draggedModel is MotionFolderItem folder && IsDescendantOf(target, folder)) {
            e.Effects = DragDropEffects.None;
            dropIndicator.Hide();
            dragGhost?.SetInvalid(true);
            return;
        }
        MotionTreeDropMode mode = GetDropMode(target, container, e.GetPosition(container));
        dropIndicator.Show(container, mode == MotionTreeDropMode.Inside
            ? DropTargetIndicatorMode.Inside
            : mode == MotionTreeDropMode.After ? DropTargetIndicatorMode.After : DropTargetIndicatorMode.Before);
        dragGhost?.SetInvalid(false);
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e) {
        try {
            ListBoxItem container = FindContainer(e.OriginalSource as DependencyObject);
            MotionItemBase target = GetModel(container);
            if (target == null || draggedModel == null || ReferenceEquals(target, draggedModel) ||
                draggedModel is MotionFolderItem folder && IsDescendantOf(target, folder))
                return;
            MoveRequested?.Invoke(draggedModel, target, GetDropMode(target, container, e.GetPosition(container)));
        } finally {
            CleanupDrag();
        }
    }

    private void CleanupDrag() {
        dropIndicator.Hide();
        dragGhost?.Dispose();
        dragGhost = null;
        dragWheelScroll?.Dispose();
        dragWheelScroll = null;
        draggedModel = null;
        dragSourceContainer = null;
    }

    private static MotionTreeDropMode GetDropMode(MotionItemBase target, FrameworkElement row, Point point) {
        double ratio = row?.ActualHeight > 0 ? point.Y / row.ActualHeight : 0d;
        if (target is MotionFolderItem) {
            if (ratio < 0.35d) return MotionTreeDropMode.Before;
            if (ratio > 0.65d) return MotionTreeDropMode.After;
            return MotionTreeDropMode.Inside;
        }
        return ratio > 0.5d ? MotionTreeDropMode.After : MotionTreeDropMode.Before;
    }

    private static bool IsDescendantOf(MotionItemBase candidate, MotionFolderItem ancestor) {
        for (MotionFolderItem parent = candidate?.Parent; parent != null; parent = parent.Parent)
            if (ReferenceEquals(parent, ancestor)) return true;
        return false;
    }

    private ListBoxItem FindContainer(DependencyObject source) {
        while (source != null && source != this) {
            if (source is ListBoxItem item) return item;
            source = VisualTreeHelper.GetParent(source);
        }
        return null;
    }

    private MotionItemBase GetModel(ListBoxItem container) => container == null
        ? null
        : (ItemContainerGenerator.ItemFromContainer(container) as MotionTreeNode)?.Model;
}
