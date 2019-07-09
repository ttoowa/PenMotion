using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using GKit;

namespace PenMotionEditor {
	public static class CanvasUtility {
		public static void SetCanvasPosition(this UIElement element, Vector2 position) {
			Canvas.SetLeft(element, position.x);
			Canvas.SetTop(element, position.y);
		}
		public static Vector2 GetCanvasPosition(this UIElement element) {
			return new Vector2((float)Canvas.GetLeft(element), (float)Canvas.GetTop(element));
		}
		public static void SetLinePosition(this Line line, Vector2 from, Vector2 to) {
			line.X1 = from.x;
			line.Y1 = from.y;
			line.X2 = to.x;
			line.Y2 = to.y;
		}
		public static Vector2 GetLinePosition1(this Line line) {
			return new Vector2((float)line.X1, (float)line.Y1);
		}
		public static Vector2 GetLinePosition2(this Line line) {
			return new Vector2((float)line.X2, (float)line.Y2);
		}
	}
}
