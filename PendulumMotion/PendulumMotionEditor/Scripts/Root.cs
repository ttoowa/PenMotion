using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GKit;
using PendulumMotionEditor.Views.Context;
using PendulumMotionEditor.Views.Windows;

namespace PendulumMotionEditor {
	public class Root {
		public static Root Instance {
			get; private set;
		}

		public readonly GLoopEngine loopEngine;
		public readonly MotionEditorContext editorContext;
		public readonly CursorStorage cursorStorage;

		public event Action OnLoopTick;

		public Root(MotionEditorContext editorContext) {
			Instance = this;

			this.editorContext = editorContext;
			loopEngine = new GLoopEngine();
			cursorStorage = new CursorStorage();


			loopEngine.StartLoop();
			loopEngine.AddLoopAction(OnTick);

		}

		private void OnTick() {
			editorContext.ApplyPreviewFPS();

			OnLoopTick?.Invoke();
		}
	}
}
