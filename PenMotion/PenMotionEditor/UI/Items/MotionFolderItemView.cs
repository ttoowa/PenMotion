using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PenMotion.Datas.Items;
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

		public MotionFolderItemView(EditorContext editorContext, MotionFolderItem data) : base(editorContext, MotionItemType.Folder) {
			ContentPanel.Children.Remove(MotionContent);

			Data = data;
		}

		private void InsertChildView(int index, MotionItemBaseView itemView) {
			ChildStackPanel.Children.Insert(index, itemView);
		}
		private void RemoveChildView(MotionItemBaseView itemView) {
			ChildStackPanel.Children.Remove(itemView);
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
