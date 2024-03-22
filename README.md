# PenMotion
![Language badge](https://i.imgur.com/LUHwEU7.png)ㅤ![OS badge](https://i.imgur.com/MbF1zsp.png)ㅤ![State badge](https://i.imgur.com/G4YiiaG.png)

## 'ForTaleKit' branch 
- assembly 참조경로가 수정됨

## Summary
Motion Easing Editor & API

## How to use Editor

#### On editing context
Panning</br>
<kbd>Space</kbd> + <kbd>Mouse Left Drag</kbd> </br>
Zoom</br>
<kbd>Ctrl</kbd> + <kbd>Space</kbd> + <kbd>Mouse Left Drag</kbd> </br>
</br>
Add control point</br>
<kbd>Ctrl</kbd> + <kbd>Mouse Left Click</kbd></br>
Remove control point</br>
<kbd>Alt</kbd> + <kbd>Mouse Left Click</kbd>


<br/><br/>
## How to use API
``` C#
using PenMotion;

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

<br/><br/>
## Preview

![Honeycam_20220919_224543](https://user-images.githubusercontent.com/19409574/191032066-09a0ba4d-e4e0-4855-8005-354217245439.gif)

