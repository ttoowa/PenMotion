using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GKit;
using PendulumMotionEditor.Views.Windows;

namespace PendulumMotionEditor {
	public class Root {

		private GLoopEngine loopEngine;
		private MainWindow mainWindow;

		public Root(MainWindow mainWindow) {
			this.mainWindow = mainWindow;

			loopEngine = new GLoopEngine();
			loopEngine.StartLoop();

			loopEngine.AddLoopAction(OnTick);
		}


		private void OnTick() {
			
		}
	}
}
