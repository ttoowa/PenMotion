# PendulumMotion
모션그래프 에디터 & API

## How to use Editor

>#### On editing context
><kbd>Ctrl</kbd> + <kbd>Left Click</kbd> Add control point.</br>
><kbd>Ctrl</kbd> + <kbd>Right Click</kbd> Remove control point.

## How to use API
``` C#
using PendulumMotion;

…

void RegisterMotion() {
  PMotionQuery.LoadFile("C:\motionFile.pmotion", "CustomKey");
}

void UseMotion() {
  for(float time=0f; time<1f; time+=0.01f) {
    float motionTime = PMotionQuery.GetMotionValue("CustomKey", "MotionName", time);
    
    UI.Position = new Vector2(motionTime, 0f);
    
    //It's just example. please don't use it.
    //maybe GameEngine's function is better than this.
    Thread.Sleep(16);
  }
}
```
