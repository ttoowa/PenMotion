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
using PendulumMotionEditor.Properties;
using PendulumMotionEditor.Views.Windows;

namespace PendulumMotionEditor {
	public class CursorStorage {

		private MainWindow mainWindow;
		public readonly Cursor
			cursor_default,
			cursor_add,
			cursor_remove;

		public CursorStorage(MainWindow mainWindow) {
			this.mainWindow = mainWindow;

			//LoadCursor
			cursor_default = GetCursor(Properties.Resources.Cursor_Default);
			cursor_add = GetCursor(Properties.Resources.Cursor_Add);
			cursor_remove = GetCursor(Properties.Resources.Cursor_Remove);
		}

		private Cursor GetCursor(byte[] resource) {
			return new Cursor(new MemoryStream(resource));
			//return ((FrameworkElement)mainWindow.Resources[resourceName]).Cursor;
		}
	}
}
