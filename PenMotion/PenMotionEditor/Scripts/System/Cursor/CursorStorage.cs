using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GKit;
using PenMotionEditor.Properties;
using PenMotionEditor.UI.Windows;

namespace PenMotionEditor {
	public class CursorStorage {
		public readonly Cursor
			cursor_default,
			cursor_add,
			cursor_remove;

		public CursorStorage() {
			cursor_default = GetCursor(Properties.Resources.Cursor_Default);
			cursor_add = GetCursor(Properties.Resources.Cursor_Add);
			cursor_remove = GetCursor(Properties.Resources.Cursor_Remove);
		}

		private Cursor GetCursor(byte[] resource) {
			return new Cursor(new MemoryStream(resource));
		}
	}
}
