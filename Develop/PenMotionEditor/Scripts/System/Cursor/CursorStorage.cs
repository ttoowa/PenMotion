using System.IO;
using System.Windows.Input;

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
