using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PendulumMotion.Component {
	public struct Vector2 {
		public float x, y;

		public Vector2(float x, float y) {
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
			if (obj is Vector2) {
				return (Vector2)obj == this;
			} else {
				return obj.GetHashCode() == this.GetHashCode();
			}
		}
		public override string ToString() {
			return x.ToString("0.000") + ", " + y.ToString("0.000");
		}
		public static Vector2 Parse(string text) {
			text = text.Replace("(", "");
			text = text.Replace(")", "");
			text = text.Trim();
			string[] nums = text.Split(',');

			return new Vector2(float.Parse(nums[0], CultureInfo.InvariantCulture), float.Parse(nums[1], CultureInfo.InvariantCulture));
		}

		public Vector2 Normalized {
			get {
				float lengthInv = 1f / (float)Math.Sqrt(x * x + y * y);
				return new Vector2(x * lengthInv, y * lengthInv);
			}
		}


		public static bool operator ==(Vector2 left, Vector2 right) {
			return left.x == right.x && left.y == right.y;
		}
		public static bool operator !=(Vector2 left, Vector2 right) {
			return left.x != right.x || left.y != right.y;
		}

		public static Vector2 operator +(Vector2 left, Vector2 right) {
			return new Vector2(left.x + right.x, left.y + right.y);
		}
		public static Vector2 operator -(Vector2 left, Vector2 right) {
			return new Vector2(left.x - right.x, left.y - right.y);
		}
		public static Vector2 operator -(Vector2 vector2) {
			return new Vector2(-vector2.x, -vector2.y);
		}

		public static Vector2 operator *(Vector2 left, Vector2 right) {
			return new Vector2(left.x * right.x, left.y * right.y);
		}
		public static Vector2 operator *(Vector2 left, int right) {
			return new Vector2(left.x * right, left.y * right);
		}
		public static Vector2 operator *(int left, Vector2 right) {
			return right * left;
		}
		public static Vector2 operator *(Vector2 left, float right) {
			return new Vector2(left.x * right, left.y * right);
		}
		public static Vector2 operator *(float left, Vector2 right) {
			return right * left;
		}
		public static Vector2 operator *(Vector2 left, double right) {
			float rightF = (float)right;
			return new Vector2(left.x * rightF, left.y * rightF);
		}
		public static Vector2 operator *(double left, Vector2 right) {
			return right * left;
		}

		public static Vector2 operator /(Vector2 left, Vector2 right) {
			return new Vector2(left.x / right.x, left.y / right.y);
		}
		public static Vector2 operator /(Vector2 left, int right) {
			return new Vector2(left.x / right, left.y / right);
		}
		public static Vector2 operator /(Vector2 left, float right) {
			return new Vector2(left.x / right, left.y / right);
		}
		public static Vector2 operator /(Vector2 left, double right) {
			float rightF = (float)right;
			return new Vector2(left.x / rightF, left.y / rightF);
		}
	}
	public struct Vector3 {
		public float x;
		public float y;
		public float z;

		public Vector3(float x, float y, float z) {
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public override string ToString() {
			return x.ToString("0.000") + ", " + y.ToString("0.000") + ", " + z.ToString("0.000");
		}
		public override bool Equals(object other) {
			return other is Vector3 && this.Equals((Vector3)other);
		}
		public override int GetHashCode() {
			return this.x.GetHashCode() ^ this.y.GetHashCode() << 2 ^ this.z.GetHashCode() >> 2;
		}

		public static bool operator ==(Vector3 left, Vector3 right) {
			return left.x == right.x && left.y == right.y && left.z == right.z;
		}
		public static bool operator !=(Vector3 left, Vector3 right) {
			return left.x != right.x || left.y != right.y || left.z != right.z;
		}
		public static Vector3 operator +(Vector3 left, Vector3 right) {
			return new Vector3(left.x + right.x, left.y + right.y, left.z + right.z);
		}
		public static Vector3 operator -(Vector3 left, Vector3 right) {
			return new Vector3(left.x - right.x, left.y - right.y, left.z - right.z);
		}
		public static Vector3 operator -(Vector3 vector) {
			return new Vector3(-vector.x, -vector.y, -vector.z);
		}

		public static Vector3 operator *(Vector3 left, Vector3 right) {
			return new Vector3(left.x * right.x, left.y * right.y, left.z * right.z);
		}
		public static Vector3 operator *(Vector3 left, int right) {
			return new Vector3(left.x * right, left.y * right, left.z * right);
		}
		public static Vector3 operator *(int left, Vector3 right) {
			return right * left;
		}
		public static Vector3 operator *(Vector3 left, float right) {
			return new Vector3(left.x * right, left.y * right, left.z * right);
		}
		public static Vector3 operator *(float left, Vector3 right) {
			return right * left;
		}
		public static Vector3 operator *(Vector3 left, double right) {
			float rightF = (float)right;
			return new Vector3(left.x * rightF, left.y * rightF, left.z * rightF);
		}
		public static Vector3 operator *(double left, Vector3 right) {
			return right * left;
		}

		public static Vector3 operator /(Vector3 left, Vector3 right) {
			return new Vector3(left.x / right.x, left.y / right.y, left.z / right.z);
		}
		public static Vector3 operator /(Vector3 left, int right) {
			return new Vector3(left.x / right, left.y / right, left.z / right);
		}
		public static Vector3 operator /(Vector3 left, float right) {
			return new Vector3(left.x / right, left.y / right, left.z / right);
		}
		public static Vector3 operator /(Vector3 left, double right) {
			float rightF = (float)right;
			return new Vector3(left.x / rightF, left.y / rightF, left.z / rightF);
		}
	}

}
