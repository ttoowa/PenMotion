using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GKit;
using PendulumMotionEditor.Views.Windows;

namespace PendulumMotionEditor {
	public class Root {
		public static Root Instance {
			get; private set;
		}

		public readonly GLoopEngine loopEngine;
		public readonly MainWindow mainWindow;
		public readonly CursorStorage cursorStorage;

		public Root(MainWindow mainWindow) {
			Instance = this;

			this.mainWindow = mainWindow;
			loopEngine = new GLoopEngine();
			cursorStorage = new CursorStorage(mainWindow);


			loopEngine.StartLoop();
			loopEngine.AddLoopAction(OnTick);

		}

		private void OnTick() {
			mainWindow.ApplyPreviewFPS();
		}
	}
}
