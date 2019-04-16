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

		public GLoopEngine loopEngine;
		public MainWindow mainWindow;

		public Root(MainWindow mainWindow) {
			Instance = this;

			this.mainWindow = mainWindow;
			loopEngine = new GLoopEngine();


			loopEngine.StartLoop();
			loopEngine.AddLoopAction(OnTick);

		}

		private void OnTick() {
			mainWindow.ApplyPreviewFPS();
		}
	}
}
