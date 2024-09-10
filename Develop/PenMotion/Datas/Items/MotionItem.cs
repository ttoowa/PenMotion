using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using PenMotion.System;
using PenMotion.Datas.Items.Elements;

namespace PenMotion.Datas.Items {
    public class MotionItem : MotionItemBase {
        public const int DefaultMaxSample = 12;
        public const float DefaultMaxTolerance = 0.0001f;

        public List<MotionPoint> pointList;

        public delegate void PointInsertedDelegate(int index, MotionPoint point);

        public event PointInsertedDelegate PointInserted;

        public delegate void PointRemovedDelegate(MotionPoint point);

        public event PointRemovedDelegate PointRemoved;

        public static MotionItem CreateDefault(MotionFile ownerFile) {
            var item = new MotionItem(ownerFile);
            item.AddPoint(new MotionPoint(new PVector2(0f, 0f)));
            item.AddPoint(new MotionPoint(new PVector2(1f, 1f)));
            return item;
        }

        public static void CreateDefault(MotionItem motionItem) {
            motionItem.AddPoint(new MotionPoint(new PVector2(0f, 0f)));
            motionItem.AddPoint(new MotionPoint(new PVector2(1f, 1f)));
        }

        internal MotionItem(MotionFile ownerFile) : base(ownerFile, MotionItemType.Motion) {
            pointList = new List<MotionPoint>();
        }

        public float GetMotionValue(float linearValue, int maxSample = DefaultMaxSample, float tolerance = DefaultMaxTolerance) {
            linearValue = Math.Max(Math.Min(linearValue, 1f), 0f);

            int rightIndex = GetRightPointIndex(linearValue);
            if (rightIndex == -1) {
                if (pointList.Count > 0) {
                    //마지막 포인트를 벗어나면 마지막 좌표 반환
                    return pointList[pointList.Count - 1].MainPoint.y;
                } else {
                    //포인트가 하나이거나 없을 때 1 반환
                    return 1;
                }
            }

            MotionPoint left = pointList[rightIndex - 1];
            MotionPoint right = pointList[rightIndex];

            return PSpline.Bezier3_X2Y(linearValue, left.MainPoint, left.GetAbsoluteSubPoint(1), right.GetAbsoluteSubPoint(0), right.MainPoint, maxSample, tolerance);
        }

        public int GetRightPointIndex(float linearValue) {
            int rightIndex = -1;
            for (int i = 1; i < pointList.Count; ++i) {
                if (pointList[i].MainPoint.x >= linearValue) {
                    rightIndex = i;
                    break;
                }
            }

            return rightIndex;
        }

        public void AddPoint(MotionPoint point) {
            InsertPoint(pointList.Count, point);
        }

        public void InsertPoint(int index, MotionPoint point) {
            pointList.Insert(index, point);

            PointInserted?.Invoke(index, point);
        }

        public void RemovePoint(MotionPoint point) {
            pointList.Remove(point);

            PointRemoved?.Invoke(point);
        }
    }
}