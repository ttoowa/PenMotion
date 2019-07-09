using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PendulumMotion.Components.Items;
using PenMotionEditor.UI.Tabs;
using GKit;

namespace PenMotionEditor.UI.Items {
	public class MotionFolderItemView : MotionItemBaseView {
		public new MotionFolderItem Data {
			get {
				return base.Data.Cast<MotionFolderItem>();
			}
			set {
				base.Data = value;
			}
		}

		public List<MotionItemBaseView> childList;

		public MotionFolderItemView(EditorContext editorContext) : base(editorContext, MotionItemType.Folder) {
			ContentPanel.Children.Remove(MotionContent);

			childList = new List<MotionItemBaseView>();
			Data = EditingFile.CreateFolder();
		}

		public void AddChild(MotionItemBaseView itemView) {
			itemView.DetachParent();

			childList.Add(itemView);
			ChildStackPanel.Children.Add(itemView);

			Data.AddChild(itemView.Data);
		}
		public void InsertChild(int index, MotionItemBaseView itemView) {
			itemView.DetachParent();

			childList.Insert(index, itemView);
			ChildStackPanel.Children.Insert(index, itemView);

			Data.InsertChild(index, itemView.Data);
		}
		public void RemoveChild(MotionItemBaseView itemView) {
			childList.Remove(itemView);
			ChildStackPanel.Children.Remove(itemView);

			Data.RemoveChild(itemView.Data);
		}
		public void SetRootFolder() {
			BackPanel.Children.Remove(ContentContext);
			ChildStackPanel.Margin = new Thickness();
		}
	}
}
