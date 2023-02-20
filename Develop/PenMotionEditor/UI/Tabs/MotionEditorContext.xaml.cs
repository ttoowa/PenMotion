﻿using GKitForWPF;
using Microsoft.Win32;
using PenMotion.Datas;
using PenMotion.System;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PenMotionEditor.UI.Tabs;

public partial class MotionEditorContext : UserControl {
    public static MotionEditorContext Instance { get; private set; }

    //Modules
    public GLoopEngine LoopEngine { get; private set; }
    public CursorStorage CursorStorage { get; private set; }

    //EditingFile
    public bool IsSaved { get; private set; }
    public bool OnEditing => EditingFile != null;
    public MotionFile EditingFile { get; private set; }

    public MotionEditorContext() {
        Instance = this;

        InitializeComponent();

        if (this.IsDesignMode()) {
            return;
        }
    }

    public void Init(GLoopEngine loopEngine = null) {
        if (loopEngine != null) {
            LoopEngine = loopEngine;
        } else {
            LoopEngine = new GLoopEngine();

            LoopEngine.StartLoop();
            LoopEngine.MaxOverlapFrame = 1;
        }

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
        CursorStorage = new CursorStorage();

        IsSaved = true;
    }

    private void RegisterEvents() {
        LoopEngine.AddLoopAction(OnTick);
    }

    public bool CreateFile(bool checkSaved = true, bool createRootFolder = true) {
        if (checkSaved && !ShowSaveQuestion()) {
            return false;
        }

        CloseFile(false);
        EditingFile = new MotionFile(createRootFolder);

        RegisterFileEvents(EditingFile);

        MarkSaved();
        return true;
    }

    public bool OpenFile(bool checkSaved = true) {
        if (checkSaved && !ShowSaveQuestion()) {
            return false;
        }

        string filePath = GetOpenFilename();
        if (string.IsNullOrEmpty(filePath)) {
            return false;
        }

        CloseFile(false);
        EditingFile = new MotionFile(false);

        RegisterFileEvents(EditingFile);

        EditingFile.Load(filePath);

        MotionTab.UpdateItemPreviews();

        return true;
    }

    public bool SaveFile() {
        string filePath = GetSaveFilename();
        if (string.IsNullOrEmpty(filePath)) {
            return false;
        }

        EditingFile.Save(filePath);

        MarkSaved();
        return true;
    }

    public bool CloseFile(bool checkSaved = true) {
        if (!OnEditing) {
            return true;
        }

        if (checkSaved && !ShowSaveQuestion()) {
            return false;
        }

        UnregisterFileEvents(EditingFile);

        MotionTab.ClearItems();
        GraphEditorTab.DetachMotion();
        EditingFile = null;

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
            MessageBoxResult result = MessageBox.Show($"작업 중인 파일이 저장되지 않았습니다.{Environment.NewLine}" + $"저장하시겠습니까?", "저장", MessageBoxButton.YesNoCancel);
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

    private void RegisterFileEvents(MotionFile file) {
        file.ItemCreated += MotionTab.EditingFile_ItemCreated;
        file.ItemRemoved += MotionTab.EditingFile_ItemRemoved;

        MotionTab.EditingFile_ItemCreated(EditingFile.rootFolder, null);
    }

    private void UnregisterFileEvents(MotionFile file) {
        file.ItemCreated -= MotionTab.EditingFile_ItemCreated;
        file.ItemRemoved -= MotionTab.EditingFile_ItemRemoved;

        MotionTab.EditingFile_ItemRemoved(EditingFile.rootFolder, null);
    }

    //Events
    private void OnTick() {
        //ApplyPreviewFPS();
    }

    private string GetSaveFilename() {
        if (EditingFile.IsFilePathAvailable) {
            return EditingFile.filePath;
        } else {
            SaveFileDialog dialog = new();
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
        OpenFileDialog dialog = new();
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