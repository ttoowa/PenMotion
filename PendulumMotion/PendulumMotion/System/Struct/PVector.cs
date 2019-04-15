using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace PendulumMotion {
	public struct PVector2 {
		public float x, y;

		public PVector2(float x, float y) {
			this.x = x;
			this.y = y;
		}
		public float magnitude {
			get {
				return (float)Math.Sqrt(x * x + y * y);
			}
		}
		public float sqrMagnitude {
			get {
				return x * x + y * y;
			}
		}

		/// <summary>
		///우측 하단이 양수인 좌표계를 기준으로 범위 내에 있는지 검사합니다.
		/// </summary>
		public bool CheckOverlap(float left, float right, float top, float bottom) {
			return x < right && x > left && y > top && y < bottom;
		}
		public override int GetHashCode() {
			return x.GetHashCode() ^ y.GetHashCode() << 2;
		}
		public override bool Equals(object obj) {
			if (obj is PVector2) {
				return (PVector2)obj == this;
			} else {
				return obj.GetHashCode() == this.GetHashCode();
			}
		}
		public override string ToString() {
			return x.ToString("0.000") + ", " + y.ToString("0.000");
		}
		public static PVector2 Parse(string text) {
			text = text.Replace("(", "");
			text = text.Replace(")", "");
			text = text.Trim();
			string[] nums = text.Split(',');

			return new PVector2(float.Parse(nums[0], CultureInfo.InvariantCulture), float.Parse(nums[1], CultureInfo.InvariantCulture));
		}

		public PVector2 Normalized {
			get {
				float lengthInv = 1f / (float)Math.Sqrt(x * x + y * y);
				return new PVector2(x * lengthInv, y * lengthInv);
			}
		}


		public static bool operator ==(PVector2 left, PVector2 right) {
			return left.x == right.x && left.y == right.y;
		}
		public static bool operator !=(PVector2 left, PVector2 right) {
			return left.x != right.x || left.y != right.y;
		}

		public static PVector2 operator +(PVector2 left, PVector2 right) {
			return new PVector2(left.x + right.x, left.y + right.y);
		}
		public static PVector2 operator -(PVector2 left, PVector2 right) {
			return new PVector2(left.x - right.x, left.y - right.y);
		}
		public static PVector2 operator -(PVector2 vector2) {
			return new PVector2(-vector2.x, -vector2.y);
		}

		public static PVector2 operator *(PVector2 left, PVector2 right) {
			return new PVector2(left.x * right.x, left.y * right.y);
		}
		public static PVector2 operator *(PVector2 left, int right) {
			return new PVector2(left.x * right, left.y * right);
		}
		public static PVector2 operator *(int left, PVector2 right) {
			return right * left;
		}
		public static PVector2 operator *(PVector2 left, float right) {
			return new PVector2(left.x * right, left.y * right);
		}
		public static PVector2 operator *(float left, PVector2 right) {
			return right * left;
		}
		public static PVector2 operator *(PVector2 left, double right) {
			float rightF = (float)right;
			return new PVector2(left.x * rightF, left.y * rightF);
		}
		public static PVector2 operator *(double left, PVector2 right) {
			return right * left;
		}

		public static PVector2 operator /(PVector2 left, PVector2 right) {
			return new PVector2(left.x / right.x, left.y / right.y);
		}
		public static PVector2 operator /(PVector2 left, int right) {
			return new PVector2(left.x / right, left.y / right);
		}
		public static PVector2 operator /(PVector2 left, float right) {
			return new PVector2(left.x / right, left.y / right);
		}
		public static PVector2 operator /(PVector2 left, double right) {
			float rightF = (float)right;
			return new PVector2(left.x / rightF, left.y / rightF);
		}
	}
	public struct PVector3 {
		public float x;
		public float y;
		public float z;

		public PVector3(float x, float y, float z) {
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public override string ToString() {
			return x.ToString("0.000") + ", " + y.ToString("0.000") + ", " + z.ToString("0.000");
		}
		public override bool Equals(object other) {
			return other is PVector3 && this.Equals((PVector3)other);
		}
		public override int GetHashCode() {
			return this.x.GetHashCode() ^ this.y.GetHashCode() << 2 ^ this.z.GetHashCode() >> 2;
		}

		public static bool operator ==(PVector3 left, PVector3 right) {
			return left.x == right.x && left.y == right.y && left.z == right.z;
		}
		public static bool operator !=(PVector3 left, PVector3 right) {
			return left.x != right.x || left.y != right.y || left.z != right.z;
		}
		public static PVector3 operator +(PVector3 left, PVector3 right) {
			return new PVector3(left.x + right.x, left.y + right.y, left.z + right.z);
		}
		public static PVector3 operator -(PVector3 left, PVector3 right) {
			return new PVector3(left.x - right.x, left.y - right.y, left.z - right.z);
		}
		public static PVector3 operator -(PVector3 vector) {
			return new PVector3(-vector.x, -vector.y, -vector.z);
		}

		public static PVector3 operator *(PVector3 left, PVector3 right) {
			return new PVector3(left.x * right.x, left.y * right.y, left.z * right.z);
		}
		public static PVector3 operator *(PVector3 left, int right) {
			return new PVector3(left.x * right, left.y * right, left.z * right);
		}
		public static PVector3 operator *(int left, PVector3 right) {
			return right * left;
		}
		public static PVector3 operator *(PVector3 left, float right) {
			return new PVector3(left.x * right, left.y * right, left.z * right);
		}
		public static PVector3 operator *(float left, PVector3 right) {
			return right * left;
		}
		public static PVector3 operator *(PVector3 left, double right) {
			float rightF = (float)right;
			return new PVector3(left.x * rightF, left.y * rightF, left.z * rightF);
		}
		public static PVector3 operator *(double left, PVector3 right) {
			return right * left;
		}

		public static PVector3 operator /(PVector3 left, PVector3 right) {
			return new PVector3(left.x / right.x, left.y / right.y, left.z / right.z);
		}
		public static PVector3 operator /(PVector3 left, int right) {
			return new PVector3(left.x / right, left.y / right, left.z / right);
		}
		public static PVector3 operator /(PVector3 left, float right) {
			return new PVector3(left.x / right, left.y / right, left.z / right);
		}
		public static PVector3 operator /(PVector3 left, double right) {
			float rightF = (float)right;
			return new PVector3(left.x / rightF, left.y / rightF, left.z / rightF);
		}
	}

}
