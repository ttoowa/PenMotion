using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PenMotion.Datas.Items;
using PenMotionEditor.UI.Tabs;
using GKitForWPF;
using GKitForWPF.UI.Controls;
using System.Windows.Controls;

namespace PenMotionEditor.UI.Elements {
	public class MotionFolderItemView : MotionItemBaseView, ITreeFolder {
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
		public MotionFolderItemView(MotionEditorContext editorContext, MotionFolderItem data) : base(editorContext, MotionItemType.Folder) {
			ContentPanel.Children.Remove(MotionContent);

			Data = data;
		}

		public void SetRootFolder() {
			BackPanel.Children.Remove(ContentContext);
			ChildStackPanel.Margin = new Thickness();
		}
	}
}
