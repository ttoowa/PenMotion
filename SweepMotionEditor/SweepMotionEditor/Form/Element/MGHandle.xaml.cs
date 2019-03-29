using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GKit;

namespace SweepMotionEditor.Form.Element
{
	/// <summary>
	/// MGHandle.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MGHandle : UserControl
	{
		public MGHandle()
		{
			InitializeComponent();
		}


		public class SubPoint
		{
			public Vector2 position;
			public Shape shape;
		}

		private static SolidColorBrush HandleColor = "DBEBC0".ToBrush();
		private const float HandleWidth = 14f;
		private const float HandleWidthHalf = HandleWidth * 0.5f;

		private Canvas graphContext;

		private SubPoint mainPoint;
		private SubPoint[] subPoints;
		private Line[] pointConnectors;

		public MGHandle(Canvas graphContext)
		{
			this.graphContext = graphContext;



		}
	}
}
