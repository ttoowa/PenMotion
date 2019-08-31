using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PenMotion.Datas.Items;
using PenMotionEditor.UI.Tabs;
using GKit;
using GKit.WPF.UI.Controls;
using System.Windows.Controls;

namespace PenMotionEditor.UI.Items {
	public class MotionFolderItemView : MotionItemBaseView, IListFolder {
		public new MotionFolderItem Data {
			get {
				return base.Data.Cast<MotionFolderItem>();
			}
			set {
				base.Data = value;
			}
		}

		public UIElementCollection ChildItemCollection => ChildStackPanel.Children;

		public MotionFolderItemView() : base() {

		}
		public MotionFolderItemView(EditorContext editorContext, MotionFolderItem data) : base(editorContext, MotionItemType.Folder) {
			ContentPanel.Children.Remove(MotionContent);

			Data = data;
		}

		private void InsertChildView(int index, MotionItemBaseView itemView) {
			if (itemView.GetParentItem() != null) {
				((MotionFolderItemView)itemView.GetParentItem()).RemoveChildView(itemView);
			}
			ChildItemCollection.Insert(index, itemView);
		}
		private void RemoveChildView(MotionItemBaseView itemView) {
			ChildItemCollection.Remove(itemView);
		}

		public void SetRootFolder() {
			BackPanel.Children.Remove(ContentContext);
			ChildStackPanel.Margin = new Thickness();
		}

		internal void Data_ChildInserted(int index, MotionItemBase childItem) {
			InsertChildView(index, MotionTab.DataToViewDict[childItem]);
		}
		internal void Data_ChildRemoved(MotionItemBase childItem) {
			RemoveChildView(MotionTab.DataToViewDict[childItem]);
		}
	}
}
